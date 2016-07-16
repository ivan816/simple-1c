using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Simple1C.Impl.Queriables
{
    internal class PropertiesExtractingVisitor : RelinqExpressionVisitor
    {
        private readonly MemberAccessBuilder memberAccessBuilder;
        public List<QueryField> fields = new List<QueryField>();
        public List<SelectedPropertyItem> items = new List<SelectedPropertyItem>();

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
            items.Clear();
            Visit(expression);
            return new SelectedProperty
            {
                expression = expression,
                items = items.ToArray()
            };
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            items.Add(new SelectedPropertyItem(node.Value, -1));
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var queryField = memberAccessBuilder.GetFieldOrNull(node);
            if (queryField == null)
                return base.VisitMember(node);
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