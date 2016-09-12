namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class UnionClause : ISqlElement
    {
        public SelectClause SelectClause { get; set; }
        public UnionType Type { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitUnion(this);
        }
    }
}