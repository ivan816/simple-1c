using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal static class SqlHelpers
    {
        public static void WriteEqConditions(StringBuilder builder, IEnumerable<EqCondition> eqConditions)
        {
            var isFirst = true;
            foreach (var eq in eqConditions)
            {
                if (isFirst)
                    isFirst = false;
                else
                    builder.Append(" and ");
                WriteReference(builder, eq.ColumnTableName, eq.ColumnName);
                builder.Append(" = ");
                if (eq.ComparandConstantValue != null)
                    builder.Append("'" + eq.ComparandConstantValue + "'");
                else
                    WriteReference(builder, eq.ComparandTableName, eq.ComparandColumnName);
            }
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