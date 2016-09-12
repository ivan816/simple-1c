namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class AggregateFunction : ISqlElement
    {
        public AggregateFunctionType Type { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitAggregateFunction(this);
        }
    }
}