using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql
{
    internal class QueryToSqlTranslator
    {
        private readonly IMappingSource mappingSource;

        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+([^\s]+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex valueFunctionRegex = new Regex(@"значение\(([^\)]+)\)",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, string> keywordsMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"выбрать", "select"},
                {"как", "as"},
                {"из", "from"},
                {"где", "where"},
                {"и", "and"},
                {"или", "or"}
            };

        private static readonly Regex keywordsRegex = new Regex(string.Format(@"\b({0})\b",
            keywordsMap.Keys.JoinStrings("|")),
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex propertiesRegex = new Regex(GetPropertiesRegex(),
            RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly Dictionary<string, QueryEntity> queryTables =
            new Dictionary<string, QueryEntity>(StringComparer.OrdinalIgnoreCase);

        private readonly NameGenerator nameGenerator = new NameGenerator();

        public QueryToSqlTranslator(IMappingSource mappingSource)
        {
            this.mappingSource = mappingSource;
        }

        public string Translate(string source)
        {
            var result = source;
            result = result.Replace("\"", "'");
            result = keywordsRegex.Replace(result, m => keywordsMap[m.Groups[1].Value]);
            var match = tableNameRegex.Match(result);
            while (match.Success)
            {
                var queryName = match.Groups[2].Value;
                var alias = match.Groups[3].Value;
                queryTables.Add(alias, CreateQueryEntity(queryName));
                match = match.NextMatch();
            }
            result = propertiesRegex.Replace(result, delegate(Match m)
            {
                var properyPath = m.Groups["prop"].Value;
                var properties = properyPath.Split('.');
                if (properties.Length < 2)
                {
                    const string messageFormat = "invalid propery [{0}], alias must be specified";
                    throw new InvalidOperationException(string.Format(messageFormat, properyPath));
                }
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
                return GetColumnName(properties, functionName);
            });
            result = tableNameRegex.Replace(result,
                m => m.Groups[1].Value + " " + GetSql(m.Groups[3].Value));
            result = valueFunctionRegex.Replace(result, m => GetEnumValueSql(m.Groups[1].Value));
            return result;
        }

        private string GetEnumValueSql(string enumValue)
        {
            var enumValueItems = enumValue.Split('.');
            var table = CreateQueryEntity(enumValueItems[0] + "." + enumValueItems[1]);
            var selectClause = CreateSelectClause(table);
            selectClause.Columns.Add(new SelectColumn
            {
                Name = table.mapping.GetByPropertyName("Ссылка").ColumnName,
                TableName = GetQueryEntityAlias(table)
            });
            var enumMappingsJoinClause = CreateEnumMappingsJoinClause(table);
            selectClause.JoinClauses.Add(enumMappingsJoinClause);
            selectClause.WhereEqConditions.Add(new EqCondition
            {
                ColumnName = "enumValueName",
                ColumnTableName = enumMappingsJoinClause.TableAlias,
                ComparandConstantValue = enumValueItems[2]
            });
            return selectClause.GetSql();
        }

        private QueryEntity GetQueryTable(string alias)
        {
            QueryEntity mainEntity;
            if (!queryTables.TryGetValue(alias, out mainEntity))
            {
                const string messageFormat = "can't find query table by alias [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, alias));
            }
            return mainEntity;
        }

        private string GetColumnName(string[] properties, FunctionName? functionName)
        {
            var lastEntity = GetQueryTable(properties[0]);
            QueryEntityProperty lastProperty;
            var columnNeedsAlias = false;
            for (var i = 1; i < properties.Length - 1; i++)
            {
                lastProperty = lastEntity.GetOrCreateProperty(properties[i]);
                lastEntity = GetOrCreateQueryEntity(lastProperty, properties);
                columnNeedsAlias = true;
            }
            lastProperty = lastEntity.GetOrCreateProperty(properties[properties.Length - 1]);
            if (functionName.HasValue)
            {
                if (functionName.Value != FunctionName.Representation)
                {
                    const string messageFormat = "unexpected function [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        FormatFunctionName(functionName.Value)));
                }
                lastEntity = GetOrCreateQueryEntity(lastProperty, properties);
                var scope = lastEntity.mapping.ObjectName.Scope;
                var validScopes = new[] {ConfigurationScope.Перечисления, ConfigurationScope.Справочники};
                if (!validScopes.Contains(scope))
                {
                    const string messageFormat = "function [{0}] is only supported for [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        FormatFunctionName(functionName.Value), validScopes.JoinStrings(",")));
                }
                var propertyName = scope == ConfigurationScope.Справочники
                    ? "Наименование"
                    : "Порядок";
                lastProperty = lastEntity.GetOrCreateProperty(propertyName);
                columnNeedsAlias = true;
            }
            lastProperty.selected = true;
            if (lastProperty.alias == null && columnNeedsAlias)
                lastProperty.alias = nameGenerator.GenerateColumnName();
            return properties[0] + "." + (lastProperty.alias ?? lastProperty.mapping.ColumnName);
        }

        private QueryEntity GetOrCreateQueryEntity(QueryEntityProperty property, string[] propertyPath)
        {
            if (property.nestedEntity == null)
            {
                var nestedTableName = property.mapping.NestedTableName;
                if (string.IsNullOrEmpty(nestedTableName))
                {
                    const string messageFormat = "property [{0}] has no table mapping, property path [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        property.mapping.PropertyName, propertyPath.JoinStrings(".")));
                }
                property.nestedEntity = CreateQueryEntity(nestedTableName);
            }
            return property.nestedEntity;
        }

        private QueryEntity CreateQueryEntity(string tableName)
        {
            var tableMapping = mappingSource.ResolveTable(tableName);
            return new QueryEntity(tableMapping);
        }

        private string GetSql(string alias)
        {
            var table = GetQueryTable(alias);
            var hasNestedTables = false;
            foreach (var f in table.properties)
                if (f.nestedEntity != null)
                {
                    hasNestedTables = true;
                    break;
                }
            string sql;
            if (hasNestedTables)
            {
                var selectClause = CreateSelectClause(table);
                BuildSubQuery(table, selectClause);
                sql = selectClause.GetSql();
            }
            else
                sql = table.mapping.DbTableName;
            return sql + " as " + alias;
        }

        private void BuildSubQuery(QueryEntity entity, SelectClause target)
        {
            foreach (var f in entity.properties)
                AddPropertyToSubquery(entity, f, target);
        }

        private void AddPropertyToSubquery(QueryEntity entity, QueryEntityProperty property, SelectClause target)
        {
            if (property.selected)
            {
                if (entity.mapping.IsEnum())
                {
                    var enumMappingsJoinClause = CreateEnumMappingsJoinClause(entity);
                    target.JoinClauses.Add(enumMappingsJoinClause);
                    target.Columns.Add(new SelectColumn
                    {
                        Name = "enumValueName",
                        Alias = property.alias,
                        TableName = enumMappingsJoinClause.TableAlias
                    });
                    return;
                }
                target.Columns.Add(new SelectColumn
                {
                    Name = property.mapping.ColumnName,
                    Alias = property.alias,
                    TableName = GetQueryEntityAlias(entity)
                });
            }
            if (property.nestedEntity != null)
            {
                var joinClause = new JoinClause
                {
                    TableAlias = GetQueryEntityAlias(property.nestedEntity),
                    TableName = property.nestedEntity.mapping.DbTableName,
                    JoinKind = "left",
                    EqConditions = new[]
                    {
                        new EqCondition
                        {
                            ColumnName = property.nestedEntity.mapping.GetByPropertyName("Ссылка").ColumnName,
                            ColumnTableName = GetQueryEntityAlias(property.nestedEntity),
                            ComparandColumnName = property.mapping.ColumnName,
                            ComparandTableName = GetQueryEntityAlias(entity)
                        }
                    }
                };
                target.JoinClauses.Add(joinClause);
                BuildSubQuery(property.nestedEntity, target);
            }
        }

        private string GetQueryEntityAlias(QueryEntity entity)
        {
            return entity.alias ?? (entity.alias = nameGenerator.GenerateTableName());
        }

        private static string GetPropertiesRegex()
        {
            const string propRegex = @"[a-zA-Z]+\.[а-яА-Я\.]+";
            return string.Format(@"(?<func>ПРЕДСТАВЛЕНИЕ)\((?<prop>{0})\)|(?<prop>{0})",
                propRegex);
        }

        private SelectClause CreateSelectClause(QueryEntity queryEntity)
        {
            return new SelectClause(queryEntity.mapping.DbTableName,
                GetQueryEntityAlias(queryEntity));
        }

        private class QueryEntity
        {
            public QueryEntity(TableMapping mapping)
            {
                this.mapping = mapping;
            }

            public readonly TableMapping mapping;
            public string alias;
            public readonly List<QueryEntityProperty> properties = new List<QueryEntityProperty>();

            public QueryEntityProperty GetOrCreateProperty(string name)
            {
                foreach (var f in properties)
                    if (f.mapping.PropertyName.EqualsIgnoringCase(name))
                        return f;
                var result = new QueryEntityProperty {mapping = mapping.GetByPropertyName(name)};
                properties.Add(result);
                return result;
            }
        }

        private class QueryEntityProperty
        {
            public PropertyMapping mapping;
            public string alias;
            public bool selected;
            public QueryEntity nestedEntity;
        }

        private JoinClause CreateEnumMappingsJoinClause(QueryEntity enumEntity)
        {
            var tableAlias = nameGenerator.GenerateTableName();
            return new JoinClause
            {
                TableName = "simple1c__enumMappings",
                TableAlias = tableAlias,
                JoinKind = "left",
                EqConditions = new[]
                {
                    new EqCondition
                    {
                        ColumnName = "enumName",
                        ColumnTableName = tableAlias,
                        ComparandConstantValue = enumEntity.mapping.ObjectName.Name
                    },
                    new EqCondition
                    {
                        ColumnName = "orderIndex",
                        ColumnTableName = tableAlias,
                        ComparandTableName = GetQueryEntityAlias(enumEntity),
                        ComparandColumnName = enumEntity.mapping.GetByPropertyName("Порядок").ColumnName
                    }
                }
            };
        }

        private enum FunctionName
        {
            Representation
        }

        private static string FormatFunctionName(FunctionName name)
        {
            switch (name)
            {
                case FunctionName.Representation:
                    return "ПРЕДСТАВЛЕНИЕ";
                default:
                    throw new ArgumentOutOfRangeException("name", name, null);
            }
        }

        private class NameGenerator
        {
            private readonly Dictionary<string, int> lastUsed = new Dictionary<string, int>();

            public string GenerateTableName()
            {
                return Generate("__nested_table");
            }

            public string GenerateColumnName()
            {
                return Generate("__nested_field");
            }

            private string Generate(string prefix)
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