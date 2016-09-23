using System;
using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class ColumnReferenceRewriter : SqlVisitor
    {
        private readonly QueryEntityAccessor queryEntityAccessor;
        private bool isPresentation;
        private SelectPart? currentPart;
        private readonly HashSet<ColumnReferenceExpression> rewritten = new HashSet<ColumnReferenceExpression>(); 

        public ColumnReferenceRewriter(QueryEntityAccessor queryEntityAccessor)
        {
            this.queryEntityAccessor = queryEntityAccessor;
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            if (rewritten.Contains(expression))
                return expression;
            rewritten.Add(expression);
            if (!currentPart.HasValue)
                throw new InvalidOperationException("assertion failure");
            var queryField = queryEntityAccessor.GetOrCreateQueryField(expression,
                isPresentation, currentPart.Value);
            expression.Name = queryField.alias ?? queryField.properties[0].GetDbColumnName();
            return expression;
        }

        private void WithCurrentPart(SelectPart part, Action handle)
        {
            var oldPart = currentPart;
            currentPart = part;
            handle();
            currentPart = oldPart;
        }

        public override SelectFieldExpression VisitSelectField(SelectFieldExpression clause)
        {
            WithCurrentPart(SelectPart.Select, () => base.VisitSelectField(clause));
            return clause;
        }

        public override ISqlElement VisitWhere(ISqlElement element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitWhere(element));
            return element;
        }

        public override GroupByClause VisitGroupBy(GroupByClause element)
        {
            WithCurrentPart(SelectPart.GroupBy, () => base.VisitGroupBy(element));
            return element;
        }

        public override JoinClause VisitJoin(JoinClause element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitJoin(element));
            return element;
        }

        public override OrderByClause VisitOrderBy(OrderByClause element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitOrderBy(element));
            return element;
        }

        public override ISqlElement VisitHaving(ISqlElement element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitHaving(element));
            return element;
        }

        public override ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            isPresentation = expression.KnownFunction == KnownQueryFunction.Presentation;
            base.VisitQueryFunction(expression);
            isPresentation = false;
            return expression;
        }
    }
}