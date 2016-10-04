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

        public override string ToString()
        {
            return string.Format("{0} Value: [{1}], SqlType: [{2}]",
                typeof(LiteralExpression).Name, Value, SqlType);
        }
    }
}