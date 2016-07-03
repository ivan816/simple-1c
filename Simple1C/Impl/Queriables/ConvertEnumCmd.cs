using System;

namespace Simple1C.Impl.Queriables
{
    internal class ConvertEnumCmd : IConvertParameterCmd
    {
        public int valueIndex;
        public Type enumType;
    }
}