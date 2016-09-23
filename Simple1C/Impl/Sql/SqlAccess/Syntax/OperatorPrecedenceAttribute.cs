using System;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    public class OperatorPrecedenceAttribute : Attribute
    {
        public OperatorPrecedenceAttribute(int precedence)
        {
            Precedence = precedence;
        }

        public int Precedence { get; private set; }
    }
}