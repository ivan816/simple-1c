using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class QueryFunctionExpression : ISqlElement
    {
        public QueryFunctionName FunctionName { get; set; }
        public List<ISqlElement> Arguments { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitQueryFunction(this);
        }
    }
}