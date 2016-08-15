using System;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Interface
{
    public static class ObjectPresentation
    {
        public static string OfClass(object obj)
        {
            return OfClass(obj.GetType());
        }

        public static string OfClass(Type objectType)
        {
            var attribute = AttributesCache.GetCustomAttribute<ObjectPresentationAttribute>(objectType, true);
            return attribute.Value;
        }
    }
}