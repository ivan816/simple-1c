using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql
{
    internal class PostgreeSqlSchemaStore : ITableMappingSource
    {
        private readonly PostgreeSqlDatabase database;
        private readonly TableMappingsCache cache;

        private static readonly TableDesc typeMappingsTableDesc =
            new TableDesc
            {
                tableName = "simple1c__tableMappings",
                columns = new[]
                {
                    new DataColumn
                    {
                        ColumnName = "queryTableName",
                        AllowDBNull = false,
                        DataType = typeof(string),
                        MaxLength = 200
                    },
                    new DataColumn
                    {
                        ColumnName = "dbName",
                        AllowDBNull = false,
                        DataType = typeof(string),
                        MaxLength = 200
                    },
                    new DataColumn
                    {
                        ColumnName = "properties",
                        AllowDBNull = false,
                        DataType = typeof(string),
                        MaxLength = -1
                    }
                },
                createIndexesSql =
                    "CREATE INDEX simple1c__tableMappings_index ON simple1c__tableMappings ((lower(queryTableName)))"
            };

        private static readonly TableDesc enumMappingsTableDesc =
            new TableDesc
            {
                tableName = "simple1c__enumMappings",
                columns = new[]
                {
                    new DataColumn
                    {
                        ColumnName = "enumName",
                        AllowDBNull = false,
                        DataType = typeof(string),
                        MaxLength = 200
                    },
                    new DataColumn
                    {
                        ColumnName = "enumValueName",
                        AllowDBNull = false,
                        DataType = typeof(string),
                        MaxLength = 200
                    },
                    new DataColumn
                    {
                        ColumnName = "orderIndex",
                        AllowDBNull = false,
                        DataType = typeof(int)
                    }
                },
                createIndexesSql =
                    "CREATE INDEX simple1c__enumMappings_index ON simple1c__enumMappings (enumName,orderIndex)"
            };

        public PostgreeSqlSchemaStore(PostgreeSqlDatabase database)
        {
            this.database = database;
            cache = new TableMappingsCache(Enumerable.Empty<TableMapping>(), LoadMappingOrNull);
        }

        public void WriteEnumMappings(EnumMapping[] mappings)
        {
            RecreateTable(enumMappingsTableDesc, mappings, x => new object[]
            {
                x.enumName,
                x.enumValueName,
                x.orderIndex
            });
        }

        public void WriteTableMappings(TableMapping[] mappings)
        {
            RecreateTable(typeMappingsTableDesc, mappings, x => new object[]
            {
                x.QueryTableName,
                x.DbTableName,
                x.Properties.Select(p => p.Serialize()).JoinStrings("\r\n")
            });
        }

        public TableMapping GetByQueryName(string queryName)
        {
            return cache.GetByQueryName(queryName);
        }

        private TableMapping LoadMappingOrNull(string queryName)
        {
            const string sql = "select queryTableName,dbName,properties " +
                               "from simple1c__tableMappings " +
                               "where lower(queryTableName) = lower(@p0)" +
                               "limit 1";
            return database.ExecuteReader(
                sql, new object[] {queryName.ToLower()},
                r => new TableMapping(r.GetString(0),
                    r.GetString(1),
                    r.GetString(2)
                        .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(PropertyMapping.Parse).ToArray()))
                .SingleOrDefault();
        }

        private void RecreateTable<T>(TableDesc tableDesc, IEnumerable<T> data, Func<T, object[]> getColumnValues)
        {
            if (database.TableExists(tableDesc.tableName))
                database.DropTable(tableDesc.tableName);
            database.CreateTable("public." + tableDesc.tableName, tableDesc.columns);
            database.BulkCopy(data.Select(getColumnValues),
                tableDesc.tableName,
                tableDesc.columns);
            database.ExecuteNonQuery(tableDesc.createIndexesSql);
        }

        private class TableDesc
        {
            public string tableName;
            public DataColumn[] columns;
            public string createIndexesSql;
        }
    }
}