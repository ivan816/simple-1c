using System;
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
            string[][] fields;
            NewExpression xNew;
            var memberAccessBuilder = new MemberAccessBuilder();
            if (xMemberInit != null)
            {
                members = new MemberInfo[xMemberInit.Bindings.Count];
                fields = new string[xMemberInit.Bindings.Count][];
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
                fields = new string[xNew.Arguments.Count][];
                members = null;
                for (var i = 0; i < fields.Length; i++)
                    fields[i] = memberAccessBuilder.GetMembers(xNew.Arguments[i]);
            }
            queryBuilder.SetProjection(new Projection
            {
                sourceFieldNames = fields.Select(x => x.JoinStrings(".")).ToArray(),
                aliasFieldNames = fields.Select(x => x.JoinStrings("_")).ToArray(),
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
                var firstOperator = o as FirstResultOperator;
                if (firstOperator != null)
                    queryBuilder.Take = 1;
                var singleOperator = o as SingleResultOperator;
                if (singleOperator != null)
                    queryBuilder.Take = 2;
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