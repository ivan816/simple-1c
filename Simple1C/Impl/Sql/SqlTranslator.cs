using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Simple1C.Impl.Sql.SqlBuilders;

namespace Simple1C.Impl.Sql
{
    public class SqlTranslator
    {
        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+([^\s]+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex fieldsRegex = new Regex(@"([a-zA-Z]+\.[^\,\s]+)",
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
                var tableAlias = fieldDesc[0];
                var fieldName = GetTableMarker(tableNameMarkers, tableAlias).GetFieldName(fieldDesc);
                return tableAlias + "." + fieldName;
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
            private readonly TableMapping mapping;
            private readonly List<NestedTableFieldReference> nestedReferences = new List<NestedTableFieldReference>();
            private readonly List<string> references = new List<string>();
            private static int genNumber;

            public TableNameMarker(string alias, TableMapping mapping)
            {
                this.alias = alias;
                this.mapping = mapping;
            }

            public string GetFieldName(string[] properties)
            {
                var mainProperty = mapping.GetByPropertyName(properties[1]);
                if (properties.Length == 2)
                {
                    if (mainProperty.NestedTableMapping != null)
                        throw new InvalidOperationException("assertion failure");
                    references.Add(mainProperty.FieldName);
                    return mainProperty.FieldName;
                }
                if(properties.Length != 3)
                    throw new InvalidOperationException("assertion failure");
                if (mainProperty.NestedTableMapping == null)
                    throw new InvalidOperationException("assertion failure");
                var nestedProperty = mainProperty.NestedTableMapping.GetByPropertyName(properties[2]);
                NestedTableFieldReference nestedReference = null;
                foreach (var r in nestedReferences)
                    if (r.mainProperty == mainProperty && r.nestedProperty == nestedProperty)
                    {
                        nestedReference = r;
                        break;
                    }
                if (nestedReference == null)
                    nestedReferences.Add(nestedReference = new NestedTableFieldReference
                    {
                        alias = "__nested_field" + genNumber++,
                        mainProperty = mainProperty,
                        nestedProperty = nestedProperty
                    });
                return nestedReference.alias;
            }

            public string GetSql()
            {
                if (nestedReferences.Count == 0)
                    return mapping.DbName + " as " + alias;
                var selectClause = new SelectClause
                {
                    TableName = mapping.DbName,
                    TableAlias = "__nested_main_table" + genNumber++,
                    JoinClauses = new List<JoinClause>(),
                    Fields = new List<SelectField>()
                };
                var propertyToJoinClause = new Dictionary<PropertyMapping, JoinClause>();
                foreach (var r in nestedReferences)
                    if (!propertyToJoinClause.ContainsKey(r.mainProperty))
                    {
                        var tableAlias = "__nested_table" + genNumber++;
                        var joinClause = new JoinClause
                        {
                            TableName = r.mainProperty.NestedTableMapping.DbName,
                            TableAlias = tableAlias,
                            LeftFieldName = r.mainProperty.FieldName,
                            LeftFieldTableName = selectClause.TableAlias,
                            RightFieldName = r.mainProperty.NestedTableMapping.GetByPropertyName("Ссылка").FieldName,
                            RightFieldTableName = tableAlias,
                            JoinKind = "left"
                        };
                        selectClause.JoinClauses.Add(joinClause);
                        propertyToJoinClause.Add(r.mainProperty, joinClause);
                    }
                foreach (var r in references)
                    selectClause.Fields.Add(new SelectField
                    {
                        Name = r,
                        TableName = selectClause.TableAlias
                    });
                foreach (var r in nestedReferences)
                    selectClause.Fields.Add(new SelectField
                    {
                        Name = r.nestedProperty.FieldName,
                        TableName = propertyToJoinClause[r.mainProperty].TableAlias,
                        Alias = r.alias
                    });
                return "(" + selectClause.GetSql() + ") as " + alias;
            }

            private class NestedTableFieldReference
            {
                public string alias;
                public PropertyMapping mainProperty;
                public PropertyMapping nestedProperty;
            }
        }
    }
}