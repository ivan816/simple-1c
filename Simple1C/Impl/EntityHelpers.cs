using System;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal class EntityHelpers
    {
        public const string idPropertyName = "УникальныйИдентификатор";

        public static bool IsTableSection(Type type)
        {
            return type.Name.StartsWith("ТабличнаяЧасть");
        }

        public static bool IsConstant(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(Constant<>);
        }
    }
}