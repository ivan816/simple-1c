using System.Text;

namespace Simple1C.Impl.Sql.SqlBuilders
{
    public static class SqlHelpers
    {
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