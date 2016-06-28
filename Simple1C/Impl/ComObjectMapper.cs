using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Simple1C.Impl.Com;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal class ComObjectMapper
    {
        private readonly EnumMapper enumMapper;
        private readonly TypeRegistry typeRegistry;
        private static readonly DateTime nullDateTime = new DateTime(100, 1, 1);

        public ComObjectMapper(EnumMapper enumMapper, TypeRegistry typeRegistry)
        {
            this.enumMapper = enumMapper;
            this.typeRegistry = typeRegistry;
        }

        public object MapFrom1C(object source, Type type)
        {
            if (source == null || source == DBNull.Value)
                return null;
            if (type == typeof (object))
                if (source is MarshalByRefObject)
                {
                    var typeName = GetFullName(source);
                    type = typeRegistry.GetTypeOrNull(typeName);
                    if (type == null)
                    {
                        const string messageFormat = "can't resolve .NET type by 1c type [{0}]";
                        throw new InvalidOperationException(string.Format(messageFormat, typeName));
                    }
                }
                else
                    type = source.GetType();
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof (DateTime))
            {
                var dateTime = (DateTime) source;
                return dateTime == nullDateTime ? null : source;
            }
            if (type.IsEnum)
                return (bool) ComHelpers.Invoke(source, "IsEmpty")
                    ? null
                    : enumMapper.MapFrom1C(type, source);
            if (typeof (Abstract1CEntity).IsAssignableFrom(type))
            {
                var configurationName = ConfigurationName.GetOrNull(type);
                var isEmpty = configurationName.HasValue &&
                              configurationName.Value.HasReference &&
                              (bool) ComHelpers.Invoke(source, "IsEmpty");
                if (isEmpty)
                    return null;
                var result = (Abstract1CEntity) FormatterServices.GetUninitializedObject(type);
                result.Controller = new EntityController(new ComValueSource(source, this, false));
                return result;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (List<>))
            {
                var itemType = type.GetGenericArguments()[0];
                if (!typeof (Abstract1CEntity).IsAssignableFrom(itemType))
                    throw new InvalidOperationException("assertion failure");
                var itemsCount = Convert.ToInt32(ComHelpers.Invoke(source, "Количество"));
                var list = ListFactory.Create(itemType, null, itemsCount);
                for (var i = 0; i < itemsCount; ++i)
                    list.Add(MapFrom1C(ComHelpers.Invoke(source, "Получить", i), itemType));
                return list;
            }
            return source is IConvertible ? Convert.ChangeType(source, type) : source;
        }

        private static string GetFullName(object source)
        {
            var metadata = ComHelpers.Invoke(source, "Метаданные");
            var result = ComHelpers.Invoke(metadata, "ПолноеИмя");
            return Convert.ToString(result);
        }
    }
}