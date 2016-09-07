using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class DeclarationClause : ISqlElement
    {
        public string Name { get; set; }
        public string Alias { get; set; }

        public void WriteTo(StringBuilder b)
        {
            b.Append(Name);
            SqlHelpers.WriteAlias(b, Alias);
        }
    }
}