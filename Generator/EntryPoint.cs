using System;
using System.CodeDom.Compiler;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Simple1C;
using Simple1C.Impl;
using Simple1C.Impl.Generation;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.Translation;
using Simple1C.Interface;
using Simple1C.Interface.Sql;

namespace Generator
{
    //todo генерить вьюшки в отдельную базу, реврайтить select-ы на эти вьюшки
    //motivation: сейчас имена колонок в join-ах в генерируемых подзапросах не переименовываются,
    //и руками можно было бы запросы прямо на sql-е писать
    public static class EntryPoint
    {
        public static int Main(string[] args)
        {
            try
            {
                var parameters = NameValueCollectionHelpers.ParseCommandLine(args);
                var cmd = parameters["cmd"];
                if (cmd == "gen-cs-meta")
                    return GenCsMeta(parameters);
                if (cmd == "gen-sql-meta")
                    return GenSqlMeta(parameters);
                if (cmd == "run-sql")
                    return RunSql(parameters);
                if (cmd == "translate-sql")
                    return TranslateSql(parameters);
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine("Usage: Generator.exe -cmd [gen-cs-meta|gen-sql-meta|run-sql|translate-sql]");
                return -1;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("unhandled exception\r\n{0}", e);
                return -1;
            }
        }

        private static int GenCsMeta(NameValueCollection parameters)
        {
            var connectionString = parameters["connection-string"];
            var resultAssemblyFullPath = parameters["result-assembly-full-path"];
            var namespaceRoot = parameters["namespace-root"];
            var scanItems = (parameters["scan-items"] ?? "").Split(',');
            var sourcePath = parameters["source-path"];
            var csprojFilePath = parameters["csproj-file-path"];
            var parametersAreValid =
                !string.IsNullOrEmpty(connectionString) &&
                (!string.IsNullOrEmpty(resultAssemblyFullPath) || !string.IsNullOrEmpty(sourcePath)) &&
                !string.IsNullOrEmpty(namespaceRoot) &&
                scanItems.Length > 0;
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd gen-cs-meta -connection-string <string> [-result-assembly-full-path <path>] -namespace-root <namespace> -scanItems Справочник.Банки,Документ.СписаниеСРасчетногоСчета [-source-path <sourcePath>] [-csproj-file-path]");
                return -1;
            }
            object globalContext = null;
            LogHelpers.LogWithTiming(string.Format("connecting to [{0}]", connectionString),
                () => globalContext = new GlobalContextFactory().Create(connectionString));

            sourcePath = sourcePath ?? GetTemporaryDirectoryFullPath();
            if (Directory.Exists(sourcePath))
                Directory.Delete(sourcePath, true);
            string[] fileNames = null;
            LogHelpers.LogWithTiming(string.Format("generating code into [{0}]", sourcePath),
                () =>
                {
                    var generator = new ObjectModelGenerator(globalContext,
                        scanItems, namespaceRoot, sourcePath);
                    fileNames = generator.Generate().ToArray();
                });

            if (!string.IsNullOrEmpty(csprojFilePath))
            {
                csprojFilePath = Path.GetFullPath(csprojFilePath);
                if (!File.Exists(csprojFilePath))
                {
                    Console.Out.WriteLine("proj file [{0}] does not exist, create it manually for the first time",
                        csprojFilePath);
                    return -1;
                }
                LogHelpers.LogWithTiming(string.Format("patching proj file [{0}]", csprojFilePath),
                    () =>
                    {
                        var updater = new CsProjectFileUpdater(csprojFilePath, sourcePath);
                        updater.Update();
                    });
            }

            if (!string.IsNullOrEmpty(resultAssemblyFullPath))
                LogHelpers.LogWithTiming(string.Format("compiling [{0}] to assembly [{1}]",
                    sourcePath, resultAssemblyFullPath), () =>
                    {
                        var cSharpCodeProvider = new CSharpCodeProvider();
                        var compilerParameters = new CompilerParameters
                        {
                            OutputAssembly = resultAssemblyFullPath,
                            GenerateExecutable = false,
                            GenerateInMemory = false,
                            IncludeDebugInformation = false
                        };
                        var linqTo1CFilePath = PathHelpers.AppendBasePath("Simple1C.dll");
                        compilerParameters.ReferencedAssemblies.Add(linqTo1CFilePath);
                        var compilerResult = cSharpCodeProvider.CompileAssemblyFromFile(compilerParameters, fileNames);
                        if (compilerResult.Errors.Count > 0)
                        {
                            Console.Out.WriteLine("compile errors");
                            foreach (CompilerError error in compilerResult.Errors)
                            {
                                Console.Out.WriteLine(error);
                                Console.Out.WriteLine("===================");
                            }
                        }
                    });
            return 0;
        }

