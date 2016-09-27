using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class OrderByAliasInliner : SqlVisitor
    {
        private readonly Stack<Dictionary<string, SelectFieldExpression>> aliasedExpressions =
            new Stack<Dictionary<string, SelectFieldExpression>>();

        private bool inOrderBy;

        public override SelectFieldExpression VisitSelectField(SelectFieldExpression clause)
        {
            if (!string.IsNullOrEmpty(clause.Alias))
                aliasedExpressions.Peek()[clause.Alias] = clause;
            return base.VisitSelectField(clause);
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            return InlineOrNull(expression) ?? base.VisitColumnReference(expression);
        }

        public override SqlQuery VisitSqlQuery(SqlQuery sqlQuery)
        {
            aliasedExpressions.Push(new Dictionary<string, SelectFieldExpression>());
            var result = base.VisitSqlQuery(sqlQuery);
            aliasedExpressions.Pop();
            return result;
        }

        public override OrderByClause VisitOrderBy(OrderByClause element)
        {
            inOrderBy = true;
            var result = base.VisitOrderBy(element);
            inOrderBy = false;
            return result;
        }

        private ISqlElement InlineOrNull(ColumnReferenceExpression expression)
        {
            if (!inOrderBy)
                return null;
            if (string.IsNullOrEmpty(expression.Name))
                return null;
            SelectFieldExpression selectExpression;
            return aliasedExpressions.Peek().TryGetValue(expression.Name, out selectExpression)
                ? selectExpression.Expression
                : null;
        }
    }
}