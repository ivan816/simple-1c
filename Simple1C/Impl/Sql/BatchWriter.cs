using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Npgsql;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    internal class BatchWriter : IDisposable
    {
        private readonly object lockObject = new object();
        private readonly List<object[]> rows = new List<object[]>();
        private DataColumn[] columns;
        private int filledRowsCount;

        private readonly MsSqlDatabase target;
        private readonly string tableName;
        private readonly int batchSize;

        public BatchWriter(MsSqlDatabase target, string tableName, int batchSize)
        {
            this.target = target;
            this.tableName = tableName;
            this.batchSize = batchSize;
        }

        public void EnsureTable(DbDataReader dbReader)
        {
            var reader = (NpgsqlDataReader) dbReader;
            lock (lockObject)
            {
                if (columns != null)
                    return;
                //reader.GetColumnSchema() на алиасы колонок в запросе забивает почему-то
                var schemaTable = reader.GetSchemaTable();

                if (schemaTable == null)
                    throw new InvalidOperationException("assertion failure");
                columns = schemaTable.Rows
                    .Cast<DataRow>()
                    .Select(r => new DataColumn
                    {
                        ColumnName = (string) r["ColumnName"],
                        AllowDBNull = (bool) r["AllowDBNull"],
                        DataType = (Type) r["DataType"],

                        //ебнутый Npgsql на четыре символа меньше возрващает почему-то
                        MaxLength = (int) r["ColumnSize"] + 4
                    })
                    .ToArray();
                if (target.TableExists(tableName))
                    target.DropTable("dbo." + tableName);
                target.CreateTable(tableName, columns);
            }
        }

        public void InsertRow(DbDataReader dbReader)
        {
            var reader = (NpgsqlDataReader) dbReader;
            lock (lockObject)
            {
                var currentRowIndex = filledRowsCount;
                object[] rowData;
                if (currentRowIndex < rows.Count)
                    rowData = rows[currentRowIndex];
                else
                    rows.Add(rowData = new object[columns.Length]);
                reader.GetValues(rowData);
                filledRowsCount++;
                if (filledRowsCount == batchSize)
                    Flush();
            }
        }

        public void Dispose()
        {
            if (filledRowsCount > 0)
                Flush();
        }

        private void Flush()
        {
            var reader = new InMemoryDataReader(rows, filledRowsCount, columns.Length);
            target.BulkCopy(reader, tableName, columns.Length);
            filledRowsCount = 0;
        }
    }
}