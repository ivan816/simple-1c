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
        private readonly GlobalContext globalContext;
        private static readonly DateTime nullDateTime = new DateTime(100, 1, 1);

        public ComObjectMapper(EnumMapper enumMapper, TypeRegistry typeRegistry, GlobalContext globalContext)
        {
            this.enumMapper = enumMapper;
            this.typeRegistry = typeRegistry;
            this.globalContext = globalContext;
        }

        public object MapFrom1C(object source, Type type)
        {
            if (source == null || source == DBNull.Value)
                return null;
            if (type == typeof(object))
                if (source is MarshalByRefObject)
                {
                    var typeName = GetFullName(source);
                    type = GetTypeByTypeName(typeName);
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
                return Guid.Parse(globalContext.String(source));
            if (type.IsEnum)
                return Call.IsEmpty(source) ? null : enumMapper.MapFrom1C(type, source);
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

        private Type ConvertType(object source)
        {
            var metadata = Call.НайтиПоТипу(globalContext.Metadata, source);
            var typeName = Call.ПолноеИмя(metadata);
            return GetTypeByTypeName(typeName);
        }

        private Type GetTypeByTypeName(string typeName)
        {
            var type = typeRegistry.GetTypeOrNull(typeName);
            if (type == null)
            {
                const string messageFormat = "can't resolve .NET type by 1c type [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, typeName));
            }
            return type;
        }

        private static string GetFullName(object source)
        {
            var metadata = ComHelpers.Invoke(source, "Метаданные");
            return Call.ПолноеИмя(metadata);
        }
    }
}