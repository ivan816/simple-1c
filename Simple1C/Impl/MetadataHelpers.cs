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
        public static readonly Dictionary<string, string> simpleTypesMap = new Dictionary<string, string>
        {
            {"Строка", "string"},
            {"Булево", "bool"},
            {"Дата", "DateTime?"},
            {"Уникальный идентификатор", "Guid?"},
            {"Хранилище значения", null},
            {"Описание типов", "Type[]"}
        };

        public static readonly ConfigurationItemDescriptor tableSectionDescriptor = new ConfigurationItemDescriptor
        {
            AttributePropertyNames = new[] { "Реквизиты" }
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
            var isChartOfAccounts = Call.Имя(comObject) == "Хозрасчетный";
            foreach (var attr in (IEnumerable)standardAttributes)
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