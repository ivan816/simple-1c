using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class InExpression : ISqlElement
    {
        public ISqlElement Expression { get; set; }
        public List<ISqlElement> Constant { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitIn(this);
        }
    }
}