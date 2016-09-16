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

        public virtual ISqlElement VisitSubquery(SubqueryClause clause)
        {
            clause.SelectClause = VisitSelect(clause.SelectClause);
            return clause;
        }

        public virtual ISqlElement VisitValueLiteral(ValueLiteralExpression expression)
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

        public virtual ISqlElement VisitRoot(RootClause rootClause)
        {
            if (rootClause.Unions != null)
                VisitEnumerable(rootClause.Unions);
            if (rootClause.OrderBy != null)
                rootClause.OrderBy = VisitOrderBy(rootClause.OrderBy);
            return rootClause;
        }

        public virtual SelectClause VisitSelect(SelectClause clause)
        {
            if (clause.Top != null)
                clause.Top = Visit(clause.Top);
            if (clause.Fields != null)
                VisitEnumerable(clause.Fields);
            clause.Source = Visit(clause.Source);
            VisitEnumerable(clause.JoinClauses);
            if (clause.WhereExpression != null)
                clause.WhereExpression = VisitWhere(clause.WhereExpression);
            if (clause.GroupBy != null)
                clause.GroupBy = VisitGroupBy(clause.GroupBy);
            if (clause.Having != null)
                clause.Having = VisitHaving(clause.Having);
            return clause;
        }

        public virtual ISqlElement VisitHaving(ISqlElement element)
        {
            return Visit(element);
        }

        public virtual ISqlElement VisitWhere(ISqlElement filter)
        {
            return Visit(filter);
        }

        public virtual SelectFieldElement VisitSelectField(SelectFieldElement clause)
        {
            clause.Expression = Visit(clause.Expression);
            return clause;
        }

        public virtual ISqlElement VisitBinary(BinaryExpression expression)
        {
            expression.Left = Visit(expression.Left);
            expression.Right = Visit(expression.Right);
            return expression;
        }

        public virtual CaseExpression VisitCase(CaseExpression expression)
        {
            if (expression.DefaultValue != null)
                expression.DefaultValue = (LiteralExpression) Visit(expression.DefaultValue);
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
            expression.Column = (ColumnReferenceExpression) Visit(expression.Column);
            VisitEnumerable(expression.Values);
            return expression;
        }

        public virtual JoinClause VisitJoin(JoinClause clause)
        {
            clause.Source = Visit(clause.Source);
            clause.Condition = Visit(clause.Condition);
            return clause;
        }

        public virtual OrderByClause VisitOrderBy(OrderByClause element)
        {
            VisitEnumerable(element.Expressions);
            return element;
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
            if (expression.Argument != null)
                Visit(expression.Argument);
            return expression;
        }

        protected void VisitEnumerable<T>(List<T> elements)
            where T : ISqlElement
        {
            for (var i = 0; i < elements.Count; i++)
                elements[i] = (T) Visit(elements[i]);
        }

        public virtual ISqlElement VisitOrderingElement(OrderByClause.OrderingElement orderingElement)
        {
            orderingElement.Expression = Visit(orderingElement.Expression);
            return orderingElement;
        }

        public virtual ISqlElement VisitRawSql(RawSqlElement sqlElement)
        {
            return sqlElement;
        }

        public virtual ISqlElement VisitIsNullExpression(IsNullExpression expression)
        {
            expression.Argument = Visit(expression.Argument);
            return expression;
        }
    }
}