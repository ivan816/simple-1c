namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class RawSqlElement : ISqlElement
    {
        public string Sql { get; set; }
        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitRawSql(this);
        }
    }
}