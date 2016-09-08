using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal abstract class SqlVisitor
    {
        public ISqlElement Visit(ISqlElement element)
        {
            return element.Accept(this);
        }

        public virtual ISqlElement VisitSelect(SelectClause clause)
        {
            return clause;
        }
        
        public virtual ISqlElement VisitSelectColumn(SelectColumn clause)
        {
            return clause;
        }

        public virtual ISqlElement VisitBinary(BinaryExpression expression)
        {
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
            return expression;
        }

        public virtual ISqlElement VisitJoin(JoinClause clause)
        {
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
    }
}