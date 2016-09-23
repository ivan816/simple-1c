using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class PropertiesEnumerator
    {
        private readonly string[] propertyNames;
        private readonly QueryRoot queryRoot;
        private readonly QueryEntityAccessor queryEntityAccessor;
        private readonly List<QueryEntityProperty> properties = new List<QueryEntityProperty>();

        public PropertiesEnumerator(string[] propertyNames, QueryRoot queryRoot, QueryEntityAccessor queryEntityAccessor)
        {
            this.propertyNames = propertyNames;
            this.queryRoot = queryRoot;
            this.queryEntityAccessor = queryEntityAccessor;
        }

        public List<QueryEntityProperty> Enumerate()
        {
            Iterate(0, queryRoot.entity);
            if (properties.Count == 0)
            {
                var tableDeclaration = queryRoot.tableDeclaration as TableDeclarationClause;
                var tableDescription = tableDeclaration != null ? tableDeclaration.GetRefName() : "(subquery)";
                var message = string.Format("no properties found for [{0}.{1}]", tableDescription, propertyNames.JoinStrings("."));
                throw new InvalidOperationException(message);
            }
            return properties;
        }

        private void Iterate(int index, QueryEntity entity)
        {
            var property = queryEntityAccessor.GetOrCreatePropertyIfExists(entity, propertyNames[index]);
            if (property == null)
                return;
            if (index == propertyNames.Length - 1)
                properties.Add(property);
            else if (property.mapping.UnionLayout != null)
            {
                var count = properties.Count;
                foreach (var p in property.nestedEntities)
                    Iterate(index + 1, p);
                if (properties.Count == count)
                {
                    var tableDeclaration = queryRoot.tableDeclaration as TableDeclarationClause;
                    var tableDescription = tableDeclaration != null ? tableDeclaration.Alias : "(subqyery)";
                    var message = string.Format("property [{0}] in [{1}.{2}] has multiple types [{3}] " +
                                                "and none of them has property [{4}]",
                        propertyNames[index], tableDescription, propertyNames.JoinStrings("."),
                        property.nestedEntities.Select(x => x.mapping.QueryTableName).JoinStrings(","),
                        propertyNames[index + 1]);
                    throw new InvalidOperationException(message);
                }
            }
            else if (property.nestedEntities.Count == 1)
                Iterate(index + 1, property.nestedEntities[0]);
            else
            {
                const string messageFormat = "property [{0}] has no table mapping, property path [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    property.mapping.PropertyName, propertyNames.JoinStrings(".")));
            }
        }
    }
}