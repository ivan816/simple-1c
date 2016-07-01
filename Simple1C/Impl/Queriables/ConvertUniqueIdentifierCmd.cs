using System;

namespace Simple1C.Impl.Queriables
{
    internal class ConvertUniqueIdentifierCmd : IConvertParmeterCmd
    {
        public Guid id;
        public Type entityType;
    }
}