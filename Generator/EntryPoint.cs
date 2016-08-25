using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Simple1C.Impl;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Queries;
using Simple1C.Impl.Sql;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Interface;

namespace Generator
{
    public static class EntryPoint
    {
        public static int Main(string[] args)
        {
            var parameters = NameValueCollectionHelpers.ParseCommandLine(args);
            var cmd = parameters["cmd"];
            if (cmd == "gen-cs-meta")
                return GenCsMeta(parameters);
            if (cmd == "gen-sql-meta")
                return GenSqlMeta(parameters);
            if (cmd == "run-sql")
                return RunSql(parameters);
            Console.Out.WriteLine("Invalid arguments");
            Console.Out.WriteLine("Usage: Generator.exe -cmd [gen-cs-meta|gen-sql-meta|run-sql]");
            return -1;
        }

        private static int GenCsMeta(NameValueCollection parameters)
        {
            var connectionString = parameters["connection-string"];
            var resultAssemblyFullPath = parameters["result-assembly-full-path"];
            var namespaceRoot = parameters["namespace-root"];
            var scanItems = (parameters["scan-items"] ?? "").Split(',');
            var sourcePath = parameters["source-path"];
            var csprojFilePath = parameters["csproj-file-spath"];
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
            ExecuteAction(string.Format("connecting to [{0}]", connectionString),
                () => globalContext = new GlobalContextFactory().Create(connectionString));

            sourcePath = sourcePath ?? GetTemporaryDirectoryFullPath();
            if (Directory.Exists(sourcePath))
                Directory.Delete(sourcePath, true);
            string[] fileNames = null;
            ExecuteAction(string.Format("generating code into [{0}]", sourcePath),
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
                ExecuteAction(string.Format("patching proj file [{0}]", csprojFilePath),
                    () =>
                    {
                        var updater = new CsProjectFileUpdater(csprojFilePath, sourcePath);
                        updater.Update();
                    });
            }

            if (!string.IsNullOrEmpty(resultAssemblyFullPath))
                ExecuteAction(string.Format("compiling [{0}] to assembly [{1}]", sourcePath, resultAssemblyFullPath),
                    () =>
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
            var metaFile = parameters["meta-file"];
            var connectionStrings = parameters["connection-strings"];
            var queryFile = parameters["query-file"];
            var resultConnectionString = parameters["result-connection-string"];
            var parametersAreValid =
                !string.IsNullOrEmpty(metaFile) &&
                !string.IsNullOrEmpty(connectionStrings) &&
                !string.IsNullOrEmpty(queryFile) &&
                !string.IsNullOrEmpty(resultConnectionString);
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd run-sql -meta-files <path to meta file> -connection-strings <1c db connection strings comma delimited> -query-file <path to file with 1c query> -result-connection-string <where to put results>");
                return -1;
            }
            var sources = connectionStrings.Split(',')
                .Select(x => new PostgreeSqlDatabase(x))
                .ToArray();
            var target = new MsSqlDatabase(resultConnectionString);
            var sqlExecuter = new SqlExecuter(sources, target, queryFile);
            sqlExecuter.Execute();
            return 0;
        }

        private static int GenSqlMeta(NameValueCollection parameters)
        {
            var connectionString = parameters["connection-string"];
            var resultSchemaFileName = parameters["result-schema-file-name"];
            var parametersAreValid =
                !string.IsNullOrEmpty(connectionString) &&
                !string.IsNullOrEmpty(resultSchemaFileName);
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd gen-sql-meta -connection-string <string> -result-schema-file-name <file full path>");
                return -1;
            }
            GlobalContext globalContext = null;
            ExecuteAction(string.Format("connecting to [{0}]", connectionString),
                () => globalContext = new GlobalContext(new GlobalContextFactory().Create(connectionString)));

            object comTable = null;
            ExecuteAction("loading schema info",
                () => comTable = ComHelpers.Invoke(globalContext.ComObject(), "ПолучитьСтруктуруХраненияБазыДанных"));

