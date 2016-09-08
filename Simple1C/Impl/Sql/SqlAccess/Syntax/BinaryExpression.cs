namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal abstract class BinaryExpression : ISqlElement
    {
        public SqlBinaryOperator Op { get; private set; }
        public ISqlElement Left { get; set; }
        public ISqlElement Right { get; set; }

        protected BinaryExpression(SqlBinaryOperator op)
        {
            Op = op;
        }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitBinary(this);
        }
    }
}