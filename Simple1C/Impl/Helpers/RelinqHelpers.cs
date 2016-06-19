using System;
using System.Collections;
using System.Linq;
using Remotion.Linq.Parsing.Structure;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl.Helpers
{
    internal static class RelinqHelpers
    {
        public static IQueryProvider CreateQueryProvider(TypeMapper typeMapper, Func<BuiltQuery, IEnumerable> execute)
        {
            return new RelinqQueryProvider(QueryParser.CreateDefault(),
                new RelinqQueryExecutor(typeMapper, execute));
        }
    }
}