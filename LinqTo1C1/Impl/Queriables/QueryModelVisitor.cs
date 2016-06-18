using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;

namespace LinqTo1C.Impl.Queriables
{
    public class QueryModelVisitor : QueryModelVisitorBase
    {
        private readonly QueryBuilder queryBuilder;

        public QueryModelVisitor(QueryBuilder queryBuilder)
        {
            this.queryBuilder = queryBuilder;
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            var xConstant = (ConstantExpression) fromClause.FromExpression;
            var relinqQueryable = (IRelinqQueryable) xConstant.Value;
            queryBuilder.SetSource(fromClause.ItemType, relinqQueryable.SourceName);
            base.VisitMainFromClause(fromClause, queryModel);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            WhereClauseFormatter.Apply(queryBuilder, whereClause.Predicate);
            base.VisitWhereClause(whereClause, queryModel, index);
        }

        protected override void VisitResultOperators(ObservableCollection<ResultOperatorBase> resultOperators,
            QueryModel queryModel)
        {
            foreach (var o in resultOperators)
            {
                var takeOperator = o as TakeResultOperator;
                if (takeOperator != null)
                    queryBuilder.Take = takeOperator.GetConstantCount();
            }
            base.VisitResultOperators(resultOperators, queryModel);
        }

        protected override void VisitBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel)
        {
            foreach (var o in bodyClauses)
            {
                var orderByClause = o as OrderByClause;
                if (orderByClause != null)
                    queryBuilder.Orderings = orderByClause.Orderings.ToArray();
            }
            base.VisitBodyClauses(bodyClauses, queryModel);
        }
    }
}