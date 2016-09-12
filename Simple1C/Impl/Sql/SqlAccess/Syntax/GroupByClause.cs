using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class GroupByClause : ISqlElement
    {
        public List<ColumnReferenceExpression> Columns { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitGroupBy(this);
        }
    }
}