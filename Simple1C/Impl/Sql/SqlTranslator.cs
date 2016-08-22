using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simple1C.Impl.Sql
{
    public class SqlTranslator
    {
        private static readonly Regex tableNameRegex = new Regex(@"(?:from|join)\s+([^\s]+)(\s+as\s+([^\s]+))?",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex tokensRegex = new Regex(@"((?:[^\s]|\.)+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly HashSet<string> keywords = new HashSet<string>
        {
            "select",
            "from",
            "where",
            "join",
            "and",
            "or"
        };

        public string Translate(MappingSchema mappingSchema, string source)
        {
            var match = tableNameRegex.Match(source);
            var tableNameMarkers = new List<TableNameMarker>();
            while (match.Success)
            {
                var queryName = match.Groups[1].Value;
                tableNameMarkers.Add(new TableNameMarker
                {
                    index = match.Index,
                    queryName = queryName,
                    mapping = mappingSchema.GetByQueryName(queryName),
                    alias = match.Groups[3].Success ? match.Groups[3].Value : null
                });
                match = match.NextMatch();
            }
            return tokensRegex.Replace(source, delegate(Match m)
            {
                var value = m.Value;
                var tableMarker = tableNameMarkers.FirstOrDefault(x => x.queryName == value);
                if (tableMarker != null)
                    return tableMarker.mapping.DbName;
                var items = value.Split('.');
                var alias = items.Length == 2 ? items[0] : null;
                var name = items.Length == 2 ? items[1] : value;
                tableMarker = tableNameMarkers.FirstOrDefault(x => x.alias == alias && x.index >= m.Index) ??
                                  Enumerable.Reverse(tableNameMarkers)
                                      .FirstOrDefault(x => x.alias == alias && x.index <= m.Index);
                if (tableMarker == null)
                    return value;
                if (keywords.Contains(name))
                    return value;
                if (value == tableMarker.mapping.QueryName)
                    return tableMarker.mapping.DbName;
                var mapping = tableMarker.mapping.GetByQueryNameOrNull(name);
                if (mapping == null)
                    return value;
                return alias == null ? mapping.DbName : alias + "." + mapping.DbName;
            });
        }

        private class TableNameMarker
        {
            public int index;
            public string queryName;
            public string alias;
            public TableMapping mapping;
        }
    }
}