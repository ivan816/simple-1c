using System;
using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class TableDeclarationRewriter
    {
        private readonly QueryEntityRegistry queryEntityRegistry;
        private readonly QueryEntityAccessor queryEntityAccessor;
        private readonly List<ISqlElement> areas;
        private const byte configurationItemReferenceType = 8;

        public TableDeclarationRewriter(QueryEntityRegistry queryEntityRegistry,
            QueryEntityAccessor queryEntityAccessor,
            List<ISqlElement> areas)
        {
            this.queryEntityRegistry = queryEntityRegistry;
            this.queryEntityAccessor = queryEntityAccessor;
            this.areas = areas;
        }

        public ISqlElement Rewrite(TableDeclarationClause declaration)
        {
            var queryRoot = queryEntityRegistry.Get(declaration);
            var subqueryRequired = queryRoot.subqueryRequired || areas != null;
            if (!subqueryRequired)
            {
                declaration.Name = queryRoot.entity.mapping.DbTableName;
                return declaration;
            }
            if (Strip(queryRoot.entity) == StripResult.HasNoReferences)
                throw new InvalidOperationException("assertion failure");
            var selectClause = new SelectClause
            {
                Source = queryEntityAccessor.GetTableDeclaration(queryRoot.entity)
            };
            if (areas != null)
                selectClause.WhereExpression = new InExpression
                {
                    Column = new ColumnReferenceExpression
                    {
                        Name = queryRoot.entity.GetAreaColumnName(),
                        Declaration = (TableDeclarationClause)selectClause.Source
                    },
                    Values = areas
                };
            AddJoinClauses(queryRoot.entity, selectClause);
            AddColumns(queryRoot, selectClause);
            return new SubqueryClause
            {
                SelectClause = selectClause,
                Alias = declaration.GetRefName()
            };
        }

        private void AddJoinClauses(QueryEntity entity, SelectClause target)
        {
            foreach (var p in entity.properties)
                foreach (var nestedEntity in p.nestedEntities)
                {
                    if (nestedEntity == entity)
                        continue;
                    var eqConditions = new List<ISqlElement>();
                    if (!nestedEntity.mapping.IsEnum())
                        eqConditions.Add(new EqualityExpression
                        {
                            Left = new ColumnReferenceExpression
                            {
                                Name = nestedEntity.GetAreaColumnName(),
                                Declaration = queryEntityAccessor.GetTableDeclaration(nestedEntity)
                            },
                            Right = new ColumnReferenceExpression
                            {
                                Name = p.referer.GetAreaColumnName(),
                                Declaration = queryEntityAccessor.GetTableDeclaration(p.referer)
                            }
                        });
                    if (p.mapping.UnionLayout != null)
                        eqConditions.Add(nestedEntity.unionCondition = GetUnionCondition(p, nestedEntity));
                    var referenceColumnName = p.mapping.SingleLayout == null
                        ? p.mapping.UnionLayout.ReferenceColumnName
                        : p.mapping.SingleLayout.ColumnName;
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
                            Declaration = queryEntityAccessor.GetTableDeclaration(nestedEntity)
                        },
                        Right = new ColumnReferenceExpression
                        {
                            Name = referenceColumnName,
                            Declaration = queryEntityAccessor.GetTableDeclaration(p.referer)
                        }
                    });
                    var joinClause = new JoinClause
                    {
                        Source = queryEntityAccessor.GetTableDeclaration(nestedEntity),
                        JoinKind = JoinKind.Left,
                        Condition = eqConditions.Combine()
                    };
                    target.JoinClauses.Add(joinClause);
                    AddJoinClauses(nestedEntity, target);
                }
        }

        private void AddColumns(QueryRoot root, SelectClause target)
        {
            foreach (var f in root.fields.Values)
            {
                var expression = GetFieldExpression(f, target);
                if (f.invert)
                    expression = new QueryFunctionExpression
                    {
                        FunctionName = QueryFunctionName.SqlNot,
                        Arguments = new List<ISqlElement> { expression }
                    };
                target.Fields.Add(new SelectFieldElement
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
                var enumMappingsJoinClause = queryEntityAccessor.CreateEnumMappingsJoinClause(property.referer);
                selectClause.JoinClauses.Add(enumMappingsJoinClause);
                return new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    Declaration = (TableDeclarationClause)enumMappingsJoinClause.Source
                };
            }
            return new ColumnReferenceExpression
            {
                Name = property.GetDbColumnName(),
                Declaration = queryEntityAccessor.GetTableDeclaration(property.referer)
            };
        }

        private static StripResult Strip(QueryEntity queryEntity)
        {
            var result = StripResult.HasNoReferences;
            for (var i = queryEntity.properties.Count - 1; i >= 0; i--)
            {
                var p = queryEntity.properties[i];
                var propertyReferenced = p.referenced;
                for (var j = p.nestedEntities.Count - 1; j >= 0; j--)
                {
                    var nestedEntity = p.nestedEntities[j];
                    if (nestedEntity == queryEntity)
                        continue;
                    if (Strip(nestedEntity) == StripResult.HasNoReferences)
                        p.nestedEntities.RemoveAt(j);
                    else
                        propertyReferenced = true;
                }
                if (propertyReferenced)
                    result = StripResult.HasReferences;
                else
                    queryEntity.properties.RemoveAt(i);
            }
            return result;
        }

        private ISqlElement GetUnionCondition(QueryEntityProperty property, QueryEntity nestedEntity)
        {
            var typeColumnName = property.mapping.UnionLayout.TypeColumnName;
            if (string.IsNullOrEmpty(typeColumnName))
            {
                const string messageFormat = "type column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.referer.mapping.QueryTableName, property.mapping.PropertyName));
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
                        Declaration = queryEntityAccessor.GetTableDeclaration(property.referer)
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
                        Declaration = queryEntityAccessor.GetTableDeclaration(property.referer)
                    },
                    Right = new LiteralExpression
                    {
                        Value = nestedEntity.mapping.Index,
                        SqlType = SqlType.ByteArray
                    }
                }
            };
        }

        private enum StripResult
        {
            HasReferences,
            HasNoReferences
        }
    }
}