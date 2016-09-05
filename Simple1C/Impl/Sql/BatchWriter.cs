using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
                //reader.GetSchemaTable() какую-то хрень в ColumnSize возвращает
                //reader.GetColumnSchema().AllowDBNull пиздит (возвращает false, а потом DBNull.Value приезжает)
                var npgsqlColumns = reader.GetColumnSchema();
                columns = new DataColumn[npgsqlColumns.Count];
                for (var i = 0; i < columns.Length; i++)
                {
                    var c = npgsqlColumns[i];
                    columns[i] = new DataColumn
                    {
                        ColumnName = reader.GetName(i),
                        AllowDBNull = true,
                        DataType = c.DataType,
                        MaxLength = c.ColumnSize.GetValueOrDefault(-1)
                    };
                }
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