            ExecuteAction(string.Format("dumping schema into [{0}]", resultSchemaFileName),
                () =>
                {
                    var tableMappings = new ValueTable(comTable);
                    using (var writer = new StreamWriter(resultSchemaFileName))
                    {
                        for (var i = 0; i < tableMappings.Count; i++)
                        {
                            var tableMapping = tableMappings[i];
                            var queryTableName = tableMapping.GetString("ИмяТаблицы");
                            if (string.IsNullOrEmpty(queryTableName))
                                continue;
                            var attributes = GetAttributes(globalContext, queryTableName);
                            var dbTableName = tableMapping.GetString("ИмяТаблицыХранения");
                            if (string.IsNullOrEmpty(dbTableName))
                                continue;
                            writer.WriteLine("{0} {1}", queryTableName, dbTableName);
                            var colunMappings = new ValueTable(tableMapping["Поля"]);
                            for (var j = 0; j < colunMappings.Count; j++)
                            {
                                var columnMapping = colunMappings.Get(j);
                                var queryColumnName = columnMapping.GetString("ИмяПоля");
                                if (string.IsNullOrEmpty(queryColumnName))
                                    continue;
                                var dbColumnName = columnMapping.GetString("ИмяПоляХранения");
                                if (string.IsNullOrEmpty(dbColumnName))
                                    continue;
                                var attribute = attributes == null ? null : attributes.GetOrDefault(queryColumnName);
                                var typename = attribute == null ? null : attribute();
                                writer.WriteLine("\t{0} {1}{2}", queryColumnName, dbColumnName,
                                    string.IsNullOrEmpty(typename) ? "" : " " + typename);
                            }
                            if ((i + 1)%50 == 0)
                                Console.Out.WriteLine("processed [{0}] from [{1}], {2}%",
                                    i + 1, tableMappings.Count, (double) (i + 1)/tableMappings.Count*100);
                        }
                    }
                });
            return 0;
        }

        private static readonly Dictionary<string, string> simpleTypesMap = new Dictionary<string, string>
        {
            {"Строка", "string"},
            {"Булево", "bool"},
            {"Дата", "DateTime?"},
            {"Уникальный идентификатор", "Guid?"},
            {"Хранилище значения", null},
            {"Описание типов", "Type[]"}
        };

        private static Dictionary<string, Func<string>> GetAttributes(GlobalContext globalContext, string fullname)
        {
            var configurationName = ConfigurationName.ParseOrNull(fullname);
            if (configurationName == null)
                return null;
            if (!configurationName.Value.HasReference)
                return null;
            var configurationItem = globalContext.FindByName(configurationName.Value);
            var descriptor = MetadataHelpers.GetDescriptor(configurationName.Value.Scope);
            var attributes = MetadataHelpers.GetAttributes(configurationItem.ComObject, descriptor);
            return attributes.ToDictionary(Call.Имя, delegate(object o)
            {
                Func<string> result = delegate
                {
                    var type = ComHelpers.GetProperty(o, "Тип");
                    var typesObject = ComHelpers.Invoke(type, "Типы");
                    var typesCount = Call.Количество(typesObject);
                    if (typesCount != 1)
                        return null;
                    var typeObject = Call.Получить(typesObject, 0);
                    var stringPresentation = globalContext.String(typeObject);
                    if (simpleTypesMap.ContainsKey(stringPresentation))
                        return null;
                    var comObject = Call.НайтиПоТипу(globalContext.Metadata, typeObject);
                    if (comObject == null)
                        return null;
                    return Call.ПолноеИмя(comObject);
                };
                return result;
            });
        }

        public static string GetTemporaryDirectoryFullPath()
        {
            var result = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(result);
            return result;
        }

        private static void ExecuteAction(string description, Action action)
        {
            Console.Out.WriteLine(description);
            var s = Stopwatch.StartNew();
            action();
            s.Stop();
            Console.Out.WriteLine("done, took [{0}] millis", s.ElapsedMilliseconds);
        }
    }
}