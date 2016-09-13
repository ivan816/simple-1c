using System;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class ColumnReferenceRewriter : SingleSelectSqlVisitorBase
    {
        private readonly QueryEntityAccessor queryEntityAccessor;
        private bool isPresentation;
        private SelectPart? currentPart;

        public ColumnReferenceRewriter(QueryEntityAccessor queryEntityAccessor)
        {
            this.queryEntityAccessor = queryEntityAccessor;
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            if (!currentPart.HasValue)
                throw new InvalidOperationException("assertion failure");
            var queryField = queryEntityAccessor.GetOrCreateQueryField(expression.Declaration,
                expression.Name.Split('.'), isPresentation,
                currentPart.GetValueOrDefault());
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

        public override SelectFieldElement VisitSelectField(SelectFieldElement clause)
        {
            WithCurrentPart(SelectPart.Select, () => base.VisitSelectField(clause));
            return clause;
        }

        public override ISqlElement VisitWhere(ISqlElement element)
        {
            WithCurrentPart(SelectPart.Where, () => base.VisitWhere(element));
            return element;
        }

        public override GroupByClause VisitGroupBy(GroupByClause element)
        {
            WithCurrentPart(SelectPart.GroupBy, () => base.VisitGroupBy(element));
            return element;
        }

        public override JoinClause VisitJoin(JoinClause element)
        {
            WithCurrentPart(SelectPart.Join, () => base.VisitJoin(element));
            return element;
        }

        public override ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            isPresentation = expression.FunctionName == QueryFunctionName.Presentation;
            base.VisitQueryFunction(expression);
            isPresentation = false;
            return expression;
        }
    }
}