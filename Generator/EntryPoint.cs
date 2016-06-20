using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Simple1C.Impl.Generation;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;

namespace Generator
{
    public static class EntryPoint
    {
        public static int Main(string[] args)
        {
            var parameters = NameValueCollectionHelpers.ParseCommandLine(args);
            var connectionString = parameters["connectionString"];
            var resultAssemblyFullPath = parameters["resultAssemblyFullPath"];
            var namespaceRoot = parameters["namespaceRoot"];
            var scanItems = (parameters["scanItems"] ?? "").Split(',');
            var sourcePath = parameters["sourcePath"];
            var parametersAreValid =
                !string.IsNullOrEmpty(connectionString) &&
                !string.IsNullOrEmpty(resultAssemblyFullPath) &&
                !string.IsNullOrEmpty(namespaceRoot) &&
                scanItems.Length > 0;
            if (!parametersAreValid)
            {
                Console.Out.WriteLine("Invalid arguments");
                Console.Out.WriteLine(
                    "Usage: Generator.exe -connectionString <string> [-resultAssemblyFullPath <path>] -namespaceRoot <namespace> -scanItems Справочник.Банки,Документ.СписаниеСРасчетногоСчета [-sourcePath <sourcePath>]");
                return -1;
            }

            object globalContext = null;
            ExecuteAction(string.Format("connecting to [{0}]", connectionString),
                () => globalContext = new GlobalContextFactory().Create(connectionString));

            sourcePath = sourcePath ?? GetTemporaryDirectoryFullPath();
            string[] fileNames = null;
            ExecuteAction(string.Format("generating code into [{0}]", sourcePath),
                () =>
                {
                    var generator = new ObjectModelGenerator(globalContext,
                        scanItems, namespaceRoot, sourcePath);
                    fileNames = generator.Generate().ToArray();
                });

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
                            IncludeDebugInformation = true
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