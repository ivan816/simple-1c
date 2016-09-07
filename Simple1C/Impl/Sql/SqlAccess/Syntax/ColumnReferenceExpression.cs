using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferenceExpression : ISqlElement
    {
        public string Name { get; set; }
        public string TableName { get; set; }

        public void WriteTo(StringBuilder b)
        {
            b.Append(TableName);
            b.Append(".");
            b.Append(Name);
        }
    }
}