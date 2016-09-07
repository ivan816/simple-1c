using System.Collections.Generic;
using System.Text;

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

        public void WriteTo(StringBuilder b)
        {
            b.Append("case");
            foreach (var e in Elements)
            {
                b.Append("\r\n\t");
                b.Append("when ");
                e.Condition.WriteTo(b);
                b.Append(" then ");
                e.Value.WriteTo(b);
            }
            if (DefaultValue != null)
            {
                b.Append("\r\n\t");
                b.Append("else ");
                DefaultValue.WriteTo(b);
            }
            b.Append("end");
        }
    }
}