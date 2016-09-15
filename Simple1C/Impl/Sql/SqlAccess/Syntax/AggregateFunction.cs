namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class AggregateFunction : ISqlElement
    {
        public string Function { get; set; }
        public ISqlElement Argument { get; set; }
        public bool IsSelectAll { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitAggregateFunction(this);
        }
    }
}