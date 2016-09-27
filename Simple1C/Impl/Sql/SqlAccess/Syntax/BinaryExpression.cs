using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class BinaryExpression : ISqlElement
    {
        public SqlBinaryOperator Operator { get; private set; }
        public ISqlElement Left { get; set; }
        public ISqlElement Right { get; set; }

        public BinaryExpression(SqlBinaryOperator op)
        {
            Operator = op;
        }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitBinary(this);
        }
    }
}