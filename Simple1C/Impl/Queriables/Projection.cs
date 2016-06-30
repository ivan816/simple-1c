using System;
using System.Reflection;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class Projection
    {
        public QueryField[] fields;
        public Type resultType;
        public ConstructorInfo ctor;
        public MemberInfo[] initMembers;

        public string GetSelection()
        {
            var b = new StringBuilder();
            for (var i = 0; i < fields.Length; i++)
            {
                var queryField = fields[i];
                b.Append(queryField.Expression);
                if (queryField.Path != queryField.Alias)
                {
                    b.Append(" КАК ");
                    b.Append(queryField.Alias);
                }
                if (i != fields.Length - 1)
                    b.Append(',');
            }
            return b.ToString();
        }

        public string GetCacheKey()
        {
            var b = new StringBuilder();
            b.Append(resultType.FormatName());
            b.Append('-');
            foreach (var t in fields)
            {
                b.Append(t.Alias);
                b.Append('-');
            }
            return b.ToString();
        }
    }
}