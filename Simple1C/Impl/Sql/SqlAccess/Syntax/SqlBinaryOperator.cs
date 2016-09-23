namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal enum SqlBinaryOperator
    {
        [OperatorSynonyms("*")] [OperatorPrecedence(10)] Mult,
        [OperatorSynonyms("/")] [OperatorPrecedence(10)] Div,
        [OperatorSynonyms("%")] [OperatorPrecedence(10)] Remainder,
        [OperatorSynonyms("+")] [OperatorPrecedence(9)] Plus,
        [OperatorSynonyms("-")] [OperatorPrecedence(9)] Minus,
        [OperatorSynonyms("=")] [OperatorPrecedence(8)] Eq,
        [OperatorSynonyms("<>", "!=")] [OperatorPrecedence(8)] Neq,
        [OperatorSynonyms("<")] [OperatorPrecedence(8)] LessThan,
        [OperatorSynonyms("<=")] [OperatorPrecedence(8)] LessThanOrEqual,
        [OperatorSynonyms(">")] [OperatorPrecedence(8)] GreaterThan,
        [OperatorSynonyms(">=")] [OperatorPrecedence(8)] GreaterThanOrEqual,
        [OperatorSynonyms("LIKE", "ПОДОБНО")] [OperatorPrecedence(8)] Like,
        [OperatorSynonyms("И", "AND")] [OperatorPrecedence(5)] And,
        [OperatorSynonyms("ИЛИ", "OR")] [OperatorPrecedence(4)] Or
    }
}