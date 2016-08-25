using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Simple1C.Impl.Sql
{
    internal class TableMapping
    {
        public string QueryTableName { get; private set; }
        public string DbTableName { get; private set; }
        public PropertyMapping[] Properties { get; private set; }
        public string ObjectName { get; private set; }

        private readonly Dictionary<string, PropertyMapping> byPropertyName =
            new Dictionary<string, PropertyMapping>(StringComparer.OrdinalIgnoreCase);

        public TableMapping(string queryTableName, string dbName, PropertyMapping[] properties)
        {
            QueryTableName = queryTableName;
            DbTableName = dbName;
            Properties = properties;
            PatchDbTableName();
            foreach (var p in Properties)
                if (!byPropertyName.ContainsKey(p.PropertyName))
                {
                    //какие-то дурацкие дубли с полем
                    //сумма в РегистрБухгалтерии.Хозрасчетный.Остатки, забил пока
                    byPropertyName.Add(p.PropertyName, p);
                    //const string messageFormat = "property [{0}] for table [{1}] already exist";
                    //throw new InvalidOperationException(string.Format(messageFormat, p.PropertyName, QueryName));
                }
            ObjectName = QueryTableName.Split('.')[1];
        }

        private void PatchDbTableName()
        {
            var b = new StringBuilder(DbTableName);
            b[0] = char.ToLower(b[0]);
            b.Insert(0, '_');
            DbTableName = b.ToString();
        }

        public PropertyMapping GetByPropertyName(string queryName)
        {
            PropertyMapping result;
            if (!byPropertyName.TryGetValue(queryName, out result))
            {
                const string messagFormat = "can't find field [{0}] for table [{1}]";
                throw new InvalidComObjectException(string.Format(messagFormat, queryName, QueryTableName));
            }
            return result;
        }

        public bool IsEnum()
        {
            return QueryTableName.StartsWith("перечисление", StringComparison.OrdinalIgnoreCase);
        }
    }
}