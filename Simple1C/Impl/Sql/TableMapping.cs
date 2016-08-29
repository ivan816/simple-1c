using System;
using System.Collections.Generic;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql
{
    internal class TableMapping
    {
        public string QueryTableName { get; private set; }
        public string DbTableName { get; private set; }
        public PropertyMapping[] Properties { get; private set; }
        private ConfigurationName? objectName;

        public ConfigurationName ObjectName
        {
            get
            {
                var result = objectName ?? (objectName = ConfigurationName.Parse(QueryTableName));
                return result.Value;
            }
        }

        private readonly Dictionary<string, PropertyMapping> byPropertyName =
            new Dictionary<string, PropertyMapping>(StringComparer.OrdinalIgnoreCase);

        public TableMapping(string queryTableName, string dbName, PropertyMapping[] properties)
        {
            QueryTableName = queryTableName;
            DbTableName = dbName;
            Properties = properties;
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

        public PropertyMapping GetByPropertyName(string queryName)
        {
            PropertyMapping result;
            if (!byPropertyName.TryGetValue(queryName, out result))
            {
                const string messagFormat = "can't find field [{0}] for table [{1}]";
                throw new InvalidOperationException(string.Format(messagFormat, queryName, QueryTableName));
            }
            return result;
        }

        public bool IsEnum()
        {
            return ObjectName.Scope == ConfigurationScope.Перечисления;
        }
    }
}