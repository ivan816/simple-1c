using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectClause : ISqlElement
    {
        public SelectClause()
        {
            JoinClauses = new List<JoinClause>();
            Columns = new List<SelectColumn>();
        }

        public bool IsSelectAll { get; set; }
        public List<SelectColumn> Columns { get; set; }
        public List<JoinClause> JoinClauses { get; private set; }
        public ISqlElement WhereExpression { get; set; }
        public DeclarationClause Table { get; set; }
        public UnionClause Union { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSelect(this);
        }
    }
}