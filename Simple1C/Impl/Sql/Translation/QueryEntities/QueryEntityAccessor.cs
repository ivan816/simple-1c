using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class QueryEntityAccessor
    {
        private readonly QueryEntityRegistry queryEntityRegistry;
        private readonly NameGenerator nameGenerator;
        private const byte configurationItemReferenceType = 8;

        public QueryEntityAccessor(QueryEntityRegistry queryEntityRegistry, NameGenerator nameGenerator)
        {
            this.queryEntityRegistry = queryEntityRegistry;
            this.nameGenerator = nameGenerator;
        }

        public QueryField GetOrCreateQueryField(ColumnReferenceExpression columnReference,
            bool isRepresentation, SelectPart selectPart)
        {
            var queryRoot = queryEntityRegistry.Get(columnReference.Table);
            if (!isRepresentation && selectPart == SelectPart.GroupBy)
            {
                QueryField fieldWithFunction;
                var keyWithFunction = columnReference.Name + "." + true;
                if (queryRoot.fields.TryGetValue(keyWithFunction, out fieldWithFunction))
                    if (fieldWithFunction.parts.Contains(SelectPart.Select))
                        isRepresentation = true;
            }
            var key = columnReference.Name + "." + isRepresentation;
            QueryField field;
            if (!queryRoot.fields.TryGetValue(key, out field))
            {
                var propertyNames = columnReference.Name.Split('.');
                var subqueryRequired = propertyNames.Length > 1;
                var needInvert = false;
                if (propertyNames[propertyNames.Length - 1].EqualsIgnoringCase("ЭтоГруппа"))
                {
                    needInvert = true;
                    subqueryRequired = true;
                }
                var propertiesEnumerator = new PropertiesEnumerator(propertyNames, queryRoot, this);
                var referencedProperties = propertiesEnumerator.Enumerate();
                if (isRepresentation)
                    if (ReplaceWithRepresentation(referencedProperties))
                        subqueryRequired = true;
                string fieldAlias = null;
                if (subqueryRequired)
                {
                    queryRoot.subqueryRequired = true;
                    fieldAlias = nameGenerator.GenerateColumnName();
                }
                foreach (var p in referencedProperties)
                    p.referenced = true;
                field = new QueryField(fieldAlias, referencedProperties.ToArray(), needInvert);
                queryRoot.fields.Add(key, field);
            }
            if (!field.parts.Contains(selectPart))
                field.parts.Add(selectPart);
            return field;
        }

        public ISqlElement GetUnionCondition(QueryEntityProperty property, QueryEntity entity)
        {
            if (property.mapping.UnionLayout == null)
                return null;
            return entity.unionCondition ?? (entity.unionCondition = CreateUnionCondition(property, entity));
        }

        private ISqlElement CreateUnionCondition(QueryEntityProperty property, QueryEntity nestedEntity)
        {
            var typeColumnName = property.mapping.UnionLayout.TypeColumnName;
            if (string.IsNullOrEmpty(typeColumnName))
            {
                const string messageFormat = "type column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
            }
            if (!nestedEntity.mapping.Index.HasValue)
            {
                var message = string.Format("Invalid table name {0}. Table name must contain index.",
                    nestedEntity.mapping.DbTableName);
                throw new InvalidOperationException(message);
            }
            var tableIndexColumnName = property.mapping.UnionLayout.TableIndexColumnName;
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
                        Table = GetTableDeclaration(property.referer)
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
                        Table = GetTableDeclaration(property.referer)
                    },
                    Right = new LiteralExpression
                    {
                        Value = nestedEntity.mapping.Index,
                        SqlType = SqlType.ByteArray
                    }
                }
            };
        }

        public QueryEntityProperty GetOrCreatePropertyIfExists(QueryEntity queryEntity, string name)
        {
            foreach (var f in queryEntity.properties)
                if (f.mapping.PropertyName.EqualsIgnoringCase(name))
                    return f;
            PropertyMapping propertyMapping;
            if (!queryEntity.mapping.TryGetProperty(name, out propertyMapping))
                return null;
            var property = new QueryEntityProperty(queryEntity, propertyMapping);
            if (propertyMapping.SingleLayout != null)
            {
                if (name.EqualsIgnoringCase(PropertyNames.id))
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
                    var nestedTableName = propertyMapping.SingleLayout.NestedTableName;
                    if (!string.IsNullOrEmpty(nestedTableName))
                        AddQueryEntity(property, nestedTableName);
                }
            }
            else
                foreach (var t in propertyMapping.UnionLayout.NestedTables)
                    AddQueryEntity(property, t);
            queryEntity.properties.Add(property);
            return property;
        }

        public TableDeclarationClause GetTableDeclaration(QueryEntity entity)
        {
            return entity.declaration
                   ?? (entity.declaration = new TableDeclarationClause
                   {
                       Name = entity.mapping.DbTableName,
                       Alias = nameGenerator.GenerateTableName()
                   });
        }

        public JoinClause CreateEnumMappingsJoinClause(QueryEntity enumEntity)
        {
            if (!enumEntity.mapping.ObjectName.HasValue)
                throw new InvalidOperationException("assertion failure");
            var declaration = new TableDeclarationClause
            {
                Name = "simple1c__enumMappings",
                Alias = nameGenerator.GenerateTableName()
            };
            return new JoinClause
            {
                Source = declaration,
                JoinKind = JoinKind.Left,
                Condition = new AndExpression
                {
                    Left = new EqualityExpression
                    {
                        Left = new ColumnReferenceExpression
                        {
                            Name = "enumName",
                            Table = declaration
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
                            Table = declaration
                        },
                        Right = new ColumnReferenceExpression
                        {
                            Name = enumEntity.GetSingleColumnName("Порядок"),
                            Table = GetTableDeclaration(enumEntity)
                        }
                    }
                }
            };
        }

        private bool ReplaceWithRepresentation(List<QueryEntityProperty> properties)
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
                    {
                        ConfigurationScope.Перечисления, ConfigurationScope.Справочники
                    };
                    if (!validScopes.Contains(scope))
                    {
                        const string messageFormat = "[ПРЕДСТАВЛЕНИЕ] is only supported for [{0}]";
                        throw new InvalidOperationException(string.Format(messageFormat, validScopes.JoinStrings(",")));
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

        private void AddQueryEntity(QueryEntityProperty referer, string tableName)
        {
            var queryEntity = queryEntityRegistry.CreateQueryEntity(referer, tableName);
            referer.nestedEntities.Add(queryEntity);
        }
    }
}