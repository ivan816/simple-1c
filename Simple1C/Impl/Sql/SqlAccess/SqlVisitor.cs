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

        public virtual ISqlElement VisitValueLiteral(ValueLiteral expression)
        {
            return expression;
        }
        
        public virtual GroupByClause VisitGroupBy(GroupByClause clause)
        {
            VisitEnumerable(clause.Columns);
            return clause;
        }

        public virtual UnionClause VisitUnion(UnionClause clause)
        {
            clause.SelectClause = VisitSelect(clause.SelectClause);
            return clause;
        }

        public virtual SelectClause VisitSelect(SelectClause clause)
        {
            clause.Source = Visit(clause.Source);
            VisitEnumerable(clause.JoinClauses);
            if (clause.WhereExpression != null)
                clause.WhereExpression = VisitWhere(clause.WhereExpression);
            if (clause.Fields != null)
                VisitEnumerable(clause.Fields);
            if (clause.GroupBy != null)
                clause.GroupBy = VisitGroupBy(clause.GroupBy);
            if (clause.Union != null)
                clause.Union = VisitUnion(clause.Union);
            return clause;
        }

        public virtual ISqlElement VisitWhere(ISqlElement filter)
        {
            Visit(filter);
            return filter;
        }

        public virtual SelectField VisitSelectField(SelectField clause)
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

        public virtual CaseExpression VisitCase(CaseExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            return clause;
        }

        public virtual ISqlElement VisitIn(InExpression expression)
        {
            Visit(expression.Column);
            VisitEnumerable(expression.Values);
            return expression;
        }

        public virtual JoinClause VisitJoin(JoinClause clause)
        {
            clause.Source = Visit(clause.Source);
            Visit(clause.Condition);
            return clause;
        }

        public virtual ISqlElement VisitLiteral(LiteralExpression expression)
        {
            return expression;
        }

        public virtual ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            VisitEnumerable(expression.Arguments);
            return expression;
        }

        public virtual AggregateFunction VisitAggregateFunction(AggregateFunction expression)
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