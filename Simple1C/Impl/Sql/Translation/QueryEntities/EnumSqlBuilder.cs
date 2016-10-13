using System;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class EnumSqlBuilder
    {
        private readonly QueryEntityTree queryEntityTree;
        private readonly NameGenerator nameGenerator;

        public EnumSqlBuilder(QueryEntityTree queryEntityTree, NameGenerator nameGenerator)
        {
            this.queryEntityTree = queryEntityTree;
            this.nameGenerator = nameGenerator;
        }

        public JoinClause GetJoinSql(QueryEntity enumEntity)
        {
            if (!enumEntity.mapping.ObjectName.HasValue)
                throw new InvalidOperationException("assertion failure");
            var declaration = new TableDeclarationClause
            {
                Name = "simple1c.enumMappings",
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
                            Value = enumEntity.mapping.ObjectName.Value.Name.ToLower()
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
                            Table = queryEntityTree.GetTableDeclaration(enumEntity)
                        }
                    }
                }
            };
        }

        public ISqlElement GetValueSql(string enumName, string enumValueName)
        {
            var table = queryEntityTree.CreateQueryEntity(null, enumName);
            var selectClause = new SelectClause {Source = queryEntityTree.GetTableDeclaration(table)};
            selectClause.Fields.Add(new SelectFieldExpression
            {
                Expression = new ColumnReferenceExpression
                {
                    Name = table.GetIdColumnName(),
                    Table = (TableDeclarationClause) selectClause.Source
                }
            });
            var enumMappingsJoinClause = GetJoinSql(table);
            selectClause.JoinClauses.Add(enumMappingsJoinClause);
            selectClause.WhereExpression = new EqualityExpression
            {
                Left = new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    Table = enumMappingsJoinClause.Source
                },
                Right = new LiteralExpression
                {
                    Value = enumValueName.ToLower()
                }
            };
            return new SubqueryClause
            {
                Query = new SqlQuery
                {
                    Unions = {new UnionClause {SelectClause = selectClause}}
                }
            };
        }
    }
}