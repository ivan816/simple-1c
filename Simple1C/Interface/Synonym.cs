using System;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Interface
{
    public static class Synonym
    {
        public static string OfEnum<T>(T enumValue)
            where T : struct
        {
            return EnumAttributesCache<SynonymAttribute>.instance.GetAttribute(enumValue).Value;
        }

        public static string OfEnumUnsafe(object enumValue)
        {
            return EnumAttributesCache<SynonymAttribute>.instance.GetAttributeUnsafe(enumValue).Value;
        }

        public static string OfClass(object obj)
        {
            return OfClass(obj.GetType());
        }

        public static string OfClass(Type objType)
        {
            return ClassAttributesCache<SynonymAttribute>.instance.GetAttribute(objType).Value;
        }
    }
}