using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal static class SqlHelpers
    {
        public static ISqlElement Combine(this List<ISqlElement> items)
        {
            return items.Aggregate((left, right) => new AndExpression
            {
                Left = left,
                Right = right
            });
        }
    }
}