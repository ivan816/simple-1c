using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class UnaryExpression : ISqlElement
    {
        public ISqlElement Argument { get; set; }
        public UnaryOperator Operator { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitUnary(this);
        }
    }
}