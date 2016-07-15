using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Simple1C.Impl.Queriables
{
    internal class MemberAccessBuilder : RelinqExpressionVisitor
    {
        private readonly Dictionary<IQuerySource, string> querySourceMapping =
            new Dictionary<IQuerySource, string>();

        private readonly List<string> members = new List<string>();
        private string sourceName;
        private bool isLocal;

        public QueryField GetFieldOrNull(Expression expression)
        {
            members.Clear();
            isLocal = false;
            Visit(expression);
            return isLocal ? null : new QueryField(sourceName, members, expression.Type);
        }

        public void Map(IQuerySource source, string value)
        {
            querySourceMapping[source] = value;
        }

        public override Expression Visit(Expression node)
        {
            if (isLocal)
                return node;
            var isMembersChain = node is MemberExpression ||
                                 node is QuerySourceReferenceExpression ||
                                 node.NodeType == ExpressionType.Convert;
            if (!isMembersChain)
            {
                isLocal = true;
                return node;
            }
            return base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Visit(node.Expression);
            members.Add(node.Member.Name);
            return node;
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression expression)
        {
            sourceName = querySourceMapping[expression.ReferencedQuerySource];
            return base.VisitQuerySourceReference(expression);
        }
    }
}