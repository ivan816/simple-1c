using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal interface ISqlElement
    {
        void WriteTo(StringBuilder b);
    }

    internal class JoinClause : ISqlElement
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public string JoinKind { get; set; }
        public List<ColumnFilter> EqConditions { get; private set; }

        public JoinClause()
        {
            EqConditions = new List<ColumnFilter>();
        }

        public void WriteTo(StringBuilder b)
        {
            b.Append(JoinKind);
            b.Append(" join ");
            SqlHelpers.WriteDeclaration(b, TableName, TableAlias);
            b.Append(" on ");
            SqlHelpers.WriteFilters(b, EqConditions);
        }
    }
}