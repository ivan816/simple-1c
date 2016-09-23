using System.Collections.Generic;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ListExpression : ISqlElement
    {
        public ListExpression()
        {
            Elements = new List<ISqlElement>();
        }

        public List<ISqlElement> Elements { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitList(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. ({1})", Elements.JoinStrings(","), typeof(ListExpression));
        }
    }
}