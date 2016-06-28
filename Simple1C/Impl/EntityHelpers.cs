using System;

namespace Simple1C.Impl
{
    internal class EntityHelpers
    {
        public static bool IsTableSection(Type type)
        {
            return type.Name.StartsWith("ТабличнаяЧасть");
        }
    }
}