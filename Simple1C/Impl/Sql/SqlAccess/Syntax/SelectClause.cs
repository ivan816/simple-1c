using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectClause
    {
        public SelectClause()
        {
            JoinClauses = new List<JoinClause>();
            Columns = new List<SelectColumn>();
        }

        public List<SelectColumn> Columns { get; private set; }
        public List<JoinClause> JoinClauses { get; private set; }
        public ISqlElement WhereExpression { get; set; }
        public DeclarationClause Table { get; set; }

        public string GetSql()
        {
            var b = new StringBuilder();
            b.Append("(select\r\n\t");
            SqlHelpers.WriteElements(Columns, ",\r\n\t", b);
            b.Append("\r\nfrom ");
            Table.WriteTo(b);
            if (JoinClauses.Count > 0)
            {
                b.Append("\r\n");
                SqlHelpers.WriteElements(JoinClauses, "\r\n", b);    
            }
            if (WhereExpression != null)
            {
                b.Append("\r\nwhere ");
                WhereExpression.WriteTo(b);
            }
            b.Append(")");
            return b.ToString();
        }
    }
}