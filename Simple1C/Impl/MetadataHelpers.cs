using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Interface;

namespace Simple1C.Impl
{
    internal static class MetadataHelpers
    {
        private static readonly Dictionary<string, string> simpleTypesMap = new Dictionary<string, string>
        {
            {"Строка", "string"},
            {"Булево", "bool"},
            {"Дата", "DateTime?"},
            {"Уникальный идентификатор", "Guid?"},
            {"Описание типов", "Type[]"},
            {"ВидСчета", "int"}
        };

        public static List<TypeInfo> GetTypesOrNull(this GlobalContext globalContext, object item)
        {
            var typeDescriptor = ComHelpers.GetProperty(item, "Тип");
            var types = ComHelpers.Invoke(typeDescriptor, "Типы");
            var typesCount = Call.Количество(types);
            if (typesCount == 0)
                throw new InvalidOperationException("assertion failure");
            var result = new List<TypeInfo>();
            for (var i = 0; i < typesCount; i++)
            {
                var typeObject = Call.Получить(types, i);
                var typeInfo = GetTypeInfoOrNull(globalContext, typeDescriptor, typeObject);
                if (typeInfo.HasValue)
                    result.Add(typeInfo.Value);
            }
            return result.Count == 0 ? null : result;
        }

        private static TypeInfo? GetTypeInfoOrNull(GlobalContext globalContext, object typeDescriptor, object type)
        {
            var typeAsString = globalContext.String(type);
            if (typeAsString == "Число")
            {
                var квалификаторыЧисла = ComHelpers.GetProperty(typeDescriptor, "КвалификаторыЧисла");
                var floatLength = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "РазрядностьДробнойЧасти"));
                var digits = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "Разрядность"));
                return TypeInfo.Simple(floatLength == 0 ? (digits < 10 ? "int" : "long") : "decimal");
            }
            if (typeAsString == "Строка")
            {
                var квалификаторыСтроки = ComHelpers.GetProperty(typeDescriptor, "КвалификаторыСтроки");
                var maxLength = Call.IntProp(квалификаторыСтроки, "Длина");
                return TypeInfo.Simple("string", maxLength == 0 ? (int?) null : maxLength);
            }
            if (typeAsString == "Хранилище значения")
                return null;
            string typeName;
            if (simpleTypesMap.TryGetValue(typeAsString, out typeName))
                return TypeInfo.Simple(typeName);
            var comObject = GetMetaByType(globalContext, type);
            var fullName = Call.ПолноеИмя(comObject);
            var name = ConfigurationName.ParseOrNull(fullName);
            if (!name.HasValue)
                return null;
            return new TypeInfo {configurationItem = new ConfigurationItem(name.Value, comObject)};
        }

        public static object GetMetaByType(this GlobalContext globalContext, object type)
        {
            var result = Call.НайтиПоТипу(globalContext.Metadata, type);
            if (result == null)
            {
                const string messageFormat = "can't find meta by type [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    globalContext.String(type)));
            }
            return result;
        }

        public static readonly ConfigurationItemDescriptor tableSectionDescriptor = new ConfigurationItemDescriptor
        {
            AttributePropertyNames = new[] {"Реквизиты"}
        };

        public static readonly ConfigurationItemDescriptor standardTableSectionDescriptor = new ConfigurationItemDescriptor
        {
            AttributePropertyNames = new string[0]
        };

        private static readonly string[] standardPropertiesToExclude =
        {
            "ИмяПредопределенныхДанных",
            "Ссылка"
        };

        public static ConfigurationItemDescriptor GetDescriptor(ConfigurationScope scope)
        {
            var result = GetDescriptorOrNull(scope);
            if (result == null)
            {
                const string messageFormat = "no descriptor defined for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, scope));
            }
            return result;
        }

        public static ConfigurationItemDescriptor GetDescriptorOrNull(ConfigurationScope scope)
        {
            ConfigurationItemDescriptor result;
            return descriptors.TryGetValue(scope, out result) ? result : null;
        }

        public static IEnumerable<object> GetAttributes(object comObject, ConfigurationItemDescriptor descriptor)
        {
            var standardAttributes = ComHelpers.GetProperty(comObject, "СтандартныеРеквизиты");
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Call.Имя(attr);
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
                    ConfigurationScope.РегистрыБухгалтерии,
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
                        HasStandardTableSections = true,
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