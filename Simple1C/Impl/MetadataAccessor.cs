using System.Linq;
using Simple1C.Impl.Com;
using Simple1C.Interface;

namespace Simple1C.Impl
{
    internal class MetadataAccessor
    {
        private readonly MappingSource mappingSource;
        private readonly GlobalContext globalContext;

        public MetadataAccessor(MappingSource mappingSource, GlobalContext globalContext)
        {
            this.mappingSource = mappingSource;
            this.globalContext = globalContext;
        }

        public Metadata GetMetadata(ConfigurationName configurationName)
        {
            Metadata result;
            if (!mappingSource.MetadataCache.TryGetValue(configurationName, out result))
            {
                result = CreateMetadata(configurationName);
                mappingSource.MetadataCache.TryAdd(configurationName, result);
            }
            return result;
        }

        private Metadata CreateMetadata(ConfigurationName name)
        {
            var metadata = globalContext.FindByName(name);
            if (name.Scope == ConfigurationScope.Константы)
                return new Metadata(name.Fullname, new[]
                {
                    new MetadataRequisite {MaxLength = GetMaxLength(metadata.ComObject)}
                });
            var descriptor = MetadataHelpers.GetDescriptor(name.Scope);
            var attributes = MetadataHelpers.GetAttributes(metadata.ComObject, descriptor).ToArray();
            var result = new MetadataRequisite[attributes.Length];
            for (var i = 0; i < attributes.Length; i++)
            {
                var attr = attributes[i];
                result[i] = new MetadataRequisite
                {
                    Name = Call.Имя(attr),
                    MaxLength = GetMaxLength(attr)
                };
            }
            return new Metadata(name.Fullname, result);
        }

        private int? GetMaxLength(object attribute)
        {
            var type = ComHelpers.GetProperty(attribute, "Тип");
            var typesObject = ComHelpers.Invoke(type, "Типы");
            var typesCount = Call.Количество(typesObject);
            if (typesCount != 1)
                return null;
            var typeObject = Call.Получить(typesObject, 0);
            var stringPresentation = globalContext.String(typeObject);
            if (stringPresentation != "Строка")
                return null;
            var квалификаторыСтроки = ComHelpers.GetProperty(type, "КвалификаторыСтроки");
            var result = Call.IntProp(квалификаторыСтроки, "Длина");
            if (result == 0)
                return null;
            return result;
        }
    }
}