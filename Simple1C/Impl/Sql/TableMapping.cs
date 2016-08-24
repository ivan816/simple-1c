using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Simple1C.Impl.Sql
{
    public class TableMapping
    {
        public string QueryName { get; private set; }
        public string DbName { get; private set; }
        public PropertyMapping[] Properties { get; private set; }
        private readonly Dictionary<string, PropertyMapping> byQueryName;

        public TableMapping(string queryName, string dbName, PropertyMapping[] properties)
        {
            QueryName = queryName;
            DbName = dbName;
            Properties = properties;
            byQueryName = Properties.ToDictionary(x => x.PropertyName, StringComparer.OrdinalIgnoreCase);
        }

        public PropertyMapping GetByQueryName(string queryName)
        {
            PropertyMapping result;
            if (!byQueryName.TryGetValue(queryName, out result))
            {
                const string messagFormat = "can't find field [{0}] for table [{1}]";
                throw new InvalidComObjectException(string.Format(messagFormat, queryName, QueryName));
            }
            return result;
        }
    }
}