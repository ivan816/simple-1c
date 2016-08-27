using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class JoinClause
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public string JoinKind { get; set; }
        public EqCondition[] EqConditions { get; set; }

        public void WriteTo(StringBuilder b)
        {
            b.Append("\r\n");
            b.Append(JoinKind);
            b.Append(" join ");
            SqlHelpers.WriteDeclaration(b, TableName, TableAlias);
            b.Append(" on ");
            SqlHelpers.WriteEqConditions(b, EqConditions);
        }
    }
}