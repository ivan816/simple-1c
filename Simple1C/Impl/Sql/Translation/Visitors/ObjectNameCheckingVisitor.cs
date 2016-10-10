using System;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class ObjectNameCheckingVisitor : SqlVisitor
    {
        private readonly IMappingSource mappingSource;

        public ObjectNameCheckingVisitor(IMappingSource mappingSource)
        {
            this.mappingSource = mappingSource;
        }

        public override ISqlElement VisitIsReference(IsReferenceExpression expression)
        {
            var result = base.VisitIsReference(expression);
            var tableMapping = mappingSource.ResolveTableOrNull(expression.ObjectName);
            if (tableMapping == null)
            {
                const string messageFormat = "operator [Ссылка] has unknown object name [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, expression.ObjectName));
            }
            return result;
        }
    }
}