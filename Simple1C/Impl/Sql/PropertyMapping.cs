using System;

namespace Simple1C.Impl.Sql
{
    internal class PropertyMapping
    {
        public PropertyMapping(string propertyName, string columnName, string nestedTableName)
        {
            PropertyName = propertyName;
            ColumnName = columnName;
            NestedTableName = nestedTableName;
        }

        public string Serialize()
        {
            var result = PropertyName + " " + ColumnName;
            return string.IsNullOrEmpty(NestedTableName)
                ? result
                : result + " " + NestedTableName;
        }

        public static PropertyMapping Parse(string s)
        {
            var columnDesc = s.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (columnDesc.Length != 2 && columnDesc.Length != 3)
                throw new InvalidOperationException(string.Format("can't parse line [{0}]", s));
            return new PropertyMapping(columnDesc[0],
                columnDesc[1],
                columnDesc.Length == 3 ? columnDesc[2] : null);
        }

        public string PropertyName { get; private set; }
        public string ColumnName { get; private set; }
        public string NestedTableName { get; private set; }
    }
}