using Simple1C.Impl.Generation;

namespace Simple1C.Impl
{
    internal struct TypeInfo
    {
        public SimpleTypeInfo? simpleType;
        public ConfigurationItem configurationItem;

        public static TypeInfo Simple(string typeName, int? maxLength = null)
        {
            return new TypeInfo
            {
                simpleType = new SimpleTypeInfo
                {
                    typeName = typeName,
                    maxLength = maxLength
                }
            };
        }
    }
}