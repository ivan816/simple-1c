using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple1C.Impl.Sql
{
    public class TableMapping
    {
        public string QueryName { get; private set; }
        public string DbName { get; private set; }
        public ColumnMapping[] Columns { get; private set; }
        private readonly Dictionary<string, ColumnMapping> byQueryName;

        public TableMapping(string queryName, string dbName, ColumnMapping[] columns)
        {
            QueryName = queryName;
            DbName = dbName;
            Columns = columns;
            byQueryName = Columns.ToDictionary(x => x.QueryName, StringComparer.OrdinalIgnoreCase);
        }

        public ColumnMapping GetByQueryNameOrNull(string queryName)
        {
            ColumnMapping result;
            return byQueryName.TryGetValue(queryName, out result) ? result : null;
        }
    }
}