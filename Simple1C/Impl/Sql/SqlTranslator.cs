using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql
{
    public class SqlTranslator
    {
        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+([^\s]+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex fieldsRegex = new Regex(GetFieldsRegex(),
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static string GetFieldsRegex()
        {
            const string propRegex = @"[a-zA-Z]+\.[^\,\s]+";
            return string.Format(@"(?<func>ПРЕДСТАВЛЕНИЕ)\((?<prop>{0})\)|(?<prop>{0})",
                propRegex);
        }

        public string Translate(MappingSchema mappingSchema, string source)
        {
            source = source.Replace('"', '\'');
            var match = tableNameRegex.Match(source);
            var tableNameMarkers = new Dictionary<string, TableNameMarker>();
            var nameGenerator = new NameGenerator();
            while (match.Success)
            {
                var queryName = match.Groups[2].Value;
                var alias = match.Groups[3].Value;
                var tableNameMarker = new TableNameMarker(alias,
                    mappingSchema.GetByQueryName(queryName), nameGenerator);
                tableNameMarkers.Add(alias, tableNameMarker);
                match = match.NextMatch();
            }
            var result = fieldsRegex.Replace(source, delegate(Match m)
            {
                var properyPath = m.Groups["prop"].Value;
                var properties = properyPath.Split('.');
                if (properties.Length < 2)
                {
                    const string messageFormat = "invalid propery [{0}], alias must be specified";
                    throw new InvalidOperationException(string.Format(messageFormat, properyPath));
                }
                var tableAlias = properties[0];
                FunctionName? functionName = null;
                if (m.Groups["func"].Success)
                {
                    var functionNameString = m.Groups["func"].Value;
                    if (functionNameString == "ПРЕДСТАВЛЕНИЕ")
                        functionName = FunctionName.Representation;
                    else
                    {
                        const string messageFormat = "unexpected function [{0}] for [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            functionNameString, properyPath));
                    }
                }
                var fieldName = GetTableMarker(tableNameMarkers, tableAlias)
                    .GetFieldName(properties, functionName);
                return tableAlias + "." + fieldName;
            });
            result = tableNameRegex.Replace(result,
                m => m.Groups[1].Value + " " +
                     GetTableMarker(tableNameMarkers, m.Groups[3].Value).GetSql());
            return result;
        }

        private enum FunctionName
        {
            Representation
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
            private readonly string outerAlias;
            private const string mainTableInnerAlias = "__nested_main_table";
            private readonly TableMapping mapping;
            private readonly NameGenerator nameGenerator;

            private readonly Dictionary<PropertyMapping, JoinTable> joinTables =
                new Dictionary<PropertyMapping, JoinTable>();

            private readonly List<string> mainTableFields = new List<string>();

            public TableNameMarker(string outerAlias, TableMapping mapping, NameGenerator nameGenerator)
            {
                this.outerAlias = outerAlias;
                this.mapping = mapping;
                this.nameGenerator = nameGenerator;
            }

            public string GetFieldName(string[] properties, FunctionName? functionName)
            {
                JoinTable joinTable = null;
                var referencingTableAlias = mainTableInnerAlias;
                var referencingTableMapping = mapping;
                for (var i = 1; i < properties.Length - 1; i++)
                {
                    var propertyName = properties[i];
                    var referencingProperty = referencingTableMapping.GetByPropertyName(propertyName);
                    if (referencingProperty.NestedTableMapping == null)
                    {
                        const string messageFormat = "no table maping for [{0}] in [{1}]";
                        throw new InvalidComObjectException(string.Format(messageFormat,
                            propertyName, properties.JoinStrings(".")));
                    }
                    joinTable = GetJoinTable(referencingTableAlias, referencingProperty);
                    referencingTableAlias = joinTable.Alias;
                    referencingTableMapping = referencingProperty.NestedTableMapping;
                }
                var lastPropertyName = properties[properties.Length - 1];
                var lastProperty = referencingTableMapping.GetByPropertyName(lastPropertyName);
                if (lastProperty.NestedTableMapping != null)
                {
                    if (!lastProperty.NestedTableMapping.IsEnum())
                    {
                        const string messageFormat = "unexpected mapping found for [{0}] in [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            lastPropertyName, properties));
                    }
                    if (functionName == FunctionName.Representation)
                    {
                        joinTable = GetJoinTable(referencingTableAlias, lastProperty);
                        return GetEnumTextJoinTable(joinTable.Alias, lastProperty.NestedTableMapping)
                            .GetProperty("enumValueName", nameGenerator)
                            .alias;
                    }
                }
                if (joinTable != null)
                    return joinTable.GetProperty(lastProperty.FieldName, nameGenerator).alias;
                mainTableFields.Add(lastProperty.FieldName);
                return lastProperty.FieldName;
            }

            private JoinTable GetJoinTable(string referencingTableAlias, PropertyMapping property)
            {
                JoinTable result;
                if (!joinTables.TryGetValue(property, out result))
                    joinTables.Add(property, result = new JoinTable(new JoinClause
                    {
                        TableName = property.NestedTableMapping.DbTableName,
                        TableAlias = nameGenerator.Generate("__nested_table"),
                        JoinKind = "left",
                        EqConditions = new[]
                        {
                            new JoinEqCondition
                            {
                                FieldName = property.NestedTableMapping.GetByPropertyName("Ссылка").FieldName,
                                ComparandTableName = referencingTableAlias,
                                ComparandFieldName = property.FieldName
                            }
                        }
                    }));
                return result;
            }

            private JoinTable GetEnumTextJoinTable(string referencingTableAlias,
                TableMapping enumTableMapping)
            {
                JoinTable result;
                var orderProperty = enumTableMapping.GetByPropertyName("Порядок");
                if (!joinTables.TryGetValue(orderProperty, out result))
                    joinTables.Add(orderProperty, result = new JoinTable(new JoinClause
                    {
                        TableName = "simple1c__enumValues",
                        TableAlias = nameGenerator.Generate("__nested_table"),
                        JoinKind = "left",
                        EqConditions = new[]
                        {
                            new JoinEqCondition
                            {
                                FieldName = "enumName",
                                ComparandConstantValue = "'" + enumTableMapping.ObjectName + "'"
                            },
                            new JoinEqCondition
                            {
                                FieldName = "order",
                                ComparandTableName = referencingTableAlias,
                                ComparandFieldName = orderProperty.FieldName
                            }
                        }
                    }));
                return result;
            }

            public string GetSql()
            {
                if (joinTables.Count == 0)
                    return mapping.DbTableName + " as " + outerAlias;
                var selectClause = new SelectClause(mapping.DbTableName, mainTableInnerAlias);
                foreach (var r in mainTableFields)
                    selectClause.Fields.Add(new SelectField
                    {
                        Name = r,
                        TableName = selectClause.TableAlias
                    });
                foreach (var r in joinTables.Values)
                    r.AppendTo(selectClause);
                return "(" + selectClause.GetSql() + ") as " + outerAlias;
            }

            private class JoinTable
            {
                private readonly JoinClause joinClause;

                private readonly Dictionary<string, JoinTableProperty> properties =
                    new Dictionary<string, JoinTableProperty>();

                public JoinTable(JoinClause joinClause)
                {
                    this.joinClause = joinClause;
                }

                public string Alias
                {
                    get { return joinClause.TableAlias; }
                }

                public JoinTableProperty GetProperty(string fieldName, NameGenerator nameGenerator)
                {
                    JoinTableProperty result;
                    if (!properties.TryGetValue(fieldName, out result))
                        properties.Add(fieldName, result = new JoinTableProperty
                        {
                            alias = nameGenerator.Generate("__nested_field"),
                            fieldName = fieldName
                        });
                    return result;
                }

                public void AppendTo(SelectClause selectClause)
                {
                    selectClause.JoinClauses.Add(joinClause);
                    foreach (var p in properties)
                        selectClause.Fields.Add(new SelectField
                        {
                            TableName = Alias,
                            Name = p.Value.fieldName,
                            Alias = p.Value.alias
                        });
                }
            }

            private class JoinTableProperty
            {
                public string alias;
                public string fieldName;
            }
        }

        private class NameGenerator
        {
            private readonly Dictionary<string, int> lastUsed = new Dictionary<string, int>();

            public string Generate(string prefix)
            {
                int lastUsedForPrefix;
                var number = lastUsed[prefix] = lastUsed.TryGetValue(prefix, out lastUsedForPrefix)
                    ? lastUsedForPrefix + 1
                    : 0;
                return prefix + number;
            }
        }
    }
}