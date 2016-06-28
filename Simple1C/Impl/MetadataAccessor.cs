using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Interface;

namespace Simple1C.Impl
{
    internal class MetadataAccessor
    {
        private readonly GlobalContext globalContext;

        private readonly ConcurrentDictionary<ConfigurationName, string[]> requisiteNames =
            new ConcurrentDictionary<ConfigurationName, string[]>();

        private readonly Func<ConfigurationName, string[]> createRequisiteNames;

        public MetadataAccessor(GlobalContext globalContext)
        {
            this.globalContext = globalContext;
            createRequisiteNames = CreateRequisiteNames;
        }

        public string[] GetRequisiteNames(ConfigurationName configurationName)
        {
            return requisiteNames.GetOrAdd(configurationName, createRequisiteNames);
        }

        public static ConfigurationItemDescriptor GetDescriptor(ConfigurationScope scope)
        {
            return descriptors[scope];
        }

        private string[] CreateRequisiteNames(ConfigurationName configurationName)
        {
            var metadata = globalContext.Metadata;
            var itemMetadata = ComHelpers.Invoke(metadata, "НайтиПоПолномуИмени", configurationName.Fullname);
            var result = new List<string>();
            var standardAttributes = ComHelpers.GetProperty(itemMetadata, "СтандартныеРеквизиты");
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Convert.ToString(ComHelpers.GetProperty(attr, "Имя"));
                result.Add(name);
            }
            var descriptor = GetDescriptor(configurationName.Scope);
            foreach (var propertyName in descriptor.AttributePropertyNames)
            {
                var attributes = ComHelpers.GetProperty(itemMetadata, propertyName);
                var attributesCount = Convert.ToInt32(ComHelpers.Invoke(attributes, "Количество"));
                for (var i = 0; i < attributesCount; ++i)
                {
                    var attr = ComHelpers.Invoke(attributes, "Получить", i);
                    var name = Convert.ToString(ComHelpers.GetProperty(attr, "Имя"));
                    result.Add(name);
                }
            }
            return result.ToArray();
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
                }
            };
    }
}