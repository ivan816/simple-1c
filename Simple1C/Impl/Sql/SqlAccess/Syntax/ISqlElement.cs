using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal interface ISqlElement
    {
        void WriteTo(StringBuilder b);
    }
}