using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectColumn : ISqlElement
    {
        public string Name { get; set; }
        public string TableName { get; set; }
        public string Alias { get; set; }
        public string FunctionName { get; set; }

        public void WriteTo(StringBuilder b)
        {
            if (!string.IsNullOrEmpty(FunctionName))
            {
                b.Append(FunctionName);
                b.Append('(');
            }
            SqlHelpers.WriteReference(b, TableName, Name);
            if (!string.IsNullOrEmpty(FunctionName))
                b.Append(')');
            SqlHelpers.WriteAlias(b, Alias);
        }
    }
}