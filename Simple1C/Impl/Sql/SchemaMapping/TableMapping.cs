using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal class TableMapping
    {
        public string QueryTableName { get; private set; }
        public string DbTableName { get; private set; }
        public int Index { get; private set; }
        public TableType Type { get; private set; }
        public PropertyMapping[] Properties { get; private set; }
        private ConfigurationName? objectName;
        private bool objectNameParsed;

        public ConfigurationName? ObjectName
        {
            get
            {
                if (objectNameParsed)
                    return objectName;
                objectNameParsed = true;
                return objectName = ConfigurationName.ParseOrNull(QueryTableName);
            }
        }

        private readonly Dictionary<string, PropertyMapping> byPropertyName =
            new Dictionary<string, PropertyMapping>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex tableIndexRegex = new Regex(@"(\d+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public TableMapping(string queryTableName, string dbName, TableType type, PropertyMapping[] properties)
        {
            QueryTableName = queryTableName;
            DbTableName = dbName;
            Type = type;
            Properties = properties;
            var indexMatch = tableIndexRegex.Match(dbName);
            if (!indexMatch.Success)
            {
                const string messageFormat = "can't extract table index, queryTableName [{0}], dbName [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, queryTableName, dbName));
            }
            Index = int.Parse(indexMatch.Groups[1].Value);
            foreach (var p in Properties)
                if (!byPropertyName.ContainsKey(p.PropertyName))
                {
                    //какие-то дурацкие дубли с полем
                    //сумма в РегистрБухгалтерии.Хозрасчетный.Остатки, забил пока
                    byPropertyName.Add(p.PropertyName, p);
                    //const string messageFormat = "property [{0}] for table [{1}] already exist";
                    //throw new InvalidOperationException(string.Format(messageFormat, p.PropertyName, QueryName));
                }
        }

        public static TableType ParseTableType(string s)
        {
            return (TableType) Enum.Parse(typeof(TableType), s);
        }

        public bool HasProperty(string queryName)
        {
            return byPropertyName.ContainsKey(queryName);
        }

        public PropertyMapping GetByPropertyNameOrNull(string queryName)
        {
            return byPropertyName.GetOrDefault(queryName);
        }

        public PropertyMapping GetByPropertyName(string queryName)
        {
            var result = GetByPropertyNameOrNull(queryName);
            if (result != null)
                return result;
            const string messagFormat = "can't find field [{0}] for table [{1}]";
            throw new InvalidOperationException(string.Format(messagFormat, queryName, QueryTableName));
        }

        public bool IsEnum()
        {
            return ObjectName.HasValue && ObjectName.Value.Scope == ConfigurationScope.Перечисления;
        }

        public static string GetMainQueryNameByTableSectionQueryName(string s)
        {
            var lastDot = s.LastIndexOf('.');
            return s.Substring(0, lastDot);
        }
    }
}