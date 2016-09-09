using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql
{
    internal class QueryToSqlTranslator
    {
        private static readonly Regex dateTimeRegex = new Regex(@"(?<year>\d+)[\,\s]+(?<month>\d+)[\,\s]+(?<day>\d+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+(\S+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex joinRegex = new Regex(@"join\s+\S+\s+as\s+(\S+)\s+on\s+",
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
                {"датавремя", (_, s) => FormatDateTime(s)},
                {"год", (_, s) => string.Format("date_part('year', {0})", s)},
                {"квартал", (_, s) => string.Format("date_trunc('quarter', {0})", s)},
                {"значение", (t, s) => t.GetEnumValueSql(s)}
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

        private readonly Dictionary<string, MainQueryEntity> queryTables =
            new Dictionary<string, MainQueryEntity>(StringComparer.OrdinalIgnoreCase);

        private const byte configurationItemReferenceType = 8;

        private readonly NameGenerator nameGenerator = new NameGenerator();
        private readonly IMappingSource mappingSource;
        private readonly List<ISqlElement> areas;
        private string queryText;

        public QueryToSqlTranslator(IMappingSource mappingSource, int[] areas)
        {
            this.mappingSource = mappingSource;
            if (areas.Length > 0)
                this.areas = areas.Select(x => new LiteralExpression {Value = x})
                    .Cast<ISqlElement>()
                    .ToList();
        }

        public DateTime? CurrentDate { get; set; }

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
            var currentDateString = FormatSqlDate(CurrentDate ?? DateTime.Today);
            queryText = queryText.Replace("&Now", currentDateString);
            queryText = keywordsRegex.Replace(queryText, m => keywordsMap[m.Groups[1].Value]);
            var match = tableNameRegex.Match(queryText);
            while (match.Success)
            {
                var queryName = match.Groups[2].Value;
                var alias = match.Groups[3].Value;
                queryTables.Add(alias,
                    new MainQueryEntity(CreateQueryEntity(null, queryName), areas != null));
                match = match.NextMatch();
            }
            queryText = joinRegex.Replace(queryText, m => PatchJoin(m.Value, m.Index, m.Groups[1].Value));
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
                return SelectProperty(properties, functionName, GetSelectPart(m.Index, partsPositions));
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
            var table = CreateQueryEntity(null, enumValueItems[0] + "." + enumValueItems[1]);
            var selectClause = new SelectClause {Table = GetDeclarationClause(table)};
            selectClause.Columns.Add(new SelectColumn
            {
                Expression = new ColumnReferenceExpression
                {
                    Name = table.GetSingleColumnName("Ссылка"),
                    TableName = GetQueryEntityAlias(table)
                }
            });
            var enumMappingsJoinClause = CreateEnumMappingsJoinClause(table);
            selectClause.JoinClauses.Add(enumMappingsJoinClause);
            selectClause.WhereExpression = new EqualityExpression
            {
                Left = new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    TableName = enumMappingsJoinClause.Table.Alias
                },
                Right = new LiteralExpression
                {
                    Value = enumValueItems[2]
                }
            };
            return SqlFormatter.Format(selectClause);
        }

        private MainQueryEntity GetMainQueryEntity(string alias)
        {
            MainQueryEntity mainEntity;
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
            SelectProperty(new[] {mainTableAlias, "ОбластьДанныхОсновныеДанные"}, null, SelectPart.Join);
            SelectProperty(new[] { alias, "ОбластьДанныхОсновныеДанные" }, null, SelectPart.Join);
            var condition = string.Format("{0}.ОбластьДанныхОсновныеДанные = {1}.ОбластьДанныхОсновныеДанные and ",
                mainTableAlias, alias);
            return joinText + condition;
        }

        private class QueryField
        {
            public readonly string alias;
            public readonly QueryEntityProperty[] properties;
            public readonly string functionName;

            public QueryField(string alias, QueryEntityProperty[] properties, string functionName)
            {
                this.alias = alias;
                this.properties = properties;
                this.functionName = functionName;
            }

            public readonly List<SelectPart> parts = new List<SelectPart>();
        }

        private string SelectProperty(string[] propertyNames, FunctionName? functionName, SelectPart selectPart)
        {
            var mainEntity = GetMainQueryEntity(propertyNames[0]);
            var keyWithoutFunction = string.Join(".", propertyNames);
            if (!functionName.HasValue && selectPart == SelectPart.GroupBy)
            {
                QueryField fieldWithFunction;
                var keyWithFunction = keyWithoutFunction + "." + FunctionName.Representation;
                if (mainEntity.fields.TryGetValue(keyWithFunction, out fieldWithFunction))
                    if (fieldWithFunction.parts.Contains(SelectPart.Select))
                        functionName = FunctionName.Representation;
            }
            var key = keyWithoutFunction + "." + functionName;
            QueryField field;
            if (!mainEntity.fields.TryGetValue(key, out field))
            {
                var subqueryRequired = propertyNames.Length > 2;
                string fieldFunctionName = null;
                if (propertyNames[propertyNames.Length - 1] == "ЭтоГруппа")
                {
                    fieldFunctionName = "not";
                    subqueryRequired = true;
                }
                var referencedProperties = new List<QueryEntityProperty>();
                EnumProperties(propertyNames, mainEntity.queryEntity, 1, referencedProperties);
                if (functionName.HasValue)
                    if (ApplyFunction(referencedProperties, functionName.Value))
                        subqueryRequired = true;
                string fieldAlias = null;
                if (subqueryRequired)
                {
                    mainEntity.subqueryRequired = true;
                    fieldAlias = nameGenerator.GenerateColumnName();
                }
                foreach (var p in referencedProperties)
                    p.referenced = true;
                field = new QueryField(fieldAlias, referencedProperties.ToArray(), fieldFunctionName);
                mainEntity.fields.Add(key, field);
            }
            if (!field.parts.Contains(selectPart))
                field.parts.Add(selectPart);
            return propertyNames[0] + "." + (field.alias ?? field.properties[0].mapping.SingleBinding.ColumnName);
        }

        private bool ApplyFunction(List<QueryEntityProperty> properties, FunctionName functionName)
        {
            var result = false;
            for (var i = properties.Count - 1; i >= 0; i--)
            {
                var property = properties[i];
                if (property.nestedEntities.Count == 0)
                    continue;
                properties.RemoveAt(i);
                foreach (var nestedEntity in property.nestedEntities)
                {
                    var scope = nestedEntity.mapping.ObjectName.HasValue
                        ? nestedEntity.mapping.ObjectName.Value.Scope
                        : (ConfigurationScope?) null;
                    var validScopes = new ConfigurationScope?[]
                    {ConfigurationScope.Перечисления, ConfigurationScope.Справочники};
                    if (!validScopes.Contains(scope))
                    {
                        const string messageFormat = "function [{0}] is only supported for [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            FormatFunctionName(functionName), validScopes.JoinStrings(",")));
                    }
                    var propertyName = scope == ConfigurationScope.Справочники ? "Наименование" : "Порядок";
                    var presentationProperty = GetOrCreatePropertyIfExists(nestedEntity, propertyName);
                    if (presentationProperty == null)
                    {
                        const string messageFormat = "entity [{0}] has no property [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            nestedEntity.mapping.QueryTableName, propertyName));
                    }
                    properties.Add(presentationProperty);
                    result = true;
                }
            }
            return result;
        }

        private void EnumProperties(string[] propertyNames, QueryEntity queryEntity, int index,
            List<QueryEntityProperty> properties)
        {
            var propertyName = propertyNames[index];
            var property = GetOrCreatePropertyIfExists(queryEntity, propertyName);
            if (property == null)
                return;
            if (index == propertyNames.Length - 1)
            {
                properties.Add(property);
                return;
            }
            if (property.mapping.Kind == PropertyKind.Single)
            {
                if (property.nestedEntities.Count == 0)
                {
                    const string messageFormat = "property [{0}] has no table mapping, property path [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        property.mapping.PropertyName, propertyNames.JoinStrings(".")));
                }
                EnumProperties(propertyNames, property.nestedEntities[0], index + 1, properties);
            }
            else
                foreach (var p in property.nestedEntities)
                    EnumProperties(propertyNames, p, index + 1, properties);
        }

        private QueryEntityProperty GetOrCreatePropertyIfExists(QueryEntity queryEntity, string name)
        {
            foreach (var f in queryEntity.properties)
                if (f.mapping.PropertyName.EqualsIgnoringCase(name))
                    return f;
            if (!queryEntity.mapping.HasProperty(name))
                return null;
            var propertyMapping = queryEntity.mapping.GetByPropertyName(name);
            var property = new QueryEntityProperty(queryEntity, propertyMapping);
            switch (propertyMapping.Kind)
            {
                case PropertyKind.Single:
                    if (name == "Ссылка")
                    {
                        if (queryEntity.mapping.Type == TableType.TableSection)
                        {
                            var nestedTableName = queryEntity.mapping.QueryTableName;
                            nestedTableName = TableMapping.GetMainQueryNameByTableSectionQueryName(nestedTableName);
                            AddQueryEntity(property, nestedTableName);
                        }
                        else
                            property.nestedEntities.Add(queryEntity);
                    }
                    else
                    {
                        var nestedTableName = propertyMapping.SingleBinding.NestedTableName;
                         if (!string.IsNullOrEmpty(nestedTableName))
                             AddQueryEntity(property, nestedTableName);
                    }
                    break;
                case PropertyKind.UnionReferences:
                    foreach (var t in propertyMapping.UnionBinding.NestedTables)
                        AddQueryEntity(property, t);
                    break;
                default:
                    const string messageFormat = "type [{0}] is not supported";
                    throw new InvalidOperationException(string.Format(messageFormat, propertyMapping.Kind));
            }
            queryEntity.properties.Add(property);
            return property;
        }

        private QueryEntity CreateQueryEntity(QueryEntityProperty referer, string tableName)
        {
            var tableMapping = mappingSource.ResolveTable(tableName);
            return new QueryEntity(tableMapping, referer);
        }

        private void AddQueryEntity(QueryEntityProperty referer, string tableName)
        {
            referer.nestedEntities.Add(CreateQueryEntity(referer, tableName));
        }

        private string GetSql(string alias)
        {
            var mainEntity = GetMainQueryEntity(alias);
            string sql;
            if (mainEntity.subqueryRequired)
            {
                if (Strip(mainEntity.queryEntity) == StripResult.HasNoReference)
                    throw new InvalidOperationException("assertion failure");
                var selectClause = new SelectClause
                {
                    Table = GetDeclarationClause(mainEntity.queryEntity)
                };
                if (areas != null)
                    selectClause.WhereExpression = new InExpression
                    {
                        Expression = new ColumnReferenceExpression
                        {
                            Name = mainEntity.queryEntity.GetAreaColumnName(),
                            TableName = GetQueryEntityAlias(mainEntity.queryEntity)
                        },
                        Constant = areas
                    };
                AddJoinClauses(mainEntity.queryEntity, selectClause);
                AddColumns(mainEntity, selectClause);
                sql = SqlFormatter.Format(selectClause);
            }
            else
                sql = mainEntity.queryEntity.mapping.DbTableName;
            return sql + " as " + alias;
        }

        private void AddColumns(MainQueryEntity entity, SelectClause target)
        {
            foreach (var f in entity.fields.Values)
            {
                var expression = GetFieldExpression(f, target);
                if (f.functionName != null)
                    expression = new UnaryFunctionExpression
                    {
                        FunctionName = f.functionName,
                        Argument = expression
                    };
                target.Columns.Add(new SelectColumn
                {
                    Expression = expression,
                    Alias = f.alias
                });
            }
        }

        private ISqlElement GetFieldExpression(QueryField field, SelectClause selectClause)
        {
            if (field.properties.Length < 1)
                throw new InvalidOperationException("assertion failure");
            if (field.properties.Length == 1)
                return GetPropertyReference(field.properties[0], selectClause);
            var result = new CaseExpression();
            var eqConditions = new List<ISqlElement>();
            foreach (var property in field.properties)
            {
                eqConditions.Clear();
                var entity = property.referer;
                while (entity != null)
                {
                    if (entity.unionCondition != null)
                        eqConditions.Add(entity.unionCondition);
                    entity = entity.referer == null ? null : entity.referer.referer;
                }
                result.Elements.Add(new CaseElement
                {
                    Value = GetPropertyReference(property, selectClause),
                    Condition = eqConditions.Combine()
                });
            }
            return result;
        }

        private ColumnReferenceExpression GetPropertyReference(QueryEntityProperty property, SelectClause selectClause)
        {
            if (property.referer.mapping.IsEnum())
            {
                var enumMappingsJoinClause = CreateEnumMappingsJoinClause(property.referer);
                selectClause.JoinClauses.Add(enumMappingsJoinClause);
                return new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    TableName = enumMappingsJoinClause.Table.Alias
                };
            }
            return new ColumnReferenceExpression
            {
                Name = property.mapping.SingleBinding.ColumnName,
                TableName = GetQueryEntityAlias(property.referer)
            };
        }

        private void AddJoinClauses(QueryEntity entity, SelectClause target)
        {
            foreach (var p in entity.properties)
                foreach (var nestedEntity in p.nestedEntities)
                {
                    if(nestedEntity == entity)
                        continue;
                    var eqConditions = new List<ISqlElement>();
                    if (!nestedEntity.mapping.IsEnum())
                        eqConditions.Add(new EqualityExpression
                        {
                            Left = new ColumnReferenceExpression
                            {
                                Name = nestedEntity.GetAreaColumnName(),
                                TableName = GetQueryEntityAlias(nestedEntity)
                            },
                            Right = new ColumnReferenceExpression
                            {
                                Name = p.referer.GetAreaColumnName(),
                                TableName = GetQueryEntityAlias(p.referer)
                            }
                        });
                    if (p.mapping.Kind == PropertyKind.UnionReferences)
                        eqConditions.Add(nestedEntity.unionCondition = GetUnionCondition(p, nestedEntity));
                    var referenceColumnName = p.mapping.Kind == PropertyKind.Single
                        ? p.mapping.SingleBinding.ColumnName
                        : p.mapping.UnionBinding.ReferenceColumnName;
                    if (string.IsNullOrEmpty(referenceColumnName))
                    {
                        const string messageFormat = "ref column is not defined for [{0}.{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            p.referer.mapping.QueryTableName, p.mapping.PropertyName));
                    }
                    eqConditions.Add(new EqualityExpression
                    {
                        Left = new ColumnReferenceExpression
                        {
                            Name = nestedEntity.GetIdColumnName(),
                            TableName = GetQueryEntityAlias(nestedEntity)
                        },
                        Right = new ColumnReferenceExpression
                        {
                            Name = referenceColumnName,
                            TableName = GetQueryEntityAlias(p.referer)
                        }
                    });
                    var joinClause = new JoinClause
                    {
                        Table = new DeclarationClause
                        {
                            Name = nestedEntity.mapping.DbTableName,
                            Alias = GetQueryEntityAlias(nestedEntity)
                        },
                        JoinKind = JoinKind.Left,
                        Condition = eqConditions.Combine()
                    };
                    target.JoinClauses.Add(joinClause);
                    AddJoinClauses(nestedEntity, target);
                }
        }

        private static StripResult Strip(QueryEntity queryEntity)
        {
            var result = StripResult.HasNoReference;
            for (var i = queryEntity.properties.Count - 1; i >= 0; i--)
            {
                var p = queryEntity.properties[i];
                var propertyReferenced = p.referenced;
                for (var j = p.nestedEntities.Count - 1; j >= 0; j--)
                {
                    var nestedEntity = p.nestedEntities[j];
                    if(nestedEntity == queryEntity)
                        continue;
                    if (Strip(nestedEntity) == StripResult.HasNoReference)
                        p.nestedEntities.RemoveAt(j);
                    else
                        propertyReferenced = true;
                }
                if (propertyReferenced)
                    result = StripResult.HasReferenced;
                else
                    queryEntity.properties.RemoveAt(i);
            }
            return result;
        }

        private ISqlElement GetUnionCondition(QueryEntityProperty property, QueryEntity nestedEntity)
        {
            var typeColumnName = property.mapping.UnionBinding.TypeColumnName;
            if (string.IsNullOrEmpty(typeColumnName))
            {
                const string messageFormat = "type column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
            }
            var tableIndexColumnName = property.mapping.UnionBinding.TableIndexColumnName;
            if (string.IsNullOrEmpty(tableIndexColumnName))
            {
                const string messageFormat = "tableIndex column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
            }
            return new AndExpression
            {
                Left = new EqualityExpression
                {
                    Left = new ColumnReferenceExpression
                    {
                        Name = typeColumnName,
                        TableName = GetQueryEntityAlias(property.referer)
                    },
                    Right = new LiteralExpression
                    {
                        Value = configurationItemReferenceType,
                        SqlType = SqlType.ByteArray
                    }
                },
                Right = new EqualityExpression
                {
                    Left = new ColumnReferenceExpression
                    {
                        Name = tableIndexColumnName,
                        TableName = GetQueryEntityAlias(property.referer)
                    },
                    Right = new LiteralExpression
                    {
                        Value = nestedEntity.mapping.Index,
                        SqlType = SqlType.ByteArray
                    }
                }
            };
        }

        private static string FormatDateTime(string s)
        {
            var m = dateTimeRegex.Match(s);
            if (!m.Success)
            {
                const string messageFormat = "invalid ДАТАВРЕМЯ arguments [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, s));
            }
            var date = new DateTime(m.AsInt("year"), m.AsInt("month"), m.AsInt("day"));
            return FormatSqlDate(date);
        }

        private static string FormatSqlDate(DateTime dateTime)
        {
            return "'" + dateTime.ToString("yyyy-MM-dd") + "'";
        }

        private string GetQueryEntityAlias(QueryEntity entity)
        {
            return entity.alias ?? (entity.alias = nameGenerator.GenerateTableName());
        }

        private DeclarationClause GetDeclarationClause(QueryEntity queryEntity)
        {
            return new DeclarationClause
            {
                Name = queryEntity.mapping.DbTableName,
                Alias = GetQueryEntityAlias(queryEntity)
            };
        }

        private class MainQueryEntity
        {
            public readonly QueryEntity queryEntity;
            public readonly Dictionary<string, QueryField> fields = new Dictionary<string, QueryField>();
            public bool subqueryRequired;

            public MainQueryEntity(QueryEntity queryEntity, bool subqueryRequired)
            {
                this.queryEntity = queryEntity;
                this.subqueryRequired = subqueryRequired;
            }
        }

        private class QueryEntity
        {
            public QueryEntity(TableMapping mapping, QueryEntityProperty referer)
            {
                this.mapping = mapping;
                this.referer = referer;
            }

            public readonly TableMapping mapping;
            public readonly QueryEntityProperty referer;
            public string alias;
            public readonly List<QueryEntityProperty> properties = new List<QueryEntityProperty>();
            public ISqlElement unionCondition;

            public string GetAreaColumnName()
            {
                return GetSingleColumnName("ОбластьДанныхОсновныеДанные");
            }

            public string GetIdColumnName()
            {
                return GetSingleColumnName("Ссылка");
            }

            public string GetSingleColumnName(string propertyName)
            {
                return mapping.GetByPropertyName(propertyName).SingleBinding.ColumnName;
            }
        }

        private class QueryEntityProperty
        {
            public readonly QueryEntity referer;
            public readonly PropertyMapping mapping;
            public readonly List<QueryEntity> nestedEntities = new List<QueryEntity>();
            public bool referenced;

            public QueryEntityProperty(QueryEntity referer, PropertyMapping mapping)
            {
                this.referer = referer;
                this.mapping = mapping;
            }
        }

        private JoinClause CreateEnumMappingsJoinClause(QueryEntity enumEntity)
        {
            var tableAlias = nameGenerator.GenerateTableName();
            if (!enumEntity.mapping.ObjectName.HasValue)
                throw new InvalidOperationException("assertion failure");
            return new JoinClause
            {
                Table = new DeclarationClause
                {
                    Name = "simple1c__enumMappings",
                    Alias = tableAlias
                },
                JoinKind = JoinKind.Left,
                Condition = new AndExpression
                {
                    Left = new EqualityExpression
                    {
                        Left = new ColumnReferenceExpression
                        {
                            Name = "enumName",
                            TableName = tableAlias
                        },
                        Right = new LiteralExpression
                        {
                            Value = enumEntity.mapping.ObjectName.Value.Name
                        }
                    },
                    Right = new EqualityExpression
                    {
                        Left = new ColumnReferenceExpression
                        {
                            Name = "orderIndex",
                            TableName = tableAlias
                        },
                        Right = new ColumnReferenceExpression
                        {
                            Name = enumEntity.GetSingleColumnName("Порядок"),
                            TableName = GetQueryEntityAlias(enumEntity)
                        }
                    }
                }
            };
        }

        private enum StripResult
        {
            HasReferenced,
            HasNoReference
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
                var number =
                    lastUsed[prefix] = lastUsed.TryGetValue(prefix, out lastUsedForPrefix) ? lastUsedForPrefix + 1 : 0;
                return prefix + number;
            }
        }
    }
}