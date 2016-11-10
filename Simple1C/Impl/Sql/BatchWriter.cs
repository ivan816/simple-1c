using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using Npgsql;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Interface.Sql;

namespace Simple1C.Impl.Sql
{
    internal class BatchWriter : IDisposable
    {
        private readonly object lockObject = new object();
        private readonly List<object[]> rows = new List<object[]>();
        private int filledRowsCount;
        private readonly IBatchWriter writer;
        private readonly int batchSize;
        private DataColumn[] columns;

        public BatchWriter(IBatchWriter writer, int batchSize)
        {
            this.writer = writer;
            this.batchSize = batchSize;
        }

        public void HandleNewDataSource(DbDataReader dbReader)
        {
            var reader = (NpgsqlDataReader)dbReader;
            lock (lockObject)
            {
                columns = PostgreeSqlDatabase.GetColumns(reader);
                writer.HandleNewDataSource(columns);
            }
        }

        public void InsertRow(DbDataReader dbReader)
        {
            var reader = (NpgsqlDataReader)dbReader;
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
            {
                Flush();
                writer.Dispose();
            }
        }

        private void Flush()
        {
            for (var i = 0; i < filledRowsCount; i++)
            {
                var r = rows[i];
                for (var j = 0; j < columns.Length; j++)
                    r[j] = ConvertType(r[j], columns[j]);
            }
            writer.RowsCache(rows, filledRowsCount);
            filledRowsCount = 0;
        }

        private static readonly DateTime minSqlDate = new DateTime(1753, 1, 1);

        private static object ConvertType(object source, DataColumn column)
        {
            if (!(source is string))
                return source;
            if (column.DataType == typeof(decimal))
                return Convert.ChangeType(((string)source).Replace('.', ','), typeof(decimal));
            if (column.DataType == typeof(bool))
                return ((string)source).EqualsIgnoringCase("t");
            if (column.DataType == typeof(DateTime))
            {
                DateTime dateTime;
                if (!TryParseDate((string)source, out dateTime))
                {
                    const string messageFormat = "can't parse datetime from [{0}] for column [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        source, column.ColumnName));
                }
                return dateTime < minSqlDate ? (object)null : dateTime;
            }
            return source;
        }

        private static bool TryParseDate(string s, out DateTime result)
        {
            return DateTime.TryParseExact(s, "yyyy-MM-dd", null, DateTimeStyles.None, out result) ||
                   DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out result);
        }
    }
}