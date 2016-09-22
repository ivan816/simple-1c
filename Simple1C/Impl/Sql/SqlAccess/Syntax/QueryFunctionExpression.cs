using System.Collections.Generic;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class QueryFunctionExpression : ISqlElement
    {
        public QueryFunctionExpression()
        {
            Arguments = new List<ISqlElement>();
        }

        public KnownQueryFunction Function { get; set; }
        public List<ISqlElement> Arguments { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitQueryFunction(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. {1}({2})", typeof (QueryFunctionExpression).Name, Function, Arguments.JoinStrings(","));
        }
    }
}