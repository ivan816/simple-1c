using System.IO;
using Simple1C.Interface;

namespace Simple1C.Tests.Helpers
{
    public static class Testing1CConnector
    {
        public static object Create(string etalonDatabasePath, string targetFolderName)
        {
            ProcessesHelpers.KillOwnProcessesByName("1cv8.exe");
            var testDatabasePath = Path.GetFullPath(targetFolderName);
            var connectionStringBuilder = new ConnectionStringBuilder
            {
                Type = Connection1CType.File,
                FileLocation = testDatabasePath,
                User = "Администратор",
                Password = ""
            };
            var globalContextFactory = new GlobalContextFactory();
            return globalContextFactory.Create(connectionStringBuilder.GetConnectionString());
        }
    }
}