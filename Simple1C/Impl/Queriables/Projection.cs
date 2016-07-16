using System;
using System.Reflection;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class Projection
    {
        public QueryField[] fields;
        public SelectedProperty[] properties;
        public Type resultType;
        public ConstructorInfo ctor;
        public MemberInfo[] initMembers;

        public string GetSelection()
        {
            var b = new StringBuilder();
            foreach (var queryField in fields)
            {
                b.Append(queryField.Expression);
                b.Append(" КАК ");
                b.Append(queryField.Alias);
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
                b.Append(t.Alias);
                b.Append('-');
            }
            return b.ToString();
        }
    }
}