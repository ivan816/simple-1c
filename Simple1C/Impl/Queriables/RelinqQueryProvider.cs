using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace Simple1C.Impl.Queriables
{
    internal class RelinqQueryProvider : QueryProviderBase
    {
        public RelinqQueryProvider(IQueryParser queryParser, IQueryExecutor executor)
            : base(queryParser, executor)
        {
        }

        public override IQueryable<T> CreateQuery<T>(Expression expression)
        {
            return new RelinqQueryable<T>(this, expression);
        }
    }
}