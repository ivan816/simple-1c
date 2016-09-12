using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal abstract class SqlVisitor
    {
        public ISqlElement Visit(ISqlElement element)
        {
            return element.Accept(this);
        }

        public virtual ISqlElement VisitUnion(UnionClause clause)
        {
            Visit(clause.SelectClause);
            return clause;
        }

        public virtual ISqlElement VisitSelect(SelectClause clause)
        {
            Visit(clause.Table);
            foreach (var join in clause.JoinClauses)
                Visit(join);
            if (clause.WhereExpression != null)
                Visit(clause.WhereExpression);
            if (clause.Columns != null)
                foreach (var column in clause.Columns)
                    Visit(column);
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
            foreach (var v in expression.Values)
                Visit(v);
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
    }
}