using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using Simple1C.Interface;

namespace Simple1C.Impl.Queriables
{
    internal class PropertiesExtractingVisitor : RelinqExpressionVisitor
    {
        private readonly MemberAccessBuilder memberAccessBuilder;
        private readonly List<QueryField> fields = new List<QueryField>();
        private readonly List<SelectedPropertyItem> items = new List<SelectedPropertyItem>();
        private Expression xRoot;
        private bool rootIsSingleItem;

        private static readonly MethodInfo presentationMethodInfo = typeof(Функции)
            .GetMethod("Представление", BindingFlags.Static | BindingFlags.Public);

        public PropertiesExtractingVisitor(MemberAccessBuilder memberAccessBuilder)
        {
            this.memberAccessBuilder = memberAccessBuilder;
        }

        public QueryField[] GetFields()
        {
            var result = fields.ToArray();
            fields.Clear();
            return result;
        }

        public SelectedProperty GetProperty(Expression expression)
        {
            xRoot = expression;
            rootIsSingleItem = false;
            items.Clear();
            Visit(xRoot);
            return new SelectedProperty
            {
                expression = expression,
                needLocalEval = !rootIsSingleItem,
                items = items.ToArray(),
            };
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression node)
        {
            return VisitMember(node, base.VisitQuerySourceReference);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            rootIsSingleItem = ReferenceEquals(node, xRoot);
            items.Add(new SelectedPropertyItem(node.Value, -1));
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == presentationMethodInfo)
                return VisitMember(node, m => Visit(node.Arguments[0]));
            if (node.Object != null && node.Method.Name == "GetType" && node.Arguments.Count == 0 &&
                node.Type == typeof(Type))
                return VisitMember(node, m => Visit(node.Object));
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return VisitMember(node, base.VisitMember);
        }

        private Expression VisitMember<T>(T node, Func<T, Expression> baseCaller) where T:Expression
        {
            var queryField = memberAccessBuilder.GetFieldOrNull(node);
            if (queryField == null)
                return baseCaller(node);
            rootIsSingleItem = ReferenceEquals(node, xRoot);
            var fieldIndex = -1;
            for (var i = 0; i < fields.Count; i++)
                if (fields[i].Alias == queryField.Alias)
                {
                    fieldIndex = i;
                    break;
                }
            if (fieldIndex < 0)
            {
                fields.Add(queryField);
                fieldIndex = fields.Count - 1;
            }
            items.Add(new SelectedPropertyItem(null, fieldIndex));
            return node;
        }
    }
}