using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Simple1C.Impl.Com;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal class ComObjectMapper
    {
        private static readonly DateTime nullDateTime = new DateTime(100, 1, 1);
        private readonly MappingSource mappingSource;
        private readonly GlobalContext globalContext;
        private readonly object enumerations;

        public ComObjectMapper(MappingSource mappingSource, GlobalContext globalContext)
        {
            this.mappingSource = mappingSource;
            this.globalContext = globalContext;
            enumerations = ComHelpers.GetProperty(globalContext.ComObject(), "Перечисления");
        }

        public object MapTo1C(object value)
        {
            if (value == null)
                return null;
            if (value.GetType().IsEnum)
                return MapEnumTo1C(value);
            if (value is Guid)
                return MapGuidTo1C((Guid) value);
            return value;
        }

        public object MapGuidTo1C(object value)
        {
            return value == null ? null : MapGuidTo1C((Guid) value);
        }

        public object MapGuidTo1C(Guid value)
        {
            return ComHelpers.Invoke(globalContext.ComObject(),
                "NewObject", "УникальныйИдентификатор", value.ToString());
        }

        public object MapFrom1C(object source, Type type)
        {
            if (source == null || source == DBNull.Value)
                return null;
            if (type == typeof(object))
                if (source is MarshalByRefObject)
                {
                    var typeName = GetFullName(source);
                    type = mappingSource.TypeRegistry.GetType(typeName);
                }
                else
                    type = source.GetType();
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(DateTime))
            {
                var dateTime = (DateTime) source;
                return dateTime == nullDateTime ? null : source;
            }
            if (type == typeof(Guid))
            {
                var guid = Guid.Parse(globalContext.String(source));
                return guid == Guid.Empty ? (object) null : guid;
            }
            if (type.IsEnum)
                return Call.IsEmpty(source) ? null : MapEnumFrom1C(type, source);
            if (type == typeof(Type))
                return ConvertType(source);
            if (type == typeof(Type[]))
            {
                var typesObject = ComHelpers.Invoke(source, "Типы");
                var typesCount = Call.Количество(typesObject);
                var result = new Type[typesCount];
                for (var i = 0; i < result.Length; i++)
                {
                    var typeObject = Call.Получить(typesObject, i);
                    result[i] = ConvertType(typeObject);
                }
                return result;
            }
            if (typeof(Abstract1CEntity).IsAssignableFrom(type))
            {
                var configurationName = ConfigurationName.GetOrNull(type);
                var isEmpty = configurationName.HasValue &&
                              configurationName.Value.HasReference &&
                              Call.IsEmpty(source);
                if (isEmpty)
                    return null;
                var result = (Abstract1CEntity) FormatterServices.GetUninitializedObject(type);
                result.Controller = new EntityController(new ComValueSource(source, this, false));
                return result;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = type.GetGenericArguments()[0];
                if (!typeof(Abstract1CEntity).IsAssignableFrom(itemType))
                    throw new InvalidOperationException("assertion failure");
                var itemsCount = Call.Количество(source);
                var list = ListFactory.Create(itemType, null, itemsCount);
                for (var i = 0; i < itemsCount; ++i)
                    list.Add(MapFrom1C(Call.Получить(source, i), itemType));
                return list;
            }
            return source is IConvertible ? Convert.ChangeType(source, type) : source;
        }

        public object MapEnumTo1C(object value)
        {
            var enumeration = ComHelpers.GetProperty(enumerations, value.GetType().Name);
            return ComHelpers.GetProperty(enumeration, value.ToString());
        }

        public object MapEnumTo1C(int valueIndex, Type enumType)
        {
            var enumValue = Enum.GetValues(enumType).GetValue(valueIndex);
            return MapTo1C(enumValue);
        }

        private Type ConvertType(object source)
        {
            var metadata = Call.НайтиПоТипу(globalContext.Metadata, source);
            var typeName = Call.ПолноеИмя(metadata);
            return mappingSource.TypeRegistry.GetType(typeName);
        }

        private static string GetFullName(object source)
        {
            var metadata = ComHelpers.Invoke(source, "Метаданные");
            return Call.ПолноеИмя(metadata);
        }

        private object MapEnumFrom1C(Type enumType, object value1C)
        {
            var enumeration = ComHelpers.GetProperty(enumerations, enumType.Name);
            var valueIndex = Convert.ToInt32(ComHelpers.Invoke(enumeration, "IndexOf", value1C));
            var result = mappingSource.EnumMappingsCache.GetOrAdd(enumType, GetMappings)
                .SingleOrDefault(x => x.index == valueIndex);
            if (result == null)
            {
                const string messageFormat = "can't map value [{0}] to enum [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    globalContext.String(value1C), enumType.Name));
            }
            return result.value;
        }

        private EnumMapItem[] GetMappings(Type enumType)
        {
            var enumeration = ComHelpers.GetProperty(enumerations, enumType.Name);
            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(v => new EnumMapItem
                {
                    value = v,
                    index = Convert.ToInt32(ComHelpers.Invoke(enumeration, "IndexOf",
                        ComHelpers.GetProperty(enumeration, v.ToString())))
                })
                .ToArray();
        }
    }
}