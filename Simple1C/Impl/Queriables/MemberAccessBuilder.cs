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

        public QueryField GetMembers(Expression expression)
        {
            members.Clear();
            Visit(expression);
            return new QueryField(sourceName, members);
        }

        public void Map(IQuerySource source, string value)
        {
            querySourceMapping[source] = value;
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