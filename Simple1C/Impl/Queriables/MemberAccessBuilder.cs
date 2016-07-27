using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using Simple1C.Interface;

namespace Simple1C.Impl.Queriables
{
    internal class MemberAccessBuilder : RelinqExpressionVisitor
    {
        private readonly Dictionary<IQuerySource, string> querySourceMapping =
            new Dictionary<IQuerySource, string>();

        private readonly List<string> members = new List<string>();
        private string sourceName;
        private bool isLocal;

        private static readonly MethodInfo presentationMethodInfo = typeof(Функции).GetMethod("Представление",
            BindingFlags.Static | BindingFlags.Public);

        public QueryField GetFieldOrNull(Expression expression)
        {
            members.Clear();
            isLocal = false;
            var type = expression.Type;
            var xMethodCall = expression as MethodCallExpression;
            var isPresentation = xMethodCall != null && xMethodCall.Method == presentationMethodInfo;
            if (isPresentation)
                expression = xMethodCall.Arguments[0];
            xMethodCall = expression as MethodCallExpression;
            var isGetType = xMethodCall != null && xMethodCall.Object != null
                            && xMethodCall.Method.Name == "GetType"
                            && xMethodCall.Arguments.Count == 0
                            && xMethodCall.Type == typeof(Type);
            if (isGetType)
                expression = xMethodCall.Object;
            var xQuerySourceReference =  expression as QuerySourceReferenceExpression;
            if (xQuerySourceReference != null)
                return new QueryField(querySourceMapping[xQuerySourceReference.ReferencedQuerySource],
                    new List<string> {"Ссылка"}, isPresentation, isGetType, type);
            Visit(expression);
            return isLocal ? null : new QueryField(sourceName, members, isPresentation, isGetType, type);
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
            if (isMembersChain)
                return base.Visit(node);

            isLocal = true;
            return node;
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