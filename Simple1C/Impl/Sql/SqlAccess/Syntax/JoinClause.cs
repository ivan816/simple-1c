using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class JoinClause
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public string JoinKind { get; set; }
        public JoinEqCondition[] EqConditions { get; set; }

        public void WriteTo(StringBuilder b)
        {
            b.Append("\r\n");
            b.Append(JoinKind);
            b.Append(" join ");
            SqlHelpers.WriteDeclaration(b, TableName, TableAlias);
            b.Append(" on ");
            var isFirst = true;
            foreach (var eq in EqConditions)
            {
                if (isFirst)
                    isFirst = false;
                else
                    b.Append(" and ");
                SqlHelpers.WriteReference(b, TableAlias, eq.ColumnName);
                b.Append(" = ");
                if (eq.ComparandConstantValue != null)
                    b.Append(eq.ComparandConstantValue);
                else
                    SqlHelpers.WriteReference(b, eq.ComparandTableName, eq.ComparandColumnName);
            }
        }
    }
}