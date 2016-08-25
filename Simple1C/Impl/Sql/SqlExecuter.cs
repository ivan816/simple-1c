using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    public class SqlExecuter
    {
        private readonly PostgreeSqlDatabase[] sources;
        private readonly MsSqlDatabase target;
        private readonly string logFilePath;
        private readonly object lockObject = new object();
        private readonly List<object[]> writeBatch = new List<object[]>();
        private readonly bool isFirstWrite = true;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private DataColumn[] columns;
        private int rowsCount;
        private const int batchSize = 1000;

        public SqlExecuter(PostgreeSqlDatabase[] sources, MsSqlDatabase target, string queryFileName)
        {
            this.sources = sources;
            this.target = target;
            logFilePath = Path.GetFullPath("log");
            Console.Out.WriteLine("logs [{0}]", logFilePath);
            queryText = File.ReadAllText(queryFileName);
            tableName = Path.GetFileNameWithoutExtension(queryFileName);
        }

        public void Execute()
        {
            var tasks = new Task[sources.Length];
            for (var i = 0; i < tasks.Length; i++)
            {
                var source = sources[i];
                tasks[i] = Task.Run(async delegate
                {
                    try
                    {
                        var mappingSchema = new PostgreeSqlSchemaStore(source);
                        var translator = new QueryToSqlTranslator(mappingSchema);
                        var sql = translator.Translate(queryText);
                        WriteLog("[{0}] translation\r\n{1}", tableName, sql);
                        using (var connection = new NpgsqlConnection(source.ConnectionString))
                        using (var command = new NpgsqlCommand(sql, connection))
                        {
                            command.AllResultTypesAreUnknown = true;
                            await connection.OpenAsync(cancellation.Token);
                            using (var reader = (NpgsqlDataReader) await ExecuteReader(command))
                                while (await reader.ReadAsync(cancellation.Token))
                                    HandleRow(reader);
                        }
                    }
                    catch (Exception e)
                    {
                        if (e.IsCancellation())
                            return;
                        WriteLog("error for [{0}]\r\n{1}", source.ConnectionString, e);
                        cancellation.Cancel();
                    }
                }, cancellation.Token);
            }
        }

        private void HandleRow(NpgsqlDataReader reader)
        {
            lock (lockObject)
            {
                if (columns == null)
                    columns = reader.GetColumnSchema()
                        .Select(column => new DataColumn
                        {
                            ColumnName = column.ColumnName,
                            AllowDBNull = column.AllowDBNull.GetValueOrDefault(),
                            DataType = column.DataType,
                            MaxLength = column.ColumnSize.GetValueOrDefault(-1)
                        })
                        .ToArray();
                var currentRowIndex = rowsCount;
                object[] rowData;
                if (currentRowIndex < writeBatch.Count)
                    rowData = writeBatch[currentRowIndex];
                else
                    writeBatch.Add(rowData = new object[columns.Length]);
                reader.GetValues(rowData);
                rowsCount++;
                if (rowsCount == batchSize)
                {
                    if (isFirstWrite)
                    {
                        if (target.TableExists(tableName))
                            target.DropTable("dbo." + tableName);
                        target.CreateTable(tableName, columns);
                    }
                    target.BulkCopy(new InMemoryDataReader(writeBatch, rowsCount, columns.Length),
                        tableName, columns);
                }
            }
        }

        private Task<DbDataReader> ExecuteReader(NpgsqlCommand command)
        {
            return command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellation.Token);
        }

        private readonly object logLock = new object();
        private readonly string queryText;
        private readonly string tableName;

        private void WriteLog(string message, params object[] args)
        {
            lock (logLock)
                File.AppendAllText(logFilePath, string.Format(message, args));
        }
    }
}