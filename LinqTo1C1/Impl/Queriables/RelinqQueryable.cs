using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;

namespace LinqTo1C.Impl.Queriables
{
    public class RelinqQueryable<T> : QueryableBase<T>, IRelinqQueryable
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