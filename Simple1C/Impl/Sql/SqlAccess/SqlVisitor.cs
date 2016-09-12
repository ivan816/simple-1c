using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal abstract class SqlVisitor
    {
        public ISqlElement Visit(ISqlElement element)
        {
            return element.Accept(this);
        }

        public virtual ISqlElement VisitGroupBy(GroupByClause clause)
        {
            VisitEnumerable(clause.Columns);
            return clause;
        }

        public virtual ISqlElement VisitUnion(UnionClause clause)
        {
            Visit(clause.SelectClause);
            return clause;
        }

        public virtual ISqlElement VisitSelect(SelectClause clause)
        {
            Visit(clause.Table);
            VisitEnumerable(clause.JoinClauses);
            if (clause.WhereExpression != null)
                Visit(clause.WhereExpression);
            if (clause.Columns != null)
                VisitEnumerable(clause.Columns);
            if (clause.GroupBy != null)
                Visit(clause.GroupBy);
            if (clause.Union != null)
                Visit(clause.Union);
            return clause;
        }

        public virtual ISqlElement VisitSelectColumn(SelectColumn clause)
        {
            Visit(clause.Expression);
            return clause;
        }

        public virtual ISqlElement VisitBinary(BinaryExpression expression)
        {
            Visit(expression.Left);
            Visit(expression.Right);
            return expression;
        }

        public virtual ISqlElement VisitCase(CaseExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitDeclaration(DeclarationClause clause)
        {
            return clause;
        }

        public virtual ISqlElement VisitIn(InExpression expression)
        {
            Visit(expression.Column);
            VisitEnumerable(expression.Values);
            return expression;
        }

        public virtual ISqlElement VisitJoin(JoinClause clause)
        {
            Visit(clause.Table);
            Visit(clause.Condition);
            return clause;
        }

        public virtual ISqlElement VisitLiteral(LiteralExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitUnary(UnaryFunctionExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitAggregateFunction(AggregateFunction expression)
        {
            return expression;
        }

        private void VisitEnumerable(IEnumerable<ISqlElement> elements)
        {
            foreach (var el in elements)
                Visit(el);
        }
    }
}