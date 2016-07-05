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
            foreach (var queryField in fields)
            {
                if (queryField.EvaluatedLocally)
                    continue;
                b.Append(queryField.Expression);
                if (queryField.Path != queryField.Alias)
                {
                    b.Append(" КАК ");
                    b.Append(queryField.Alias);
                }
                b.Append(',');
            }
            b.Length = b.Length - 1;
            return b.ToString();
        }

        public string GetCacheKey()
        {
            var b = new StringBuilder();
            b.Append(resultType.FormatName());
            b.Append('-');
            foreach (var t in fields)
            {
                b.Append(t.EvaluatedLocally ? t.Constant ?? "<null>" : t.Alias);
                b.Append('-');
            }
            return b.ToString();
        }
    }
}