using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal abstract class SqlVisitor
    {
        public virtual ISqlElement Visit(ISqlElement element)
        {
            return element.Accept(this);
        }

        public virtual SubqueryClause VisitSubquery(SubqueryClause clause)
        {
            clause.Query = VisitSqlQuery(clause.Query);
            return clause;
        }

        public virtual ISqlElement VisitValueLiteral(ValueLiteralExpression expression)
        {
            return expression;
        }

        public virtual GroupByClause VisitGroupBy(GroupByClause clause)
        {
            VisitEnumerable(clause.Expressions);
            return clause;
        }

        public virtual UnionClause VisitUnion(UnionClause clause)
        {
            clause.SelectClause = VisitSelect(clause.SelectClause);
            return clause;
        }

        public virtual SqlQuery VisitSqlQuery(SqlQuery sqlQuery)
        {
            if (sqlQuery.Unions != null)
                VisitEnumerable(sqlQuery.Unions);
            if (sqlQuery.OrderBy != null)
                sqlQuery.OrderBy = VisitOrderBy(sqlQuery.OrderBy);
            return sqlQuery;
        }

        public virtual SelectClause VisitSelect(SelectClause clause)
        {
            if (clause.Fields != null)
                VisitEnumerable(clause.Fields);
            clause.Source = (IColumnSource) Visit(clause.Source);
            VisitEnumerable(clause.JoinClauses);
            if (clause.WhereExpression != null)
                clause.WhereExpression = VisitWhere(clause.WhereExpression);
            if (clause.GroupBy != null)
                clause.GroupBy = VisitGroupBy(clause.GroupBy);
            if (clause.Having != null)
                clause.Having = VisitHaving(clause.Having);
            return clause;
        }

        //TODO. this trash is only because of selectParts
        public virtual ISqlElement VisitHaving(ISqlElement element)
        {
            return Visit(element);
        }

        //TODO. this trash is only because of selectParts
        public virtual ISqlElement VisitWhere(ISqlElement filter)
        {
            return Visit(filter);
        }

        public virtual SelectFieldExpression VisitSelectField(SelectFieldExpression clause)
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
            Visit(expression.Source);
            return expression;
        }

        public virtual JoinClause VisitJoin(JoinClause clause)
        {
            clause.Source = (IColumnSource) Visit(clause.Source);
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

        public virtual AggregateFunctionExpression VisitAggregateFunction(AggregateFunctionExpression expression)
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

        public virtual ISqlElement VisitIsNullExpression(IsNullExpression expression)
        {
            expression.Argument = Visit(expression.Argument);
            return expression;
        }

        public virtual ISqlElement VisitList(ListExpression listExpression)
        {
            VisitEnumerable(listExpression.Elements);
            return listExpression;
        }

        public virtual SubqueryTable VisitSubqueryTable(SubqueryTable subqueryTable)
        {
            Visit(subqueryTable.Query);
            return subqueryTable;
        }

        public virtual ISqlElement VisitUnary(UnaryExpression unaryExpression)
        {
            Visit(unaryExpression.Argument);
            return unaryExpression;
        }
    }
}