using System;

namespace Simple1C.Impl.Queriables
{
    internal class ConvertEnumCmd : IConvertParmeterCmd
    {
        public int valueIndex;
        public Type enumType;
    }
}