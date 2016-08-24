using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple1C.Impl.Sql
{
    public class MappingSchema
    {
        private readonly Dictionary<string, TableMapping> tableByQueryName;

        public MappingSchema(IEnumerable<TableMapping> tables)
        {
            Tables = tables.ToArray();
            tableByQueryName = Tables.ToDictionary(x => x.QueryName, StringComparer.OrdinalIgnoreCase);
        }

        public TableMapping[] Tables { get; private set; }

        public TableMapping GetByQueryName(string queryName)
        {
            TableMapping result;
            if (!tableByQueryName.TryGetValue(queryName, out result))
                throw new InvalidOperationException(string.Format("can't find table [{0}]", queryName));
            return result;
        }

        public static MappingSchema Parse(string source)
        {
            var items = source.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var tableMappings = new List<TableMapping>();
            var columnMappings = new List<PropertyMapping>();
            string queryTableName = null;
            string dbTableName = null;
            foreach (var s in items)
            {
                if (s[0] == '\t')
                {
                    var columnDesc = s.Substring(1).Split(new[] {" "}, StringSplitOptions.None);
                    if (columnDesc.Length != 2 && columnDesc.Length != 3)
                        throw new InvalidOperationException(string.Format("can't parse line [{0}]", s));
                    columnMappings.Add(new PropertyMapping
                    {
                        PropertyName = columnDesc[0],
                        FieldName = columnDesc[1],
                        TypeName = columnDesc.Length == 3 ? columnDesc[2] : null
                    });
                }
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
            return new MappingSchema(tableMappings);
        }
    }
}