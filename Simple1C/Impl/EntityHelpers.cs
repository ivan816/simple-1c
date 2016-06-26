using System;

namespace Simple1C.Impl
{
    public class EntityHelpers
    {
        public static bool IsTableSection(Type type)
        {
            return type.Name.StartsWith("ТабличнаяЧасть");
        }
    }
}