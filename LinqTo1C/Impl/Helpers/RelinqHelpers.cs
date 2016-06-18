using System;
using System.Collections;
using System.Linq;
using LinqTo1C.Impl.Queriables;
using Remotion.Linq.Parsing.Structure;

namespace LinqTo1C.Impl.Helpers
{
    public static class RelinqHelpers
    {
        public static IQueryProvider CreateQueryProvider(Func<BuiltQuery, IEnumerable> execute)
        {
            return new RelinqQueryProvider(QueryParser.CreateDefault(),
                new RelinqQueryExecutor(execute));
        }
    }
}