using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class QueryEntityTree
    {
        private readonly QueryEntityRegistry queryEntityRegistry;
        private readonly NameGenerator nameGenerator;
        private const byte configurationItemReferenceType = 8;

        public QueryEntityTree(QueryEntityRegistry queryEntityRegistry, NameGenerator nameGenerator)
        {
            this.queryEntityRegistry = queryEntityRegistry;
            this.nameGenerator = nameGenerator;
        }

        public QueryRoot Get(IColumnSource declaration)
        {
            return queryEntityRegistry.Get(declaration);
        }

        public QueryEntity CreateQueryEntity(QueryEntityProperty referer, string queryName)
        {
            return queryEntityRegistry.CreateQueryEntity(referer, queryName);
        }

        public ISqlElement GetUnionCondition(QueryEntityProperty property, QueryEntity entity)
        {
            if (property.mapping.UnionLayout == null)
                return null;
            return entity.unionCondition ?? (entity.unionCondition = CreateUnionCondition(property, entity));
        }

        public List<QueryEntityProperty> GetProperties(QueryRoot queryRoot, string[] propertyNames)
        {
            var enumerator = new PropertiesEnumerator(propertyNames, queryRoot, this);
            return enumerator.Enumerate();
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

        private ISqlElement CreateUnionCondition(QueryEntityProperty property, QueryEntity nestedEntity)
        {
            if (!nestedEntity.mapping.Index.HasValue)
            {
                const string messageFormat = "invalid table name [{0}], table name must contain index";
                throw new InvalidOperationException(string.Format(messageFormat,
                    nestedEntity.mapping.DbTableName));
            }
            var tableIndexColumnName = property.mapping.UnionLayout.TableIndexColumnName;
            if (string.IsNullOrEmpty(tableIndexColumnName))
            {
                const string messageFormat = "tableIndex column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
            }
            ISqlElement result = new EqualityExpression
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
            };
            var typeColumnName = property.mapping.UnionLayout.TypeColumnName;
            if (!string.IsNullOrEmpty(typeColumnName))
                result = new AndExpression
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
                    Right = result
                };
            return result;
        }

        private void AddQueryEntity(QueryEntityProperty referer, string tableName)
        {
            var queryEntity = queryEntityRegistry.CreateQueryEntity(referer, tableName);
            referer.nestedEntities.Add(queryEntity);
        }

        private class PropertiesEnumerator
        {
            private readonly string[] propertyNames;
            private readonly QueryRoot queryRoot;
            private readonly QueryEntityTree queryEntityTree;
            private readonly List<QueryEntityProperty> properties = new List<QueryEntityProperty>();

            public PropertiesEnumerator(string[] propertyNames, QueryRoot queryRoot, QueryEntityTree queryEntityTree)
            {
                this.propertyNames = propertyNames;
                this.queryRoot = queryRoot;
                this.queryEntityTree = queryEntityTree;
            }

            public List<QueryEntityProperty> Enumerate()
            {
                Iterate(0, queryRoot.entity);
                if (properties.Count == 0)
                {
                    var tableDeclaration = queryRoot.tableDeclaration as TableDeclarationClause;
                    var tableDescription = tableDeclaration != null ? tableDeclaration.GetRefName() : "(subquery)";
                    const string messageFormat = "no properties found for [{0}.{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat, tableDescription,
                        propertyNames.JoinStrings(".")));
                }
                return properties;
            }

            private void Iterate(int index, QueryEntity entity)
            {
                var property = queryEntityTree.GetOrCreatePropertyIfExists(entity, propertyNames[index]);
                if (property == null)
                    return;
                if (index == propertyNames.Length - 1)
                    properties.Add(property);
                else if (property.mapping.UnionLayout != null)
                {
                    var count = properties.Count;
                    foreach (var p in property.nestedEntities)
                        Iterate(index + 1, p);
                    if (properties.Count == count)
                    {
                        var tableDeclaration = queryRoot.tableDeclaration as TableDeclarationClause;
                        var tableDescription = tableDeclaration != null ? tableDeclaration.Alias : "(subqyery)";
                        const string messageFormat = "property [{0}] in [{1}.{2}] has multiple types [{3}] " +
                                                     "and none of them has property [{4}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            propertyNames[index], tableDescription, propertyNames.JoinStrings("."),
                            property.nestedEntities.Select(x => x.mapping.QueryTableName).JoinStrings(","),
                            propertyNames[index + 1]));
                    }
                }
                else if (property.nestedEntities.Count == 1)
                    Iterate(index + 1, property.nestedEntities[0]);
                else
                {
                    const string messageFormat = "property [{0}] has no table mapping, property path [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        property.mapping.PropertyName, propertyNames.JoinStrings(".")));
                }
            }
        }
    }
}