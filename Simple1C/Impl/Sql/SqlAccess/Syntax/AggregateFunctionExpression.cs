using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class AggregateFunctionExpression : ISqlElement
    {
        public AggregationFunction Function { get; set; }
        public ISqlElement Argument { get; set; }
        public bool IsSelectAll { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitAggregateFunction(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. {1}({2})", typeof (AggregateFunctionExpression).Name, Function,
                IsSelectAll ? "*" : Argument.ToString());
        }
    }
}