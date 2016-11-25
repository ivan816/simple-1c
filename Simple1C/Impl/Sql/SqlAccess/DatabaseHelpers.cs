using System;
using System.Data;
using System.Linq;
using Npgsql;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal static class DatabaseHelpers
    {
        public static DataColumn[] GetColumns(NpgsqlDataReader reader)
        {
            //reader.GetColumnSchema() на алиасы колонок в запросе забивает почему-то
            //reader.GetSchemaTable() какую-то хрень в ColumnSize возвращает

            var npgsqlColumns = reader.GetColumnSchema();
            var result = new DataColumn[npgsqlColumns.Count];
            for (var i = 0; i < result.Length; i++)
            {
                var c = npgsqlColumns[i];
                var columnName = reader.GetName(i);
                if (string.IsNullOrEmpty(columnName) || columnName == "?column?")
                    columnName = "col_" + i;
                result[i] = new DataColumn
                {
                    ColumnName = columnName,
                    AllowDBNull = true,
                    DataType = c.DataTypeName == "bytea" ? typeof(byte[]) : c.DataType,
                    MaxLength = c.ColumnSize.GetValueOrDefault(-1),
                };
            }
            return result;
        }

        public static void CheckColumnsAreEqual(DataColumn[] a, string aName, DataColumn[] b, string bName)
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
    }
}