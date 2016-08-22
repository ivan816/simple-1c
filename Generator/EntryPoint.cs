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
using Simple1C.Interface;

namespace Generator
{
    public static class EntryPoint
    {
        public static int Main(string[] args)
        {
            var parameters = NameValueCollectionHelpers.ParseCommandLine(args);
            var cmd = parameters["cmd"];
            if (cmd == "gen-sql-meta")
                return GenSqlMeta(parameters);
            if (cmd == "get-cs-meta")
                return GenCsMeta(parameters);
            Console.Out.WriteLine("Invalid arguments");
            Console.Out.WriteLine("Usage: Generator.exe -cmd [gen-sql-meta|gen-cs-meta]");
            return -1;
        }

        private static int GenCsMeta(NameValueCollection parameters)
        {
            var connectionString = parameters["connectionString"];
            var resultAssemblyFullPath = parameters["resultAssemblyFullPath"];
            var namespaceRoot = parameters["namespaceRoot"];
            var scanItems = (parameters["scanItems"] ?? "").Split(',');
            var sourcePath = parameters["sourcePath"];
            var csprojFilePath = parameters["csprojFilePath"];
            var parametersAreValid =
                !string.IsNullOrEmpty(connectionString) &&
                (!string.IsNullOrEmpty(resultAssemblyFullPath) || !string.IsNullOrEmpty(sourcePath)) &&
                !string.IsNullOrEmpty(namespaceRoot) &&
                scanItems.Length > 0;
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd gen-cs-meta -connectionString <string> -target cs [-resultAssemblyFullPath <path>] -namespaceRoot <namespace> -scanItems Справочник.Банки,Документ.СписаниеСРасчетногоСчета [-sourcePath <sourcePath>] [-csprojFilePath]");
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

        private static int GenSqlMeta(NameValueCollection parameters)
        {
            var connectionString = parameters["connectionString"];
            var resultSchemaFileName = parameters["resultSchemaFileName"];
            var parametersAreValid =
                !string.IsNullOrEmpty(connectionString) &&
                !string.IsNullOrEmpty(resultSchemaFileName);
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -cmd gen-sql-meta -connectionString <string> -resultSchemaFileName <file full path>");
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
                        writer.WriteLine(connectionString);
                        for (var i = 0; i < tableMappings.Count; i++)
                        {
                            var tableMapping = tableMappings[i];
                            var queryTableName = tableMapping.GetString("ИмяТаблицы");
                            if (string.IsNullOrEmpty(queryTableName))
                                continue;
                            var configurationName = ConfigurationName.ParseOrNull(queryTableName);
                            if (configurationName != null)
                            {
                                var configurationItem = globalContext.FindByName(configurationName.Value);
                                var descriptor = MetadataHelpers.GetDescriptor(configurationName.Value.Scope);
                                var attributes = MetadataHelpers.GetAttributes(configurationItem.ComObject, descriptor);
                            }

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
                                writer.WriteLine("\t{0} {1} {2}", queryColumnName, dbColumnName);
                            }
                            if ((i + 1)%50 == 0)
                                Console.Out.WriteLine("processed [{0}] from [{1}], {2}%",
                                    i + 1, tableMappings.Count, (double) (i + 1)/tableMappings.Count*100);
                        }
                    }
                });
            return 0;
        }

        private static Dictionary<string, object> GetAttributes(GlobalContext globalContext, string fullname)
        {
            var configurationName = ConfigurationName.ParseOrNull(fullname);
            if (configurationName == null)
                return null;
            var configurationItem = globalContext.FindByName(configurationName.Value);
            var descriptor = MetadataHelpers.GetDescriptor(configurationName.Value.Scope);
            var attributes = MetadataHelpers.GetAttributes(configurationItem.ComObject, descriptor);
            return attributes.ToDictionary(Call.Имя);
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