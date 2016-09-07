using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class UnaryFunctionExpression : ISqlElement
    {
        public string FunctionName { get; private set; }
        public ISqlElement Argument { get; private set; }

        public UnaryFunctionExpression(string functionName, ISqlElement argument)
        {
            FunctionName = functionName;
            Argument = argument;
        }

        public void WriteTo(StringBuilder b)
        {
            b.Append(FunctionName);
            b.Append('(');
            Argument.WriteTo(b);
            b.Append(')');
        }
    }
}