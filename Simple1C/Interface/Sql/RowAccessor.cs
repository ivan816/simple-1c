using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Npgsql;
using Simple1C.Impl.Helpers;

namespace Simple1C.Interface.Sql
{
    public class RowAccessor
    {
        internal NpgsqlDataReader Reader { get; set; }
        internal DataColumn[] Columns { get; private set; }
        private readonly Dictionary<string, int> nameToIndexMap;
        private static readonly DateTime minSqlDate = new DateTime(1753, 1, 1);

        public RowAccessor(DataColumn[] columns)
        {
            Columns = columns;
            nameToIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Columns.Length; i++)
                nameToIndexMap.Add(Columns[i].ColumnName, i);
        }

        public object GetValue(int index)
        {
            return ConvertType(Reader.GetValue(index), Columns[index]);
        }

        public object GetValue(string name)
        {
            int index;
            if (nameToIndexMap.TryGetValue(name, out index))
                return GetValue(index);
            const string messageFormat = "invalid column name [{0}]";
            throw new InvalidOperationException(string.Format(messageFormat, name));
        }

        public void GetValues(object[] target)
        {
            Reader.GetValues(target);
            for (var i = 0; i < target.Length; i++)
                target[i] = ConvertType(target[i], Columns[i]);
        }

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