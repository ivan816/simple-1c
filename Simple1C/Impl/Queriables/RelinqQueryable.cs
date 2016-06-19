using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;

namespace Simple1C.Impl.Queriables
{
    internal class RelinqQueryable<T> : QueryableBase<T>, IRelinqQueryable
    {
        public RelinqQueryable(IQueryProvider queryProvider, string sourceName)
            : base(queryProvider)
        {
            SourceName = sourceName;
        }

        public RelinqQueryable(IQueryProvider queryProvider, Expression expression)
            : base(queryProvider, expression)
        {
        }

        public string SourceName { get; private set; }
    }
}