using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal static class SqlHelpers
    {
        public static void WriteFilters(StringBuilder builder, List<ColumnFilter> filters)
        {
            WriteElements(filters, " and ", builder);
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

        public static string QuoteSql(this string s)
        {
            return "'" + s + "'";
        }

        public static void WriteReference(StringBuilder builder, string objName, string itemName)
        {
            builder.Append(objName);
            builder.Append(".");
            builder.Append(itemName);
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