using System;
using System.Collections.ObjectModel;
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
        private readonly MemberAccessBuilder memberAccessBuilder = new MemberAccessBuilder();
        private readonly FilterPredicateAnalyzer filterPredicateAnalyer;
        private readonly PropertiesExtractingVisitor propertiesExtractor;

        public QueryModelVisitor(QueryBuilder queryBuilder)
        {
            this.queryBuilder = queryBuilder;
            filterPredicateAnalyer = new FilterPredicateAnalyzer(queryBuilder, memberAccessBuilder);
            propertiesExtractor = new PropertiesExtractingVisitor(memberAccessBuilder);
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            base.VisitSelectClause(selectClause, queryModel);
            var xSelector = selectClause.Selector;
            if (xSelector is QuerySourceReferenceExpression)
                return;
            var xMemberInit = xSelector as MemberInitExpression;
            MemberInfo[] members;
            SelectedProperty[] properties;
            NewExpression xNew;
            if (xMemberInit != null)
            {
                members = new MemberInfo[xMemberInit.Bindings.Count];
                properties = new SelectedProperty[xMemberInit.Bindings.Count];
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
                    properties[i] = propertiesExtractor.GetProperty(memberAssignment.Expression);
                    members[i] = binding.Member;
                }
                xNew = xMemberInit.NewExpression;
            }
            else
            {
                xNew = xSelector as NewExpression;
                members = null;
                if (xNew != null)
                {
                    properties = new SelectedProperty[xNew.Arguments.Count];
                    for (var i = 0; i < properties.Length; i++)
                        properties[i] = propertiesExtractor.GetProperty(xNew.Arguments[i]);
                }
                else
                    properties = new[] {propertiesExtractor.GetProperty(xSelector)};
            }
            queryBuilder.SetProjection(new Projection
            {
                fields = propertiesExtractor.GetFields(),
                properties = properties,
                resultType = xNew == null ? null : xNew.Type,
                ctor = xNew == null ? null : xNew.Constructor,
                initMembers = members
            });
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            var xConstant = (ConstantExpression) fromClause.FromExpression;
            var relinqQueryable = (IRelinqQueryable) xConstant.Value;
            queryBuilder.SetSource(fromClause.ItemType, relinqQueryable.SourceName);
            AdditionalFromClause additionalFromClause = null;
            foreach (var c in queryModel.BodyClauses)
            {
                additionalFromClause = c as AdditionalFromClause;
                if (additionalFromClause != null)
                    break;
            }
            if (additionalFromClause != null)
            {
                memberAccessBuilder.Map(fromClause, "src.Ссылка");
                var xSubquery = (SubQueryExpression) additionalFromClause.FromExpression;
                memberAccessBuilder.Map(xSubquery.QueryModel.MainFromClause, "src");
            }
            else
                memberAccessBuilder.Map(fromClause, "src");
            base.VisitMainFromClause(fromClause, queryModel);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            base.VisitWhereClause(whereClause, queryModel, index);
            filterPredicateAnalyer.Apply(whereClause.Predicate);
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
                var countOperator = o as CountResultOperator;
                if (countOperator != null)
                    queryBuilder.Count = true;
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
                var fromField = memberAccessBuilder.GetFieldOrNull(mainFromClause.FromExpression);
                if (fromField == null || fromField.PathItems.Length != 1)
                {
                    const string messageFormat = "unexpected members [{0}], expression [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        fromField == null ? "<empty>" : fromField.PathItems.JoinStrings(","),
                        mainFromClause.FromExpression));
                }
                queryBuilder.TableSectionName = fromField.PathItems[0];
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
                    var orderings = new Ordering[orderByClause.Orderings.Count];
                    for (var i = 0; i < orderings.Length; i++)
                    {
                        var ordering = orderByClause.Orderings[i];
                        var field = memberAccessBuilder.GetFieldOrNull(ordering.Expression);
                        if (field == null)
                        {
                            const string messageFormat = "can't apply [{0}] operator by expression [{1}]." +
                                                         "Expression must be a chain of member accesses.";
                            throw new InvalidOperationException(string.Format(messageFormat,
                                ordering.OrderingDirection == OrderingDirection.Asc ? "OrderBy" : "OrderByDescending",
                                ordering.Expression));
                        }
                        orderings[i] = new Ordering
                        {
                            Field = field,
                            IsAsc = ordering.OrderingDirection == OrderingDirection.Asc
                        };
                    }
                    queryBuilder.Orderings = orderings;
                }
            }
            base.VisitBodyClauses(bodyClauses, queryModel);
        }
    }
}