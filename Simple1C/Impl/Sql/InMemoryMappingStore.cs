using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple1C.Impl.Sql
{
    internal class InMemoryMappingStore : ITableMappingSource
    {
        private readonly TableMappingsCache cache;

        public InMemoryMappingStore(IEnumerable<TableMapping> tables)
        {
            Tables = tables.ToArray();
            cache = new TableMappingsCache(Tables, null);
        }

        public TableMapping[] Tables { get; private set; }

        public TableMapping GetByQueryName(string queryName)
        {
            return cache.GetByQueryName(queryName);
        }

        public static InMemoryMappingStore Parse(string source)
        {
            var items = source.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var tableMappings = new List<TableMapping>();
            var columnMappings = new List<PropertyMapping>();
            string queryTableName = null;
            string dbTableName = null;
            foreach (var s in items)
            {
                if (s[0] == '\t')
                    columnMappings.Add(PropertyMapping.Parse(s.Substring(1)));
                else
                {
                    if (queryTableName != null)
                        tableMappings.Add(new TableMapping(queryTableName, dbTableName, columnMappings.ToArray()));
                    var tableNames = s.Split(new[] {" "}, StringSplitOptions.None);
                    queryTableName = tableNames[0];
                    dbTableName = tableNames[1];
                    columnMappings.Clear();
                }
            }
            if (queryTableName != null)
                tableMappings.Add(new TableMapping(queryTableName, dbTableName, columnMappings.ToArray()));
            return new InMemoryMappingStore(tableMappings);
        }
    }
}