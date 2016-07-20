using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Interface
{
    public static class Synonym
    {
        public static string Of<T>(T enumValue)
            where T : struct
        {
            return EnumAttributesCache<SynonymAttribute>.instance.GetAttribute(enumValue).Value;
        }
    }
}