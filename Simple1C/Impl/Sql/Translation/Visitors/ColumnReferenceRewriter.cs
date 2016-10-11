using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class ColumnReferenceRewriter : SqlVisitor
    {
        private readonly QueryEntityTree queryEntityTree;
        private bool isPresentation;
        private SelectPart? currentPart;
        private readonly HashSet<ColumnReferenceExpression> rewritten;
        private readonly NameGenerator nameGenerator;

        public ColumnReferenceRewriter(QueryEntityTree queryEntityTree,
            HashSet<ColumnReferenceExpression> rewritten,
            NameGenerator nameGenerator)
        {
            this.queryEntityTree = queryEntityTree;
            this.rewritten = rewritten;
            this.nameGenerator = nameGenerator;
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            if (rewritten.Contains(expression))
                return expression;
            rewritten.Add(expression);
            if (!currentPart.HasValue)
                throw new InvalidOperationException("assertion failure");
            var queryField = GetOrCreateQueryField(expression,
                isPresentation, currentPart.Value);
            expression.Name = queryField.alias ?? queryField.properties[0].GetDbColumnName();
            return expression;
        }

        public override SelectFieldExpression VisitSelectField(SelectFieldExpression clause)
        {
            WithCurrentPart(SelectPart.Select, () => base.VisitSelectField(clause));
            return clause;
        }

        public override ISqlElement VisitWhere(ISqlElement element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitWhere(element));
            return element;
        }

        public override GroupByClause VisitGroupBy(GroupByClause element)
        {
            WithCurrentPart(SelectPart.GroupBy, () => base.VisitGroupBy(element));
            return element;
        }

        public override JoinClause VisitJoin(JoinClause element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitJoin(element));
            return element;
        }

        public override OrderByClause VisitOrderBy(OrderByClause element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitOrderBy(element));
            return element;
        }

        public override ISqlElement VisitHaving(ISqlElement element)
        {
            WithCurrentPart(SelectPart.Other, () => base.VisitHaving(element));
            return element;
        }

        public override ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            isPresentation = expression.KnownFunction == KnownQueryFunction.Presentation;
            base.VisitQueryFunction(expression);
            isPresentation = false;
            return expression;
        }

        private QueryField GetOrCreateQueryField(ColumnReferenceExpression columnReference,
            bool isRepresentation, SelectPart selectPart)
        {
            var queryRoot = queryEntityTree.Get(columnReference.Table);
            if (!isRepresentation && selectPart == SelectPart.GroupBy)
            {
                QueryField fieldWithFunction;
                var keyWithFunction = columnReference.Name + "." + true;
                if (queryRoot.fields.TryGetValue(keyWithFunction, out fieldWithFunction))
                    if (fieldWithFunction.parts.Contains(SelectPart.Select))
                        isRepresentation = true;
            }
            var key = columnReference.Name + "." + isRepresentation;
            QueryField field;
            if (!queryRoot.fields.TryGetValue(key, out field))
            {
                var propertyNames = columnReference.Name.Split('.');
                var subqueryRequired = propertyNames.Length > 1;
                var needInvert = false;
                if (propertyNames[propertyNames.Length - 1].EqualsIgnoringCase("ЭтоГруппа"))
                {
                    needInvert = true;
                    subqueryRequired = true;
                }
                var referencedProperties = queryEntityTree.GetProperties(queryRoot, propertyNames);
                if (isRepresentation)
                    if (ReplaceWithRepresentation(referencedProperties))
                        subqueryRequired = true;
                string fieldAlias = null;
                if (subqueryRequired)
                {
                    queryRoot.subqueryRequired = true;
                    fieldAlias = nameGenerator.GenerateColumnName();
                }
                foreach (var p in referencedProperties)
                    p.referenced = true;
                field = new QueryField(fieldAlias, referencedProperties.ToArray(), needInvert);
                queryRoot.fields.Add(key, field);
            }
            if (!field.parts.Contains(selectPart))
                field.parts.Add(selectPart);
            return field;
        }

        private bool ReplaceWithRepresentation(List<QueryEntityProperty> properties)
        {
            var result = false;
            for (var i = properties.Count - 1; i >= 0; i--)
            {
                var property = properties[i];
                if (property.nestedEntities.Count == 0)
                    continue;
                properties.RemoveAt(i);
                foreach (var nestedEntity in property.nestedEntities)
                {
                    var scope = nestedEntity.mapping.ObjectName.HasValue
                        ? nestedEntity.mapping.ObjectName.Value.Scope
                        : (ConfigurationScope?) null;
                    var validScopes = new ConfigurationScope?[]
                    {
                        ConfigurationScope.Перечисления, ConfigurationScope.Справочники
                    };
                    if (!validScopes.Contains(scope))
                    {
                        const string messageFormat = "[ПРЕДСТАВЛЕНИЕ] is only supported for [{0}]";
                        throw new InvalidOperationException(string.Format(messageFormat, validScopes.JoinStrings(",")));
                    }
                    var propertyName = scope == ConfigurationScope.Справочники ? "Наименование" : "Порядок";
                    var presentationProperty = queryEntityTree.GetOrCreatePropertyIfExists(nestedEntity, propertyName);
                    if (presentationProperty == null)
                    {
                        const string messageFormat = "entity [{0}] has no property [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            nestedEntity.mapping.QueryTableName, propertyName));
                    }
                    properties.Add(presentationProperty);
                    result = true;
                }
            }
            return result;
        }

        private void WithCurrentPart(SelectPart part, Action handle)
        {
            var oldPart = currentPart;
            currentPart = part;
            handle();
            currentPart = oldPart;
        }
    }
}