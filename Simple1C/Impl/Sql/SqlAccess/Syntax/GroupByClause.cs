using System.Collections.Generic;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class GroupByClause : ISqlElement
    {
        public List<ISqlElement> Expressions { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitGroupBy(this);
        }
    }
}