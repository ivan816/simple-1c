using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class LiteralExpression : ISqlElement
    {
        public object Value { get; set; }
        public SqlType? SqlType { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitLiteral(this);
        }
    }
}