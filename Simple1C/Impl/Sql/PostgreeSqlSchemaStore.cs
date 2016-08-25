using System;
using System.Linq;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    internal class PostgreeSqlSchemaStore : ITableMappingSource
    {
        private readonly PostgreeSqlDatabase database;
        private readonly TableMappingsCache cache;

        public PostgreeSqlSchemaStore(PostgreeSqlDatabase database)
        {
            this.database = database;
            cache = new TableMappingsCache(Enumerable.Empty<TableMapping>(), LoadMappingOrNull);
        }

        public TableMapping GetByQueryName(string queryName)
        {
            return cache.GetByQueryName(queryName);
        }

        private TableMapping LoadMappingOrNull(string queryName)
        {
            const string sql = "select queryTableName,dbName,properties " +
                               "where queryTableName = @p0 limit 1";
            return database.ExecuteReader(
                sql, new object[] {queryName.ToLower()},
                r => new TableMapping(r.GetString(0),
                    r.GetString(1),
                    r.GetString(2)
                        .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(PropertyMapping.Parse).ToArray()))
                .SingleOrDefault();
        }
    }
}