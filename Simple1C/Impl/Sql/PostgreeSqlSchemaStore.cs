using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    public class PostgreeSqlSchemaStore : ITableMappingSource
    {
        private readonly PostgreeSqlDatabase database;

        private readonly Dictionary<string, TableMapping> tableByQueryName =
            new Dictionary<string, TableMapping>(StringComparer.OrdinalIgnoreCase);

        public PostgreeSqlSchemaStore(PostgreeSqlDatabase database)
        {
            this.database = database;
        }

        public TableMapping GetByQueryName(string queryName)
        {
            TableMapping result;
            if (!tableByQueryName.TryGetValue(queryName, out result))
                tableByQueryName.Add(queryName, result = LoadMapping(queryName));
            return result;
        }

        private TableMapping LoadMapping(string queryName)
        {
            const string sql = "select queryTableName,dbName,properties " +
                               "where queryTableName = @p0 limit 1";
            var result = database.ExecuteReader(
                sql, new object[] {queryName},
                r => new TableMapping(r.GetString(0),
                    r.GetString(1),
                    r.GetString(2)
                        .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(PropertyMapping.Parse).ToArray())).SingleOrDefault();
            if (result == null)
            {
                const string messageFormat = "can't find table mapping for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, queryName));
            }
            return result;
        }
    }
}