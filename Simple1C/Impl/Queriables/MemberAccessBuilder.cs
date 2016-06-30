using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Simple1C.Impl.Queriables
{
    internal class MemberAccessBuilder : RelinqExpressionVisitor
    {
        private readonly List<string> members = new List<string>();

        public string[] GetMembers(Expression expression)
        {
            members.Clear();
            Visit(expression);
            return members.ToArray();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Visit(node.Expression);
            members.Add(node.Member.Name);
            return node;
        }
    }
}