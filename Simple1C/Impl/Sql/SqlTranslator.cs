using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Simple1C.Impl.Sql
{
    public class SqlTranslator
    {
        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+([^\s]+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex fieldsRegex = new Regex(@"([a-zA-Z]+\.[\S]+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public string Translate(MappingSchema mappingSchema, string source)
        {
            var match = tableNameRegex.Match(source);
            var tableNameMarkers = new Dictionary<string, TableNameMarker>();
            while (match.Success)
            {
                var queryName = match.Groups[2].Value;
                var alias = match.Groups[3].Value;
                var tableNameMarker = new TableNameMarker(alias,
                    mappingSchema.GetByQueryName(queryName));
                tableNameMarkers.Add(alias, tableNameMarker);
                match = match.NextMatch();
            }
            var result = fieldsRegex.Replace(source, delegate(Match m)
            {
                var fieldDesc = m.Groups[1].Value.Split('.');
                if (fieldDesc.Length < 2)
                {
                    const string messageFormat = "invalid field spec [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, m.Value));
                }
                if (fieldDesc.Length > 3)
                {
                    const string message = "only single level nesting supported, field [{0}]";
                    throw new InvalidOperationException(string.Format(message, m.Value));
                }
                var fieldTableAlias = fieldDesc[0];
                var tableMarker = GetTableMarker(tableNameMarkers, fieldTableAlias);
                var fieldMapping = tableMarker.Mapping.GetByQueryName(fieldDesc[1]);
                if (fieldDesc.Length == 3)
                {
                    if (string.IsNullOrEmpty(fieldMapping.TypeName))
                    {
                        const string messageFormat = "can't detect column type for [{0}] in [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat, fieldDesc[1], m.Value));
                    }
                    var nestedTableMapping = mappingSchema.GetByQueryName(fieldMapping.TypeName);
                    var nestedPropertyMapping = tableMarker.GetNestedTableMapping(fieldMapping, nestedTableMapping, fieldDesc[2]);
                    return joinColumnMapping.GetDbFieldRef(nestedPropertyMapping);
                }
                tableMarker.AddReferencedField(fieldMapping.FieldName);
                return fieldMapping.GetDbFieldRef(fieldTableAlias);
            });
            result = tableNameRegex.Replace(result,
                m => m.Groups[1].Value + " " +
                     GetTableMarker(tableNameMarkers, m.Groups[3].Value).GetSql());
            return result;
        }

        private static TableNameMarker GetTableMarker(Dictionary<string, TableNameMarker> markers, string alias)
        {
            TableNameMarker result;
            if (!markers.TryGetValue(alias, out result))
            {
                const string messageFormat = "invalid table alias for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, alias));
            }
            return result;
        }

        private class TableNameMarker
        {
            private readonly string alias;
            private readonly List<NestedTable> nestedTables = new List<NestedTable>();
            private readonly List<string> referencedFieldsDbNames = new List<string>();
            private static int genJoinNumber;

            public TableNameMarker(string alias, TableMapping mapping)
            {
                this.alias = alias;
                Mapping = mapping;
            }

            public void AddReferencedField(string fieldName)
            {
                referencedFieldsDbNames.Add(fieldName);
            }

            public TableMapping Mapping { get; private set; }

            public string GetSql()
            {
                var result = Mapping.DbName + " as " + alias;
                foreach (var nestingJoin in nestedTables)
                {
                    var joinCondition = nestingJoin.property.GetDbFieldRef(alias) + " = "
                                        + nestingJoin.table.GetByQueryName("Ссылка").GetDbFieldRef(nestingJoin.alias);
                    result += "\r\nleft join " + nestingJoin.table.DbName +
                              " as " + nestingJoin.alias +
                              " on " + joinCondition;
                }
                return result;
            }

            public PropertyMapping GetNestedTableMapping(PropertyMapping joinProperty, TableMapping nestedTableMapping, string nestedPropertyName)
            {
                var nestedTable = GetNestedTable(joinProperty, nestedTableMapping);
                return nestedTable;
            }

            private NestedTable GetNestedTable(PropertyMapping joinProperty, TableMapping nestedTableMapping)
            {
                foreach (var n in nestedTables)
                    if (n.property == joinProperty && n.table == nestedTableMapping)
                        return n;
                var result = new NestedTable
                {
                    alias = "__j_gen_" + genJoinNumber++,
                    table = nestedTableMapping,
                    property = joinProperty
                };
                nestedTables.Add(result);
                return result;
            }

            private class NestedTable
            {
                public string alias;
                public PropertyMapping property;
                public TableMapping table;
                public List<string> referencedFieldsDbNames = new List<string>();
            }
        }
    }
}