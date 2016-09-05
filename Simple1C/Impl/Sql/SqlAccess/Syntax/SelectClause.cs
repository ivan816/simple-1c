using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectClause
    {
        public SelectClause(string tableName, string tableAlias)
        {
            TableName = tableName;
            TableAlias = tableAlias;
            JoinClauses = new List<JoinClause>();
            Columns = new List<SelectColumn>();
            WhereFilters = new List<ColumnFilter>();
        }

        public List<SelectColumn> Columns { get; private set; }
        public List<JoinClause> JoinClauses { get; private set; }
        public List<ColumnFilter> WhereFilters { get; private set; }
        public string TableName { get; private set; }
        public string TableAlias { get; private set; }

        public string GetSql()
        {
            var b = new StringBuilder();
            b.Append("(select\r\n\t");
            SqlHelpers.WriteElements(Columns, ",\r\n\t", b);
            b.Append("\r\nfrom ");
            SqlHelpers.WriteDeclaration(b, TableName, TableAlias);
            if (JoinClauses.Count > 0)
            {
                b.Append("\r\n");
                SqlHelpers.WriteElements(JoinClauses, "\r\n", b);    
            }
            if (WhereFilters.Count > 0)
            {
                b.Append("\r\nwhere ");
                SqlHelpers.WriteFilters(b, WhereFilters);
            }
            b.Append(")");
            return b.ToString();
        }
    }
}