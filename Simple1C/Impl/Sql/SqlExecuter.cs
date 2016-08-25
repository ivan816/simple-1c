using System;
using System.Data;
using System.IO;
using Npgsql;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    public class SqlExecuter
    {
        private readonly MappingSchema mappingSchema;
        private readonly string connectionString;
        private readonly MssqlDatabase resultDatabase;
        private readonly string logFilePath;

        public SqlExecuter(MappingSchema mappingSchema, string connectionString, string resultConnectionString)
        {
            this.mappingSchema = mappingSchema;
            this.connectionString = connectionString;
            resultDatabase = new MssqlDatabase(resultConnectionString);
            logFilePath = Path.GetFullPath("log");
            Console.Out.WriteLine("logs [{0}]", logFilePath);
        }

        public void Execute(string queryFileName)
        {
            var sqlTranslator = new SqlTranslator();
            var sql = sqlTranslator.Translate(mappingSchema, File.ReadAllText(queryFileName));
            var tableName = Path.GetFileNameWithoutExtension(queryFileName);
            WriteLog("[{0}] translation\r\n{1}", tableName, sql);
            var dataTable = new DataTable(tableName);
            using (var npgsqlConnection = new NpgsqlConnection(connectionString))
            using (var cmd = new NpgsqlCommand(sql, npgsqlConnection))
            {
                cmd.AllResultTypesAreUnknown = true;
                using (var adapter = new NpgsqlDataAdapter(cmd))
                {
                    npgsqlConnection.Open();
                    adapter.Fill(dataTable);
                }
            }
            if (resultDatabase.TableExists(tableName))
                resultDatabase.DropTable("dbo." + tableName);
            resultDatabase.CreateTable(dataTable);
            resultDatabase.BulkCopy(dataTable);
        }

        private void WriteLog(string message, params object[] args)
        {
            File.AppendAllText(logFilePath, string.Format(message, args));
        }
    }
}