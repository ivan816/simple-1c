namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class BinaryExpression : ISqlElement
    {
        public SqlBinaryOperator Op { get; private set; }
        public ISqlElement Left { get; set; }
        public ISqlElement Right { get; set; }

        public BinaryExpression(SqlBinaryOperator op)
        {
            Op = op;
        }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitBinary(this);
        }
    }
}