using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal static class SqlHelpers
    {
        public static ISqlElement Combine(this List<ISqlElement> items)
        {
            return items.Aggregate((left, right) => new AndExpression
            {
                Left = left,
                Right = right
            });
        }

        public static void WriteElements<T>(List<T> elements, string delimiter, StringBuilder builder)
            where T: ISqlElement
        {
            var isFirst = true;
            foreach (var e in elements)
            {
                if (isFirst)
                    isFirst = false;
                else
                    builder.Append(delimiter);
                e.WriteTo(builder);
            }
        }

        public static void WriteAlias(StringBuilder b, string alias)
        {
            if (!string.IsNullOrEmpty(alias))
            {
                b.Append(" as ");
                b.Append(alias);
            }
        }

        public static void WriteDeclaration(StringBuilder b, string objName, string alias)
        {
            b.Append(objName);
            WriteAlias(b, alias);
        }
    }
}