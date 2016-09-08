using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class InExpression : ISqlElement
    {
        public ISqlElement Expression { get; set; }
        public List<ISqlElement> Constant { get; set; }

        public void WriteTo(StringBuilder b)
        {
            Expression.WriteTo(b);
            b.Append(" in ");
            b.Append('(');
            SqlHelpers.WriteElements(Constant, ",", b);
            b.Append(')');
        }
    }
}