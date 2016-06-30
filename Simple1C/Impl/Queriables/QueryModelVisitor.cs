using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class QueryModelVisitor : QueryModelVisitorBase
    {
        private readonly QueryBuilder queryBuilder;

        private readonly Dictionary<IQuerySource, string> querySourceMapping =
            new Dictionary<IQuerySource, string>(); 

        public QueryModelVisitor(QueryBuilder queryBuilder)
        {
            this.queryBuilder = queryBuilder;
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            base.VisitSelectClause(selectClause, queryModel);
            var xSelector = selectClause.Selector;
            if (xSelector is QuerySourceReferenceExpression)
                return;
            var xMemberInit = xSelector as MemberInitExpression;
            MemberInfo[] members;
            QueryField[] fields;
            NewExpression xNew;
            var memberAccessBuilder = new MemberAccessBuilder(querySourceMapping);
            if (xMemberInit != null)
            {
                members = new MemberInfo[xMemberInit.Bindings.Count];
                fields = new QueryField[xMemberInit.Bindings.Count];
                for (var i = 0; i < xMemberInit.Bindings.Count; i++)
                {
                    var binding = xMemberInit.Bindings[i];
                    if (binding.BindingType != MemberBindingType.Assignment)
                    {
                        const string messageFormat = "unexpected binding type [{0}] for member [{1}], selector [{2}]";
                        throw new InvalidOperationException(string.Format(messageFormat, binding.BindingType,
                            binding.Member.Name, xSelector));
                    }
                    var memberAssignment = (MemberAssignment) binding;
                    fields[i] = memberAccessBuilder.GetMembers(memberAssignment.Expression);
                    members[i] = binding.Member;
                }
                xNew = xMemberInit.NewExpression;
            }
            else
            {
                xNew = xSelector as NewExpression;
                if (xNew == null)
                {
                    const string messageFormat = "selector [{0}] is not supported";
                    throw new InvalidOperationException(string.Format(messageFormat, selectClause.Selector));
                }
                fields = new QueryField[xNew.Arguments.Count];
                members = null;
                for (var i = 0; i < fields.Length; i++)
                    fields[i] = memberAccessBuilder.GetMembers(xNew.Arguments[i]);
            }
            queryBuilder.SetProjection(new Projection
            {
                fields = fields,
                resultType = xNew.Type,
                ctor = xNew.Constructor,
                initMembers = members
            });
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            var xConstant = (ConstantExpression) fromClause.FromExpression;
            var relinqQueryable = (IRelinqQueryable) xConstant.Value;
            queryBuilder.SetSource(fromClause.ItemType, relinqQueryable.SourceName);

            var additionalFromClause = queryModel.BodyClauses
                .OfType<AdditionalFromClause>()
                .FirstOrDefault();
            if (additionalFromClause != null)
            {
                querySourceMapping[fromClause] = "src.Ссылка";
                var xSubquery = (SubQueryExpression) additionalFromClause.FromExpression;
                querySourceMapping[xSubquery.QueryModel.MainFromClause] = "src";
            }
            else
                querySourceMapping[fromClause] = "src";
            base.VisitMainFromClause(fromClause, queryModel);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            WhereClauseFormatter.Apply(queryBuilder, whereClause.Predicate, querySourceMapping);
            base.VisitWhereClause(whereClause, queryModel, index);
        }

        protected override void VisitResultOperators(ObservableCollection<ResultOperatorBase> resultOperators,
            QueryModel queryModel)
        {
            foreach (var o in resultOperators)
            {
                var takeOperator = o as TakeResultOperator;
                if (takeOperator != null)
                {
                    queryBuilder.Take = takeOperator.GetConstantCount();
                    continue;
                }
                var firstOperator = o as FirstResultOperator;
                if (firstOperator != null)
                {
                    queryBuilder.Take = 1;
                    continue;
                }
                var singleOperator = o as SingleResultOperator;
                if (singleOperator != null)
                    queryBuilder.Take = 2;
            }
            base.VisitResultOperators(resultOperators, queryModel);
        }

        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index)
        {
            base.VisitAdditionalFromClause(fromClause, queryModel, index);
            var xSubquery = fromClause.FromExpression as SubQueryExpression;
            if (xSubquery != null)
            {
                var subQueryModel = xSubquery.QueryModel;
                var mainFromClause = subQueryModel.MainFromClause;
                var memberAccessBuilder = new MemberAccessBuilder(querySourceMapping);
                var fromMembers = memberAccessBuilder.GetMembers(mainFromClause.FromExpression);
                if (fromMembers.PathItems.Length != 1)
                {
                    const string messageFormat = "unexpected members [{0}], expression [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        fromMembers.PathItems.JoinStrings(","), mainFromClause.FromExpression));
                }
                queryBuilder.TableSectionName = fromMembers.PathItems[0];
                VisitSelectClause(xSubquery.QueryModel.SelectClause, xSubquery.QueryModel);
            }
        }

        protected override void VisitBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel)
        {
            foreach (var o in bodyClauses)
            {
                var orderByClause = o as OrderByClause;
                if (orderByClause != null)
                {
                    var memberAccessBuilder = new MemberAccessBuilder(querySourceMapping);
                    queryBuilder.Orderings = orderByClause
                        .Orderings.Select(x => new Ordering
                        {
                            Field = memberAccessBuilder.GetMembers(x.Expression),
                            IsAsc = x.OrderingDirection == OrderingDirection.Asc
                        })
                        .ToArray();
                }
            }
            base.VisitBodyClauses(bodyClauses, queryModel);
        }
    }
}