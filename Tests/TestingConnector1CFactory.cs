using System;
using System.IO;
using Tests.Helpers;

namespace Tests
{
    public static class TestingConnector1CFactory
    {
        static TestingConnector1CFactory()
        {
            ProcessesHelpers.KillOwnProcessesByName("1cv8.exe");
            ProcessesHelpers.KillOwnProcessesByName("dllhost.exe");
        }

        public static Lazy<Connector1C> Lazy(string databasePath, string targetFolderName)
        {
            return new Lazy<Connector1C>(() => Create(databasePath, targetFolderName));
        }

        public static Connector1C Create(string etalonDatabasePath, string targetFolderName)
        {
            var directoryName = PathHelpers.GetFileName(etalonDatabasePath);
            var localMachineDatabaseCachePath = Path.Combine("c:\\testBases", directoryName);
            var testDatabasePath = Path.GetFullPath(targetFolderName);
            Robocopy.Execute(etalonDatabasePath, localMachineDatabaseCachePath, true);
            Robocopy.Execute(localMachineDatabaseCachePath, testDatabasePath, false);
            var database = new XmlDatabaseDescription
            {
                Type = "file",
                Location = testDatabasePath,
                User = "јдминистратор",
                Password = ""
            };

            //разобрать эту уп€чку, замкнуть на контейнер
            var logger = LogManager.GetLogger("root");
            var retry1CActionService = new Retry1CActionService(logger);
            var globalContextFactory = new GlobalContextFactory(retry1CActionService);
            var connector = new Connector1C(new GlobalContextActivator(globalContextFactory), retry1CActionService);
            var databaseUsage = connector.Use(database);
            AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
            {
                databaseUsage.Dispose();
                globalContextFactory.Dispose();
                connector = null;
            };

            return connector;
        }
    }
}