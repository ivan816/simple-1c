using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql
{
    internal class QueryToSqlTranslator
    {
        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+(\S+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex joinRegex = new Regex(@"join\s+\S+\s+as\s+(\S+)\s+on\s+",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex ofTypeRegex = new Regex(@"OfType\(([a-zA-Z]+\.[а-яА-Я\.]+)\s+as\s([а-яА-Я\.]+)\)",
            RegexOptions.Compiled | RegexOptions.Singleline);

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

        private static readonly Dictionary<string, SelectPart> selectParts = new Dictionary<string, SelectPart>
        {
            {"select", SelectPart.Select},
            {"where", SelectPart.Where},
            {"group\\s+by", SelectPart.GroupBy},
            {"join", SelectPart.Join}
        };

        private static readonly Dictionary<string, Regex> selectPartsRegexes = selectParts.Keys
            .ToDictionary(x => x, x => new Regex(string.Format(@"\b({0})\b", x),
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase));

        private static readonly Dictionary<string, Func<QueryToSqlTranslator, string, string>> functions =
            new Dictionary<string, Func<QueryToSqlTranslator, string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                {"значение", (t, s) => t.GetEnumValueSql(s)},
                {"год", (_, s) => string.Format("date_part('year', {0})", s)}
            };

        private static readonly Dictionary<string, Regex> functionRegexes = functions.Keys
            .ToDictionary(x => x, x => new Regex(string.Format(@"{0}\(([^\)]+)\)", x),
                RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase));

        private static readonly Regex propertiesRegex = new Regex(GetPropertiesRegex(),
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static string GetPropertiesRegex()
        {
            const string propRegex = @"[a-zA-Z]+\.[а-яА-Яa-zA-Z0-9\.]+";
            return string.Format(@"(?<func>ПРЕДСТАВЛЕНИЕ)\((?<prop>{0})\)|(?<prop>{0})",
                propRegex);
        }

        private static readonly Regex unionRegex = new Regex(@"\bunion(\s+all)?\b",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly Dictionary<string, QueryEntity> queryTables =
            new Dictionary<string, QueryEntity>(StringComparer.OrdinalIgnoreCase);

        private readonly NameGenerator nameGenerator = new NameGenerator();
        private readonly IMappingSource mappingSource;
        private readonly string[] areas;
        private string queryText;

        public QueryToSqlTranslator(IMappingSource mappingSource, int[] areas)
        {
            this.mappingSource = mappingSource;
            this.areas = new string[areas.Length];
            for (var i = 0; i < areas.Length; i++)
                this.areas[i] = areas[i].ToString();
        }

        public string Translate(string source)
        {
            var match = unionRegex.Match(source);
            if (!match.Success)
                return TranslateSingleSelect(source);
            var b = new StringBuilder();
            var lastPosition = 0;
            while (match.Success)
            {
                var itemText = source.Substring(lastPosition, match.Index - lastPosition);
                b.Append(TranslateSingleSelect(itemText));
                b.Append(match.Value);
                lastPosition = match.Index + match.Value.Length;
                match = match.NextMatch();
            }
            b.Append(TranslateSingleSelect(source.Substring(lastPosition)));
            return b.ToString();
        }

        private string TranslateSingleSelect(string source)
        {
            queryText = source;
            nameGenerator.Reset();
            queryTables.Clear();

            queryText = queryText.Replace("\"", "'");
            queryText = keywordsRegex.Replace(queryText, m => keywordsMap[m.Groups[1].Value]);
            var match = tableNameRegex.Match(queryText);
            while (match.Success)
            {
                var queryName = match.Groups[2].Value;
                var alias = match.Groups[3].Value;
                queryTables.Add(alias, CreateQueryEntity(queryName));
                match = match.NextMatch();
            }
            queryText = joinRegex.Replace(queryText, m => PatchJoin(m.Value, m.Index, m.Groups[1].Value));
            queryText = ofTypeRegex.Replace(queryText, m =>
            {
                var properyPath = m.Groups[1].Value.Split('.');
                var queryName = m.Groups[2].Value;
                var lastEntity = GetQueryEntity(properyPath[0]);
                for (var i = 1; i < properyPath.Length - 1; i++)
                {
                    var lastProperty = lastEntity.GetOrCreateProperty(properyPath[i]);
                    lastEntity = GetOrCreateQueryEntity(lastProperty, properyPath);
                }
                var queryEntityProperty = lastEntity.GetOrCreateProperty(properyPath[properyPath.Length - 1]);
                queryEntityProperty.nestedType = queryName;
                return m.Groups[1].Value;
            });
            var partsPositions = new List<SelectPartPosition>();
            foreach (var selectPart in selectParts)
            {
                var m = selectPartsRegexes[selectPart.Key].Match(queryText);
                if (m.Success)
                    partsPositions.Add(new SelectPartPosition
                    {
                        index = m.Index,
                        part = selectPart.Value
                    });
            }
            partsPositions.Sort((x, y) => x.index.CompareTo(y.index));
            queryText = propertiesRegex.Replace(queryText, delegate(Match m)
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
                return GetColumnName(properties, functionName, GetSelectPart(m.Index, partsPositions));
            });
            queryText = tableNameRegex.Replace(queryText,
                m => m.Groups[1].Value + " " + GetSql(m.Groups[3].Value));
            queryText = functions.Aggregate(queryText, (s, f) => functionRegexes[f.Key]
                .Replace(s, m => f.Value(this, m.Groups[1].Value)));
            return queryText;
        }

        private static SelectPart GetSelectPart(int index, List<SelectPartPosition> positions)
        {
            for (var i = positions.Count - 1; i >= 0; i--)
            {
                var position = positions[i];
                if (index > position.index)
                    return position.part;
            }
            throw new InvalidOperationException("asserton failure");
        }

        private string GetEnumValueSql(string enumValue)
        {
            var enumValueItems = enumValue.Split('.');
            var table = CreateQueryEntity(enumValueItems[0] + "." + enumValueItems[1]);
            var selectClause = CreateSelectClause(table);
            selectClause.Columns.Add(new SelectColumn
            {
                Name = table.GetColumnName("Ссылка"),
                TableName = GetQueryEntityAlias(table)
            });
            var enumMappingsJoinClause = CreateEnumMappingsJoinClause(table);
            selectClause.JoinClauses.Add(enumMappingsJoinClause);
            selectClause.WhereFilters.Add(new ColumnFilter
            {
                ColumnName = "enumValueName",
                ColumnTableName = enumMappingsJoinClause.TableAlias,
                ComparandConstantValue = enumValueItems[2].QuoteSql()
            });
            return selectClause.GetSql();
        }

        private QueryEntity GetQueryEntity(string alias)
        {
            QueryEntity mainEntity;
            if (!queryTables.TryGetValue(alias, out mainEntity))
            {
                const string messageFormat = "can't find query table by alias [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, alias));
            }
            return mainEntity;
        }

        private string PatchJoin(string joinText, int joinPosition, string alias)
        {
            var fromPosition = queryText.LastIndexOf("from", joinPosition, StringComparison.OrdinalIgnoreCase);
            if (fromPosition < 0)
                throw new InvalidOperationException("assertion failure");
            var tableMatch = tableNameRegex.Match(queryText, fromPosition);
            if (!tableMatch.Success)
                throw new InvalidOperationException("assertion failure");
            var mainTableAlias = tableMatch.Groups[3].Value;
            var mainTableEntity = GetQueryEntity(mainTableAlias);
            var joinTableEntity = GetQueryEntity(alias);
            var condition = string.Format("{0}.{1} = {2}.{3} and ",
                mainTableAlias, mainTableEntity.GetAreaColumnName(),
                alias, joinTableEntity.GetAreaColumnName());
            return joinText + condition;
        }

        private string GetColumnName(string[] properties, FunctionName? functionName, SelectPart selectPart)
        {
            var mainEntity = GetQueryEntity(properties[0]);
            var lastEntity = mainEntity;
            QueryEntityProperty lastProperty;
            var subqueryRequired = areas.Length > 0;
            for (var i = 1; i < properties.Length - 1; i++)
            {
                lastProperty = lastEntity.GetOrCreateProperty(properties[i]);
                lastEntity = GetOrCreateQueryEntity(lastProperty, properties);
                subqueryRequired = true;
            }
            lastProperty = lastEntity.GetOrCreateProperty(properties[properties.Length - 1]);
            if (!functionName.HasValue && selectPart == SelectPart.GroupBy)
            {
                var nestedEntity = lastProperty.nestedEntity;
                if (nestedEntity != null && nestedEntity.mapping.IsEnum())
                    if (nestedEntity.properties[0].parts.Contains(SelectPart.Select))
                        functionName = FunctionName.Representation;
            }
            if (functionName.HasValue && string.IsNullOrEmpty(lastProperty.mapping.NestedTableName))
                functionName = null;
            if (functionName.HasValue)
            {
                if (functionName.Value != FunctionName.Representation)
                {
                    const string messageFormat = "unexpected function [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        FormatFunctionName(functionName.Value)));
                }
                lastEntity = GetOrCreateQueryEntity(lastProperty, properties);
                var scope = lastEntity.mapping.ObjectName.HasValue
                    ? lastEntity.mapping.ObjectName.Value.Scope
                    : (ConfigurationScope?) null;
                var validScopes = new ConfigurationScope?[]
                {ConfigurationScope.Перечисления, ConfigurationScope.Справочники};
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
                subqueryRequired = true;
            }
            if (lastProperty.mapping.PropertyName == "ЭтоГруппа")
            {
                lastProperty.functionName = "not";
                subqueryRequired = true;
            }
            if (subqueryRequired)
                mainEntity.subqueryRequired = true;
            lastProperty.referenced = true;
            if (!lastProperty.parts.Contains(selectPart))
                lastProperty.parts.Add(selectPart);
            if (lastProperty.alias == null && subqueryRequired)
                lastProperty.alias = nameGenerator.GenerateColumnName();
            return properties[0] + "." + (lastProperty.alias ?? lastProperty.mapping.ColumnName);
        }

        private QueryEntity GetOrCreateQueryEntity(QueryEntityProperty property, string[] propertyPath)
        {
            if (property.nestedEntity == null)
            {
                string nestedTableName;
                if (property.mapping.PropertyName == "Ссылка")
                {
                    if (property.owner.mapping.Type == TableType.Main)
                        return property.owner;
                    var ownerTableName = property.owner.mapping.QueryTableName;
                    nestedTableName = TableMapping.GetMainQueryNameByTableSectionQueryName(ownerTableName);
                }
                else if (!string.IsNullOrEmpty(property.nestedType))
                    nestedTableName = property.nestedType;
                else
                    nestedTableName = property.mapping.NestedTableName;
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
            var mainEntity = GetQueryEntity(alias);
            string sql;
            if (mainEntity.subqueryRequired)
            {
                var selectClause = CreateSelectClause(mainEntity);
                if (areas.Length > 0)
                    selectClause.WhereFilters.Add(new ColumnFilter
                    {
                        Type = ColumnFilterType.In,
                        ColumnName = mainEntity.GetAreaColumnName(),
                        ColumnTableName = GetQueryEntityAlias(mainEntity),
                        ComparandConstantValues = areas
                    });
                mainEntity.GetOrCreateProperty("ОбластьДанныхОсновныеДанные").referenced = true;
                BuildSubQuery(mainEntity, selectClause);
                sql = selectClause.GetSql();
            }
            else
                sql = mainEntity.mapping.DbTableName;
            return sql + " as " + alias;
        }

        private void BuildSubQuery(QueryEntity entity, SelectClause target)
        {
            foreach (var f in entity.properties)
                AddPropertyToSubquery(f, target);
        }

        private void AddPropertyToSubquery(QueryEntityProperty property, SelectClause target)
        {
            if (property.referenced)
            {
                if (property.owner.mapping.IsEnum())
                {
                    var enumMappingsJoinClause = CreateEnumMappingsJoinClause(property.owner);
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
                    TableName = GetQueryEntityAlias(property.owner),
                    FunctionName = property.functionName
                });
            }
            if (property.nestedEntity != null)
            {
                var joinClause = new JoinClause
                {
                    TableAlias = GetQueryEntityAlias(property.nestedEntity),
                    TableName = property.nestedEntity.mapping.DbTableName,
                    JoinKind = "left"
                };
                if (!property.nestedEntity.mapping.IsEnum())
                    joinClause.EqConditions.Add(new ColumnFilter
                    {
                        ColumnName = property.nestedEntity.GetAreaColumnName(),
                        ColumnTableName = GetQueryEntityAlias(property.nestedEntity),
                        ComparandColumnName = property.owner.GetAreaColumnName(),
                        ComparandTableName = GetQueryEntityAlias(property.owner)
                    });
                var refColumnName = string.IsNullOrEmpty(property.nestedType)
                    ? property.mapping.ColumnName
                    : property.mapping.ColumnName + "_rrref";
                joinClause.EqConditions.Add(new ColumnFilter
                {
                    ColumnName = property.nestedEntity.GetIdColumnName(),
                    ColumnTableName = GetQueryEntityAlias(property.nestedEntity),
                    ComparandColumnName = refColumnName,
                    ComparandTableName = GetQueryEntityAlias(property.owner)
                });
                target.JoinClauses.Add(joinClause);
                BuildSubQuery(property.nestedEntity, target);
            }
        }

        private string GetQueryEntityAlias(QueryEntity entity)
        {
            return entity.alias ?? (entity.alias = nameGenerator.GenerateTableName());
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
            public bool subqueryRequired;

            public QueryEntityProperty GetOrCreateProperty(string name)
            {
                foreach (var f in properties)
                    if (f.mapping.PropertyName.EqualsIgnoringCase(name))
                        return f;
                var result = new QueryEntityProperty(this, mapping.GetByPropertyName(name));
                properties.Add(result);
                return result;
            }

            public string GetAreaColumnName()
            {
                return GetColumnName("ОбластьДанныхОсновныеДанные");
            }

            public string GetIdColumnName()
            {
                return GetColumnName("Ссылка");
            }

            public string GetColumnName(string propertyName)
            {
                return mapping.GetByPropertyName(propertyName).ColumnName;
            }
        }

        private class QueryEntityProperty
        {
            public readonly QueryEntity owner;
            public readonly PropertyMapping mapping;

            public QueryEntityProperty(QueryEntity owner, PropertyMapping mapping)
            {
                this.owner = owner;
                this.mapping = mapping;
            }

            public string alias;
            public bool referenced;
            public QueryEntity nestedEntity;
            public string functionName;
            public readonly List<SelectPart> parts = new List<SelectPart>();
            public string nestedType;
        }

        private JoinClause CreateEnumMappingsJoinClause(QueryEntity enumEntity)
        {
            var tableAlias = nameGenerator.GenerateTableName();
            var result = new JoinClause
            {
                TableName = "simple1c__enumMappings",
                TableAlias = tableAlias,
                JoinKind = "left"
            };
            if (!enumEntity.mapping.ObjectName.HasValue)
                throw new InvalidOperationException("assertion failure");
            result.EqConditions.Add(new ColumnFilter
            {
                ColumnName = "enumName",
                ColumnTableName = tableAlias,
                ComparandConstantValue = enumEntity.mapping.ObjectName.Value.Name.QuoteSql()
            });
            result.EqConditions.Add(new ColumnFilter
            {
                ColumnName = "orderIndex",
                ColumnTableName = tableAlias,
                ComparandTableName = GetQueryEntityAlias(enumEntity),
                ComparandColumnName = enumEntity.mapping.GetByPropertyName("Порядок").ColumnName
            });
            return result;
        }

        private enum SelectPart
        {
            Select,
            Where,
            GroupBy,
            Join
        }

        private class SelectPartPosition
        {
            public SelectPart part;
            public int index;
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

            public void Reset()
            {
                lastUsed.Clear();
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