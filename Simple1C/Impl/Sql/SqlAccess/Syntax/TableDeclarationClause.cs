using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class TableDeclarationClause : IColumnSource
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

        public override string ToString()
        {
            return string.Format("{0} Name: [{1}], Alias: [{2}]", typeof(TableDeclarationClause).Name, Name, Alias);
        }
    }
}