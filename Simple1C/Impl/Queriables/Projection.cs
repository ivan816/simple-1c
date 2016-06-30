using System;
using System.Reflection;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class Projection
    {
        public string[] sourceFieldNames;
        public string[] aliasFieldNames;
        public Type resultType;
        public ConstructorInfo ctor;
        public MemberInfo[] initMembers;

        public string GetSelection()
        {
            var b = new StringBuilder();
            for (var i = 0; i < sourceFieldNames.Length; i++)
            {
                b.Append("src.");
                b.Append(sourceFieldNames[i]);
                if (sourceFieldNames[i] != aliasFieldNames[i])
                {
                    b.Append(" КАК ");
                    b.Append(aliasFieldNames[i]);
                }
                if (i != sourceFieldNames.Length - 1)
                    b.Append(',');
            }
            return b.ToString();
        }

        public string GetCacheKey()
        {
            var b = new StringBuilder();
            b.Append(resultType.FormatName());
            b.Append('-');
            foreach (var t in aliasFieldNames)
            {
                b.Append(t);
                b.Append('-');
            }
            return b.ToString();
        }
    }
}