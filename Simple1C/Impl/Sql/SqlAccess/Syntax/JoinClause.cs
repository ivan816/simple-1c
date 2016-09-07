using System;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class JoinClause : ISqlElement
    {
        public DeclarationClause Table { get; set; }
        public JoinKind JoinKind { get; set; }
        public ISqlElement Condition { get; set; }

        public void WriteTo(StringBuilder b)
        {
            b.Append(GetJoinKindString());
            b.Append(" join ");
            Table.WriteTo(b);
            b.Append(" on ");
            Condition.WriteTo(b);
        }

        private string GetJoinKindString()
        {
            switch (JoinKind)
            {
                case JoinKind.Left:
                    return "left";
                case JoinKind.Right:
                    return "right";
                case JoinKind.Inner:
                    return "inner";
                case JoinKind.Outer:
                    return "outer";
                default:
                    const string messageFormat = "unexpected join kind [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, JoinKind));
            }
        }
    }
}