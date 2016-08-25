using System;
using System.Collections.Generic;

namespace Simple1C.Impl.Sql
{
    internal class TableMappingsCache
    {
        private readonly Func<string, TableMapping> resolveOrNull;

        private readonly Dictionary<string, TableMapping> cache =
            new Dictionary<string, TableMapping>(StringComparer.OrdinalIgnoreCase);

        public TableMappingsCache(IEnumerable<TableMapping> initial, Func<string, TableMapping> resolveOrNull)
        {
            this.resolveOrNull = resolveOrNull;
            foreach (var mapping in initial)
                cache.Add(mapping.QueryTableName, mapping);
        }

        public TableMapping GetByQueryName(string queryName)
        {
            TableMapping result;
            if (!cache.TryGetValue(queryName, out result))
            {
                result = resolveOrNull != null ? resolveOrNull(queryName) : null;
                if (result != null)
                    cache.Add(queryName, result);
                else
                {
                    const string messageFormat = "can't find table mapping for [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, queryName));
                }
            }
            return result;
        }
    }
}