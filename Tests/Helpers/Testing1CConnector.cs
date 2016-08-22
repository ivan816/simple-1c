using System;
using System.IO;
using Simple1C.Impl;
using Simple1C.Interface;

namespace Simple1C.Tests.Helpers
{
    internal static class Testing1CConnector
    {
        private static GlobalContext globalContext;
        private static GlobalContext tempGlobalContext;
        private static readonly string defaultDatabaseFullPath = Path.GetFullPath("base");
        private static readonly string tempDatabaseFullPath = Path.GetFullPath("temp-base");
        private const string etalonDatabaseFullPath = @"\\host\dev\testBases\houseStark";
        private const string etalonDatabaseLocalCacheFullPath = @"c:\testBases\houseStark";

        static Testing1CConnector()
        {
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                if (globalContext != null)
                    globalContext.Dispose();
                if (tempGlobalContext != null)
                    tempGlobalContext.Dispose();
            };
        }

        public static GlobalContext GetDefaultGlobalContext()
        {
            if (globalContext == null)
            {
                SyncDbDataWithEtalon(defaultDatabaseFullPath);
                globalContext = OpenContext(defaultDatabaseFullPath);
            }
            return globalContext;
        }

        public static GlobalContext GetTempGlobalContext(bool resetData)
        {
            if (tempGlobalContext != null)
            {
                tempGlobalContext.Dispose();
                tempGlobalContext = null;
            }
            if (resetData)
            {
                if (Directory.Exists(tempDatabaseFullPath))
                    Directory.Delete(tempDatabaseFullPath, true);
                SyncDbDataWithEtalon(tempDatabaseFullPath);
            }
            return tempGlobalContext = OpenContext(tempDatabaseFullPath);
        }

        private static GlobalContext OpenContext(string databaseFullPath)
        {
            ProcessesHelpers.KillOwnProcessesByName("1cv8.exe");
            var connectionStringBuilder = new ConnectionStringBuilder
            {
                Type = Connection1CType.File,
                FileLocation = databaseFullPath,
                User = "Администратор",
                Password = ""
            };
            var globalContextFactory = new GlobalContextFactory();
            var globalContextComObject = globalContextFactory.Create(connectionStringBuilder.GetConnectionString());
            return new GlobalContext(globalContextComObject);
        }

        private static void SyncDbDataWithEtalon(string databaseFullPath)
        {
            Robocopy.Execute(etalonDatabaseFullPath, etalonDatabaseLocalCacheFullPath, true);
            Robocopy.Execute(etalonDatabaseLocalCacheFullPath, databaseFullPath, false);
        }
    }
}