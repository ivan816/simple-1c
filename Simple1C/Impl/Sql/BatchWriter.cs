using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using Npgsql;
using Npgsql.Schema;
using Simple1C.Impl.Helpers;
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
                //reader.GetColumnSchema() на алиасы колонок в запросе забивает почему-то
                //reader.GetSchemaTable() какую-то хрень в ColumnSize возвращает
                var npgsqlColumns = reader.GetColumnSchema();
                if (columns != null)
                {
                    if (!CheckColumnsConsistent(npgsqlColumns))
                    {
                        const string messageFormat = "inconsistent columns, original [{0}], current [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            columns.Select(x => x.ColumnName + ":" + x.DataType.FormatName()).JoinStrings(","),
                            npgsqlColumns.Select(x => x.ColumnName + ":" + x.DataType.FormatName()).JoinStrings(",")));
                    }
                    return;
                }
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

        private bool CheckColumnsConsistent(ReadOnlyCollection<NpgsqlDbColumn> npgsqlColumns)
        {
            if (npgsqlColumns.Count != columns.Length)
                return false;
            for (var i = 0; i < columns.Length; i++)
                if (npgsqlColumns[i].DataType != columns[i].DataType)
                    return false;
            return true;
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
            for (var i = 0; i < filledRowsCount; i++)
            {
                var r = rows[i];
                for (var j = 0; j < columns.Length; j++)
                    r[j] = ConvertType(r[j], columns[j].DataType);
            }
            var reader = new InMemoryDataReader(rows, filledRowsCount, columns.Length);
            target.BulkCopy(reader, tableName, columns.Length);
            filledRowsCount = 0;
        }

        private static object ConvertType(object source, Type targetType)
        {
            return source is string && targetType == typeof(decimal)
                ? Convert.ChangeType(((string) source).Replace('.', ','), typeof(decimal))
                : source;
        }
    }
}