        private static int RunSql(NameValueCollection parameters)
        {
            var connectionStrings = parameters["connection-strings"];
            var connectionStringsFile = parameters["connection-strings-file"];
            var queryFile = parameters["query-file"];
            var resultConnectionString = parameters["result-connection-string"];
            var dumpSql = parameters["dump-sql"];
            var historyMode = parameters["history-mode"];
            var parametersAreValid =
                (!string.IsNullOrEmpty(connectionStrings) || !string.IsNullOrEmpty(connectionStringsFile)) &&
                !string.IsNullOrEmpty(queryFile) &&
                !string.IsNullOrEmpty(resultConnectionString);
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd run-sql [-connection-strings <1c db connection strings comma delimited> | -connection-strings-file <connection strings with areas>] -query-file <path to file with 1c query> -result-connection-string <where to put results> [-dump-sql true] [-history-mode true]");
                return -1;
            }
            var querySources = string.IsNullOrEmpty(connectionStrings)
                ? StringHelpers.ParseLinesWithTabs(File.ReadAllText(connectionStringsFile),
                    (s, items) => new QuerySource
                    {
                        ConnectionString = s,
                        Areas = items.Select(int.Parse).ToArray()
                    }).ToArray()
                : connectionStrings.Split(',')
                    .Select(x => new QuerySource
                    {
                        ConnectionString = x,
                        Areas = new int[0]
                    }).ToArray();
            var target = new MsSqlDatabase(resultConnectionString);
            var queryText = File.ReadAllText(queryFile);
            var targetTableName = Path.GetFileNameWithoutExtension(queryFile);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var writer = new MsSqlBulkCopyWriter(target, targetTableName, historyMode == "true", 1024);
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = CancellationToken.None,
                    MaxDegreeOfParallelism = querySources.Length
                };
                Sql.Execute(querySources, queryText, writer, parallelOptions, dumpSql == "true");
                stopwatch.Stop();
                Console.Out.WriteLine("\r\ndone, [{0}] millis", stopwatch.ElapsedMilliseconds);
                return 0;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("\r\ndone, [{0}] millis", stopwatch.ElapsedMilliseconds);
                Console.Out.WriteLine("error [{0}]", e);
                return -1;
            }
        }

        private static int TranslateSql(NameValueCollection parameters)
        {
            var connectionString = parameters["connection-string"];
            var queryFile = parameters["query-file"];
            var parametersAreValid = !string.IsNullOrEmpty(connectionString) &&
                                     !string.IsNullOrEmpty(queryFile);
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd translate-sql -connection-string <1c db connection string> -query-file <path to file with 1c query>");
                return -1;
            }
            var db = new PostgreeSqlDatabase(connectionString);
            var mappingSchema = new PostgreeSqlSchemaStore(db);
            var translator = new QueryToSqlTranslator(mappingSchema, new int[0]);
            var query = File.ReadAllText(queryFile);
            var sql = translator.Translate(query);
            Console.Out.WriteLine(sql);
            return 0;
        }

        private static int GenSqlMeta(NameValueCollection parameters)
        {
            var connectionString = parameters["connection-string"];
            var dbConnectionString = parameters["db-connection-string"];
            var parametersAreValid =
                !string.IsNullOrEmpty(connectionString) &&
                !string.IsNullOrEmpty(dbConnectionString);
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd gen-sql-meta -connection-string <string> -db-connection-string <connection string for PostgreeSql db>");
                return -1;
            }
            GlobalContext globalContext = null;
            LogHelpers.LogWithTiming(string.Format("connecting to [{0}]", connectionString),
                () => globalContext = new GlobalContext(new GlobalContextFactory().Create(connectionString)));

            var postgreeSqlDatabase = new PostgreeSqlDatabase(dbConnectionString);
            var postgreeSqlSchemaStore = new PostgreeSqlSchemaStore(postgreeSqlDatabase);
            var schemaCreator = new Simple1CSchemaCreator(postgreeSqlSchemaStore, postgreeSqlDatabase, globalContext);
            schemaCreator.Recreate();
            return 0;
        }

        private static string GetTemporaryDirectoryFullPath()
        {
            var result = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(result);
            return result;
        }
    }
}