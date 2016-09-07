using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal abstract class BinaryExpression : ISqlElement
    {
        private readonly string operand;
        public ISqlElement Left { get; set; }
        public ISqlElement Right { get; set; }

        protected BinaryExpression(string operand)
        {
            this.operand = operand;
        }

        public void WriteTo(StringBuilder b)
        {
            Left.WriteTo(b);
            b.Append(operand);
            Right.WriteTo(b);
        }
    }
}