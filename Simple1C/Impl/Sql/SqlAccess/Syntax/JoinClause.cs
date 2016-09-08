namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class JoinClause : ISqlElement
    {
        public DeclarationClause Table { get; set; }
        public JoinKind JoinKind { get; set; }
        public ISqlElement Condition { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitJoin(this);
        }
    }
}