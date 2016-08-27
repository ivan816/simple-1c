using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using Npgsql;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    internal class QueryExecuter
    {
        private readonly PostgreeSqlDatabase[] sources;
        private readonly MsSqlDatabase target;
        private readonly string logFilePath;
        private readonly object lockObject = new object();
        private DataColumn[] columns;
        private const int batchSize = 1000;
        private readonly List<object[]> writeBatch = new List<object[]>();
        private volatile bool errorOccured;
        private int filledRowsCountInBatch;

        public QueryExecuter(PostgreeSqlDatabase[] sources, MsSqlDatabase target, string queryFileName)
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
            var sourceThreads = new Thread[sources.Length];
            for (var i = 0; i < sourceThreads.Length; i++)
            {
                var source = sources[i];
                sourceThreads[i] = new Thread(delegate(object _)
                {
                    try
                    {
                        var mappingSchema = new PostgreeSqlSchemaStore(source);
                        var translator = new QueryToSqlTranslator(mappingSchema);
                        var sql = translator.Translate(queryText);
                        WriteLog("[{0}] translation for [{1}]\r\n{2}",
                            tableName, source.ConnectionString, sql);
                        source.ExecuteReader(sql, new object[0], delegate(DbDataReader reader)
                        {
                            if (errorOccured)
                                throw new OperationCanceledException();
                            HandleRow(reader);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        errorOccured = true;
                        WriteLog("error for [{0}]\r\n{1}", source.ConnectionString, e);
                    }
                });
                sourceThreads[i].Start();
            }
            foreach (var t in sourceThreads)
                t.Join();
            Console.Out.WriteLine("done");
        }

        private void HandleRow(DbDataReader dbReader)
        {
            var reader = (NpgsqlDataReader) dbReader;
            lock (lockObject)
            {
                if (columns == null)
                {
                    columns = reader.GetColumnSchema()
                        .Select(column => new DataColumn
                        {
                            ColumnName = column.ColumnName,
                            AllowDBNull = column.AllowDBNull.GetValueOrDefault(),
                            DataType = column.DataType,
                            MaxLength = column.ColumnSize.GetValueOrDefault(-1)
                        })
                        .ToArray();
                    if (target.TableExists(tableName))
                        target.DropTable("dbo." + tableName);
                    target.CreateTable(tableName, columns);
                }
                var currentRowIndex = filledRowsCountInBatch;
                object[] rowData;
                if (currentRowIndex < writeBatch.Count)
                    rowData = writeBatch[currentRowIndex];
                else
                    writeBatch.Add(rowData = new object[columns.Length]);
                reader.GetValues(rowData);
                filledRowsCountInBatch++;
                if (filledRowsCountInBatch == batchSize)
                    target.BulkCopy(new InMemoryDataReader(writeBatch, filledRowsCountInBatch, columns.Length),
                        tableName, columns);
            }
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