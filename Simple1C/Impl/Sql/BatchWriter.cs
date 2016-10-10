using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using Npgsql;
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
        private readonly bool historyMode;

        public BatchWriter(MsSqlDatabase target, string tableName, int batchSize, bool historyMode)
        {
            this.target = target;
            this.tableName = tableName;
            this.batchSize = batchSize;
            this.historyMode = historyMode;
        }

        public void EnsureTable(DbDataReader dbReader)
        {
            var reader = (NpgsqlDataReader) dbReader;
            lock (lockObject)
            {
                var readerColumns = PostgreeSqlDatabase.GetColumns(reader);
                if (columns != null)
                {
                    CheckColumns(columns, "original", readerColumns, "current");
                    return;
                }
                columns = readerColumns;
                if (historyMode)
                {
                    if (!target.TableExists(tableName))
                        target.CreateTable(tableName, columns);
                    else
                    {
                        var existingColumns = target.GetColumns(tableName);
                        CheckColumns(existingColumns, "existing", columns, "new");
                    }
                }
                else
                {
                    if (target.TableExists(tableName))
                        target.DropTable("dbo." + tableName);
                    target.CreateTable(tableName, columns);
                }
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

        private static void CheckColumns(DataColumn[] a, string aName, DataColumn[] b, string bName)
        {
            var aFormat = FormatColumns(a);
            var bFormat = FormatColumns(b);
            if (aFormat == bFormat)
                return;
            const string messageFormat = "inconsistent columns {0}(first) and {1}(second):\r\n{2}\r\n{3}\r\n";
            throw new InvalidOperationException(string.Format(messageFormat,
                aName, bName, aFormat, bFormat));
        }

        private static string FormatColumns(DataColumn[] c)
        {
            return c.Select(FormatColumn).JoinStrings(",");
        }

        private static string FormatColumn(DataColumn c)
        {
            var lengthSpec = "";
            if (c.DataType == typeof(string))
            {
                var maxLength = c.MaxLength == -1 ? 1000 : c.MaxLength;
                lengthSpec = "[" + maxLength + "]";
            }
            return string.Format("{0}:{1}{2}", c.ColumnName, c.DataType.FormatName(), lengthSpec);
        }

        private void Flush()
        {
            for (var i = 0; i < filledRowsCount; i++)
            {
                var r = rows[i];
                for (var j = 0; j < columns.Length; j++)
                    r[j] = ConvertType(r[j], columns[j]);
            }
            var reader = new InMemoryDataReader(rows, filledRowsCount, columns.Length);
            target.BulkCopy(reader, tableName, columns.Length);
            filledRowsCount = 0;
        }

        private static readonly DateTime minSqlDate = new DateTime(1753, 1, 1);

        private static object ConvertType(object source, DataColumn column)
        {
            if (!(source is string))
                return source;
            if (column.DataType == typeof(decimal))
                return Convert.ChangeType(((string) source).Replace('.', ','), typeof(decimal));
            if (column.DataType == typeof(bool))
                return ((string) source).EqualsIgnoringCase("t");
            if (column.DataType == typeof(DateTime))
            {
                DateTime dateTime;
                if (!TryParseDate((string) source, out dateTime))
                {
                    const string messageFormat = "can't parse datetime from [{0}] for column [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        source, column.ColumnName));
                }
                return dateTime < minSqlDate ? (object) null : dateTime;
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