using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class DeduceEntityTypeFromIsReferenceExpressionVisitor : SqlVisitor
    {
        private readonly QueryEntityRegistry queryEntityRegistry;
        private readonly QueryEntityTree queryEntityTree;

        public DeduceEntityTypeFromIsReferenceExpressionVisitor(QueryEntityRegistry queryEntityRegistry,
            QueryEntityTree queryEntityTree)
        {
            this.queryEntityRegistry = queryEntityRegistry;
            this.queryEntityTree = queryEntityTree;
        }

        public override SelectClause VisitSelect(SelectClause clause)
        {
            var result = base.VisitSelect(clause);
            VisitCondition(result.WhereExpression);
            return result;
        }

        private void VisitCondition(ISqlElement sqlElement)
        {
            if (sqlElement == null)
                return;
            var isReference = sqlElement as IsReferenceExpression;
            if (isReference != null)
            {
                SetPropertyType(isReference.Argument, isReference.ObjectName);
                return;
            }
            var andExpression = sqlElement as AndExpression;
            if (andExpression != null)
            {
                VisitCondition(andExpression.Left);
                VisitCondition(andExpression.Right);
            }
        }

        private void SetPropertyType(ColumnReferenceExpression columnReference, string name)
        {
            var queryRoot = queryEntityRegistry.Get(columnReference.Table);
            var propertyNames = columnReference.Name.Split('.');
            var referencedProperties = queryEntityTree.GetProperties(propertyNames, queryRoot);
            foreach (var property in referencedProperties)
                property.nestedEntities.RemoveAll(entity => entity.mapping.QueryTableName != name);
        }
    }
}