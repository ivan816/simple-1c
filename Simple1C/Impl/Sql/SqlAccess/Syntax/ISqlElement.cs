using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal interface ISqlElement
    {
        ISqlElement Accept(SqlVisitor visitor);
    }
}