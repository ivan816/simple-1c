using System.Collections.Generic;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class CaseExpression : ISqlElement
    {
        public List<CaseElement> Elements { get; private set; }
        public LiteralExpression DefaultValue { get; set; }

        public CaseExpression()
        {
            Elements = new List<CaseElement>();
        }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitCase(this);
        }
    }
}