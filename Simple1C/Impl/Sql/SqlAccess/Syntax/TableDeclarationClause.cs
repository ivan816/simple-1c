namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class TableDeclarationClause : ISqlElement
    {
        public string Name { get; set; }
        public string Alias { get; set; }

        public string GetRefName()
        {
            return Alias ?? Name;
        }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitTableDeclaration(this);
        }
    }
}