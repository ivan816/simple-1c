using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectClause : ISqlElement
    {
        public SelectClause()
        {
            JoinClauses = new List<JoinClause>();
            Fields = new List<SelectFieldElement>();
        }

        public bool IsSelectAll { get; set; }
        public List<SelectFieldElement> Fields { get; set; }
        public ISqlElement WhereExpression { get; set; }
        public ISqlElement Source { get; set; }
        public List<JoinClause> JoinClauses { get; private set; }
        public GroupByClause GroupBy { get; set; }
        public OrderByClause OrderBy { get; set; }
        public UnionClause Union { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSelect(this);
        }
    }

    internal class OrderByClause : ISqlElement
    {
        public List<OrderingElement> Expressions { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitOrderBy(this);
        }

        public class OrderingElement : ISqlElement
        {
            public ISqlElement Expression { get; set; }
            public bool IsAsc { get; set; }

            public ISqlElement Accept(SqlVisitor visitor)
            {
                return visitor.VisitOrderingElement(this);
            }
        }
    }
}