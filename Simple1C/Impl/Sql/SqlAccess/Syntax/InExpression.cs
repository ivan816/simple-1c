using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class InExpression : ISqlElement
    {
        public ColumnReferenceExpression Column { get; set; }
        public List<ISqlElement> Values { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitIn(this);
        }
    }
}