namespace Simple1C.Impl.Sql.SqlBuilders
{
    public class JoinClause
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public string JoinKind { get; set; }
        public JoinEqCondition[] EqConditions { get; set; }
    }
}