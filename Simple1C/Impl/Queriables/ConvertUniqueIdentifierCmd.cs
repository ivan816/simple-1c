using System;

namespace Simple1C.Impl.Queriables
{
    internal class ConvertUniqueIdentifierCmd : IConvertParameterCmd
    {
        public Guid id;
        public Type entityType;
    }
}