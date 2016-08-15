using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Interface;

namespace Simple1C.Impl
{
    internal class MetadataAccessor
    {
        private readonly GlobalContext globalContext;

        private static readonly ConcurrentDictionary<ConfigurationName, Metadata> requisiteNames =
            new ConcurrentDictionary<ConfigurationName, Metadata>();

        private readonly Func<ConfigurationName, Metadata> createRequisiteNames;

        public MetadataAccessor(GlobalContext globalContext)
        {
            this.globalContext = globalContext;
            createRequisiteNames = CreateMetadataProperties;
        }

        public Metadata GetMetadata(ConfigurationName configurationName)
        {
            return requisiteNames.GetOrAdd(configurationName, createRequisiteNames);
        }

        public static ConfigurationItemDescriptor GetDescriptor(ConfigurationScope scope)
        {
            return descriptors[scope];
        }

        private Metadata CreateMetadataProperties(ConfigurationName name)
        {
            var metadata = globalContext.FindByName(name);
            if (name.Scope == ConfigurationScope.Константы)
                return new Metadata(name.Fullname, new[]
                {
                    new MetadataRequisite {MaxLength = GetMaxLength(metadata.ComObject)}
                });
            var descriptor = GetDescriptor(name.Scope);
            var attributes = GetAttributes(metadata.ComObject, descriptor).ToArray();
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

        private static readonly string[] standardPropertiesToExclude =
        {
            "ИмяПредопределенныхДанных",
            "Ссылка"
        };

        public static IEnumerable<object> GetAttributes(object comObject, ConfigurationItemDescriptor descriptor)
        {
            var standardAttributes = ComHelpers.GetProperty(comObject, "СтандартныеРеквизиты");
            var isChartOfAccounts = Call.Имя(comObject) == "Хозрасчетный";
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Call.Имя(attr);
                if (isChartOfAccounts && name != "Код" && name != "Наименование")
                    continue;
                if (standardPropertiesToExclude.Contains(name))
                    continue;
                yield return attr;
            }
            foreach (var propertyName in descriptor.AttributePropertyNames)
            {
                var attributes = ComHelpers.GetProperty(comObject, propertyName);
                var attributesCount = Call.Количество(attributes);
                for (var i = 0; i < attributesCount; ++i)
                    yield return Call.Получить(attributes, i);
            }
        }

        private static readonly Dictionary<ConfigurationScope, ConfigurationItemDescriptor> descriptors =
            new Dictionary<ConfigurationScope, ConfigurationItemDescriptor>
            {
                {
                    ConfigurationScope.Справочники, new ConfigurationItemDescriptor
                    {
                        AttributePropertyNames = new[] {"Реквизиты"},
                        HasTableSections = true
                    }
                },
                {
                    ConfigurationScope.Документы,
                    new ConfigurationItemDescriptor
                    {
                        AttributePropertyNames = new[] {"Реквизиты"},
                        HasTableSections = true
                    }
                },
                {
                    ConfigurationScope.РегистрыСведений,
                    new ConfigurationItemDescriptor
                    {
                        AttributePropertyNames = new[] {"Реквизиты", "Измерения", "Ресурсы"}
                    }
                },
                {
                    ConfigurationScope.ПланыСчетов,
                    new ConfigurationItemDescriptor
                    {
                        AttributePropertyNames = new[] {"Реквизиты"},
                        HasTableSections = true
                    }
                },
                {
                    ConfigurationScope.ПланыВидовХарактеристик,
                    new ConfigurationItemDescriptor
                    {
                        AttributePropertyNames = new[] {"Реквизиты"},
                        HasTableSections = true
                    }
                }
            };
    }
}