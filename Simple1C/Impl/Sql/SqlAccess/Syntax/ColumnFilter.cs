using System;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnFilter : ISqlElement
    {
        public string ColumnTableName { get; set; }
        public string ColumnName { get; set; }
        public ColumnFilterType Type { get; set; }
        public string ComparandConstantValue { get; set; }
        public string[] ComparandConstantValues { get; set; }
        public string ComparandTableName { get; set; }
        public string ComparandColumnName { get; set; }

        public void WriteTo(StringBuilder builder)
        {
            SqlHelpers.WriteReference(builder, ColumnTableName, ColumnName);
            if (Type == ColumnFilterType.Eq)
            {
                builder.Append(" = ");
                if (ComparandConstantValue != null)
                    builder.Append(ComparandConstantValue);
                else
                    SqlHelpers.WriteReference(builder, ComparandTableName, ComparandColumnName);
            }
            else if (Type == ColumnFilterType.In)
            {
                builder.Append(" in ");
                builder.Append('(');
                builder.Append(string.Join(",", ComparandConstantValues));
                builder.Append(')');
            }
            else
                throw new InvalidOperationException(string.Format("unexpected type [{0}]", Type));
        }
    }
}