using System;
using System.Collections.Generic;

namespace Simple1C.Impl.Queriables
{
    internal class BuiltQuery
    {
        private BuiltQuery(Type entityType)
        {
            EntityType = entityType;
        }

        public BuiltQuery(Type entityType, string queryText,
            Dictionary<string, object> parameters, Projection projection)
        {
            EntityType = entityType;
            QueryText = queryText;
            Parameters = parameters;
            Projection = projection;
        }

        public static BuiltQuery Constant(Type entityType)
        {
            return new BuiltQuery(entityType) {IsQueryForConstant = true};
        }

        public Type EntityType { get; private set; }
        public string QueryText { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }
        public Projection Projection { get; private set; }
        public bool IsQueryForConstant { get; private set; }
    }
}