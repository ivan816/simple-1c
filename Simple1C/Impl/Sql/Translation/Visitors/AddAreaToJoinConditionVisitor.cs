using System.Collections.Generic;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class AddAreaToJoinConditionVisitor : SqlVisitor
    {
        private readonly Stack<Context> contexts = new Stack<Context>();

        public override JoinClause VisitJoin(JoinClause clause)
        {
            var currentContext = contexts.Peek();
            if (currentContext == null || currentContext.MainTable == null || !(clause.Source is TableDeclarationClause))
                return base.VisitJoin(clause);

            clause.Condition = new AndExpression
            {
                Left = new EqualityExpression
                {
                    Left = new ColumnReferenceExpression
                    {
                        Name = PropertyNames.Area,
                        Table = currentContext.MainTable
                    },
                    Right = new ColumnReferenceExpression
                    {
                        Name = PropertyNames.Area,
                        Table = clause.Source
                    }
                },
                Right = clause.Condition
            };
            return base.VisitJoin(clause);
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            contexts.Peek().MainTable = clause;
            return base.VisitTableDeclaration(clause);
        }

        public override SubqueryTable VisitSubqueryTable(SubqueryTable subqueryTable)
        {
            contexts.Peek().MainTable = null;
            return base.VisitSubqueryTable(subqueryTable);
        }

        public override SqlQuery VisitSqlQuery(SqlQuery sqlQuery)
        {
            contexts.Push(new Context());
            var result = base.VisitSqlQuery(sqlQuery);
            contexts.Pop();
            return result;
        }

        private class Context
        {
            public TableDeclarationClause MainTable { get; set; }
        }
    }
}