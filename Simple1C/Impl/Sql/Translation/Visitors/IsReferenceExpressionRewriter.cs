using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class IsReferenceExpressionRewriter : SqlVisitor
    {
        private readonly QueryEntityTree queryEntityTree;
        private readonly NameGenerator nameGenerator;
        private readonly HashSet<ColumnReferenceExpression> rewritten;

        public IsReferenceExpressionRewriter(QueryEntityTree queryEntityTree,
            NameGenerator nameGenerator, HashSet<ColumnReferenceExpression> rewritten)
        {
            this.queryEntityTree = queryEntityTree;
            this.nameGenerator = nameGenerator;
            this.rewritten = rewritten;
        }

        public override ISqlElement VisitIsReference(IsReferenceExpression expression)
        {
            var queryRoot = queryEntityTree.Get(expression.Argument.Table);
            var propertyNames = expression.Argument.Name.Split('.');
            var referencedProperties = queryEntityTree.GetProperties(queryRoot, propertyNames);
            if (referencedProperties.Count != 1)
            {
                const string messageFormat = "operator IsReference property [{0}] has many " +
                                             "variants which is not supported currently";
                throw new InvalidOperationException(string.Format(messageFormat,
                    expression.Argument.Name));
            }
            var property = referencedProperties[0];
            var entity = property.nestedEntities.SingleOrDefault(x => x.mapping.QueryTableName.EqualsIgnoringCase(expression.ObjectName));
            if (entity == null)
            {
                const string messageFormat = "can't find entity [{0}] for property [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    expression.ObjectName, expression.Argument.Name));
            }
            var unionCondition = queryEntityTree.GetUnionCondition(property, entity);
            if (unionCondition == null)
            {
                const string messageFormat = "property [{0}] has only one possible type";
                throw new InvalidOperationException(string.Format(messageFormat,
                    expression.Argument.Name));
            }
            if (queryRoot.additionalFields == null)
                queryRoot.additionalFields = new List<SelectFieldExpression>();
            var filterColumnName = nameGenerator.GenerateColumnName();
            queryRoot.additionalFields.Add(new SelectFieldExpression
            {
                Expression = unionCondition,
                Alias = filterColumnName
            });
            var result = new ColumnReferenceExpression
            {
                Name = filterColumnName,
                Table = expression.Argument.Table
            };
            rewritten.Add(result);
            return result;
        }
    }
}