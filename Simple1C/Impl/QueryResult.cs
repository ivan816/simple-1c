using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Queries;

namespace Simple1C.Impl
{
    internal class QueryResult : DispatchObject
    {
        public QueryResult(object comObject)
            : base(comObject)
        {
        }

        public QueryResultSelection Select()
        {
            return new QueryResultSelection(Invoke("Select"));
        }

        public ValueTable Unload()
        {
            return new ValueTable(Invoke("Unload"));
        }
    }
}