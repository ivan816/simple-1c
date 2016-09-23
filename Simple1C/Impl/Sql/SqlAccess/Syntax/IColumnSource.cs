namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal interface IColumnSource : ISqlElement
    {
        string Alias { get; }
    }
}