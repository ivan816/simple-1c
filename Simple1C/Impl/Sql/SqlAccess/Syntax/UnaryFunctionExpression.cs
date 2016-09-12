namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class UnaryFunctionExpression : ISqlElement
    {
        public UnaryFunctionName FunctionName { get; set; }
        public ISqlElement Argument { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitUnary(this);
        }
    }   
}