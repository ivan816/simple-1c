namespace Simple1C.Impl.Sql.SqlBuilders
{
    public class JoinClause
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public string JoinKind { get; set; }
        public string LeftFieldTableName { get; set; }
        public string LeftFieldName { get; set; }
        public string RightFieldTableName { get; set; }
        public string RightFieldName { get; set; }
    }
}