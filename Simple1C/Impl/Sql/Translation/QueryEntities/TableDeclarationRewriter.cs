using System;
using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.Visitors;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class TableDeclarationRewriter
    {
        private readonly NameGenerator nameGenerator;
        private readonly QueryEntityRegistry queryEntityRegistry;
        private readonly QueryEntityTree queryEntityTree;
        private readonly EnumSqlBuilder enumSqlBuilder;
        private readonly List<ISqlElement> areas;

        public TableDeclarationRewriter(QueryEntityRegistry queryEntityRegistry,
            QueryEntityTree queryEntityTree,
            EnumSqlBuilder enumSqlBuilder,
            NameGenerator nameGenerator, List<ISqlElement> areas)
        {
            this.queryEntityRegistry = queryEntityRegistry;
            this.queryEntityTree = queryEntityTree;
            this.enumSqlBuilder = enumSqlBuilder;
            this.areas = areas;
            this.nameGenerator = nameGenerator;
        }

        public void RewriteTables(ISqlElement element)
        {
            var rewrittenTables = new Dictionary<IColumnSource, IColumnSource>();
            TableDeclarationVisitor.Visit(element, original =>
            {
                var rewritten = RewriteTableIfNeeded(original);
                if (rewritten != original)
                    rewrittenTables.Add(original, rewritten);
                return rewritten;
            });
            new ColumnReferenceVisitor(column =>
            {
                IColumnSource generatedTable;
                if (rewrittenTables.TryGetValue(column.Table, out generatedTable))
                    column.Table = generatedTable;
                return column;
            }).Visit(element);
        }

        private IColumnSource RewriteTableIfNeeded(TableDeclarationClause declaration)
        {
            var queryRoot = queryEntityRegistry.Get(declaration);
            var subqueryRequired = queryRoot.subqueryRequired ||
                                   areas != null ||
                                   queryRoot.additionalFields != null;
            if (!subqueryRequired)
            {
                declaration.Name = queryRoot.entity.mapping.DbTableName;
                return declaration;
            }
            var stripResult = Strip(queryRoot.entity);
            var selectClause = new SelectClause
            {
                Source = queryEntityTree.GetTableDeclaration(queryRoot.entity)
            };
            if (areas != null)
                selectClause.WhereExpression = new InExpression
                {
                    Column = new ColumnReferenceExpression
                    {
                        Name = queryRoot.entity.GetAreaColumnName(),
                        Table = (TableDeclarationClause) selectClause.Source
                    },
                    Source = new ListExpression {Elements = areas}
                };
            if (stripResult == StripResult.HasNoReferences)
                selectClause.IsSelectAll = true;
            else
            {
                AddJoinClauses(queryRoot.entity, selectClause);
                AddColumns(queryRoot, selectClause);
                if (queryRoot.additionalFields != null)
                    foreach (var c in queryRoot.additionalFields)
                        selectClause.Fields.Add(c);
            }
            return new SubqueryTable
            {
                Alias = declaration.Alias ?? nameGenerator.GenerateSubqueryName(),
                Query = new SubqueryClause
                {
                    Query = new SqlQuery
                    {
                        Unions = {new UnionClause {SelectClause = selectClause}}
                    }
                }
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
                                Table = queryEntityTree.GetTableDeclaration(nestedEntity)
                            },
                            Right = new ColumnReferenceExpression
                            {
                                Name = p.referer.GetAreaColumnName(),
                                Table = queryEntityTree.GetTableDeclaration(p.referer)
                            }
                        });
                    var unionCondition = queryEntityTree.GetUnionCondition(p, nestedEntity);
                    if (unionCondition != null)
                        eqConditions.Add(unionCondition);
                    eqConditions.Add(new EqualityExpression
                    {
                        Left = new ColumnReferenceExpression
                        {
                            Name = nestedEntity.GetIdColumnName(),
                            Table = queryEntityTree.GetTableDeclaration(nestedEntity)
                        },
                        Right = new ColumnReferenceExpression
                        {
                            Name = p.GetDbColumnName(),
                            Table = queryEntityTree.GetTableDeclaration(p.referer)
                        }
                    });
                    var joinClause = new JoinClause
                    {
                        Source = queryEntityTree.GetTableDeclaration(nestedEntity),
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
                        KnownFunction = KnownQueryFunction.SqlNot,
                        Arguments = new List<ISqlElement> {expression}
                    };
                target.Fields.Add(new SelectFieldExpression
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
                var enumMappingsJoinClause = enumSqlBuilder.GetJoinSql(property.referer);
                selectClause.JoinClauses.Add(enumMappingsJoinClause);
                return new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    Table = (TableDeclarationClause) enumMappingsJoinClause.Source
                };
            }
            return new ColumnReferenceExpression
            {
                Name = property.GetDbColumnName(),
                Table = queryEntityTree.GetTableDeclaration(property.referer)
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

        private enum StripResult
        {
            HasReferences,
            HasNoReferences
        }

        private class ColumnReferenceVisitor : SqlVisitor
        {
            private readonly Func<ColumnReferenceExpression, ColumnReferenceExpression> visitor;

            public ColumnReferenceVisitor(Func<ColumnReferenceExpression, ColumnReferenceExpression> visitor)
            {
                this.visitor = visitor;
            }

            public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
            {
                return visitor((ColumnReferenceExpression) base.VisitColumnReference(expression));
            }
        }
    }
}