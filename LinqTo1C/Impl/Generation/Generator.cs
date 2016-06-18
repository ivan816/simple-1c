using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqTo1C.Impl.Com;
using LinqTo1C.Impl.Helpers;
using LinqTo1C.Interface;

namespace LinqTo1C.Impl.Generation
{
    public class Generator
    {
        private static readonly Dictionary<string, string> simpleTypesMap = new Dictionary<string, string>
        {
            {"Строка", "string"},
            {"Булево", "bool"},
            {"Дата", "DateTime"},
            //todo какое-то говно, разобраться
            {"Хранилище значения", null},
            {"Уникальный идентификатор", null}
        };

        private static readonly string[] standardPropertiesToExclude =
        {
            "ИмяПредопределенныхДанных",
            "Предопределенный",
            "Ссылка"
        };

        private static readonly ConfigurationItemDescriptor tableSectionDescriptor = new ConfigurationItemDescriptor
        {
            AttributePropertyNames = new[] {"Реквизиты"}
        };

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

        private readonly GlobalContext globalContext;
        private readonly object metadata;
        private readonly IEnumerable<string> itemNames;
        private readonly string namespaceRoot;
        private readonly string targetDirectory;

        public Generator(GlobalContext globalContext, IEnumerable<string> itemNames,
            string namespaceRoot, string targetDirectory)
        {
            this.globalContext = globalContext;
            metadata = globalContext.Metadata;
            this.itemNames = itemNames;
            this.namespaceRoot = namespaceRoot;
            this.targetDirectory = targetDirectory;
        }

        public string[] Generate()
        {
            var generationContext = new GenerationContext();
            foreach (var itemName in itemNames)
            {
                var item = FindByFullName(itemName);
                generationContext.EnqueueIfNeeded(item);
            }
            var processedCount = 0;
            while (generationContext.ItemsToProcess.Count > 0)
            {
                var item = generationContext.ItemsToProcess.Dequeue();
                switch (item.Name.Scope)
                {
                    case ConfigurationScope.Справочники:
                    case ConfigurationScope.Документы:
                    case ConfigurationScope.РегистрыСведений:
                    case ConfigurationScope.ПланыСчетов:
                        GenerateClass(item, generationContext);
                        break;
                    case ConfigurationScope.Перечисления:
                        GenerateEnum(item, generationContext);
                        break;
                    default:
                        const string messageFormat = "unexpected scope for [{0}]";
                        throw new InvalidOperationException(string.Format(messageFormat, item.Name));
                }
                processedCount++;
                if (processedCount%10 == 0)
                    Console.Out.WriteLine("[{0}] items processed, queue length [{1}]",
                        processedCount, generationContext.ItemsToProcess.Count);
            }
            var fileNames = new List<string>();
            foreach (var items in generationContext.GetNamespaces())
            {
                var namespaceName = GetNamespaceName(items.Key);
                var content = GeneratorTemplates.namespaceFormat.Apply(new FormatParameters()
                    .With("namespace-name", namespaceRoot + "." + namespaceName)
                    .With("content", items.Value.JoinStrings("\r\n\r\n")));
                var filePath = Path.Combine(targetDirectory, namespaceName + ".cs");
                File.WriteAllText(filePath, content);
                fileNames.Add(filePath);
            }
            return fileNames.ToArray();
        }

        private static string GetNamespaceName(ConfigurationScope scope)
        {
            return scope.ToString();
        }

        private ConfigurationItem FindByFullName(string fullname)
        {
            return new ConfigurationItem(fullname, ComHelpers.Invoke(metadata, "НайтиПоПолномуИмени", fullname));
        }

        private ConfigurationItem FindByType(object typeObject)
        {
            var comObject = ComHelpers.Invoke(metadata, "НайтиПоТипу", typeObject);
            var fullName = Convert.ToString(ComHelpers.Invoke(comObject, "ПолноеИмя"));
            return fullName.StartsWith("Документ") || fullName.StartsWith("Справочник")
                   || fullName.StartsWith("Перечисление") || fullName.StartsWith("ПланСчетов")
                ? new ConfigurationItem(fullName, comObject)
                : null;
        }

        private void GenerateClass(ConfigurationItem item, GenerationContext context)
        {
            var content = GenerateClassContent(new ClassDescriptor
            {
                fullConfigurationItemName = item.Name.Fullname,
                className = item.Name.Name,
                itemDescriptor = descriptors[item.Name.Scope]
            }, item.ComObject, context);
            var scopeAttribute = string.Format("    [ConfigurationScope(ConfigurationScope.{0})]", item.Name.Scope);
            context.AddItem(item.Name.Scope, scopeAttribute + "\r\n" + content);
        }

        private string GenerateClassContent(ClassDescriptor classDescriptor, object comObject,
            GenerationContext context)
        {
            var properties = new List<string>();
            var standardAttributes = ComHelpers.GetProperty(comObject, "СтандартныеРеквизиты");
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Convert.ToString(ComHelpers.GetProperty(attr, "Имя"));
                if (classDescriptor.className == "Хозрасчетный" && name != "Код" && name != "Наименование")
                    continue;
                if (standardPropertiesToExclude.Contains(name))
                    continue;
                properties.Add(FormatProperty(attr, context, classDescriptor.fullConfigurationItemName));
            }
            foreach (var propertyName in classDescriptor.itemDescriptor.AttributePropertyNames)
            {
                var attributes = ComHelpers.GetProperty(comObject, propertyName);
                var attributesCount = Convert.ToInt32(ComHelpers.Invoke(attributes, "Количество"));
                for (var i = 0; i < attributesCount; ++i)
                {
                    var attr = ComHelpers.Invoke(attributes, "Получить", i);
                    var имя = Convert.ToString(ComHelpers.GetProperty(attr, "Имя"));
                    if (classDescriptor.className == "Хозрасчетный" && имя != "Код" && имя != "Наименование")
                        continue;
                    properties.Add(FormatProperty(attr, context, classDescriptor.fullConfigurationItemName));
                }
            }
            var nestedClasses = new List<string>();
            if (classDescriptor.itemDescriptor.HasTableSections)
            {
                var tableSections = ComHelpers.GetProperty(comObject, "ТабличныеЧасти");
                var tableSectionsCount = Convert.ToInt32(ComHelpers.Invoke(tableSections, "Количество"));
                for (var i = 0; i < tableSectionsCount; i++)
                {
                    var tableSection = ComHelpers.Invoke(tableSections, "Получить", i);
                    var tableSectionName = Convert.ToString(ComHelpers.GetProperty(tableSection, "Имя"));
                    var nestedClassName = "ТабличнаяЧасть" + tableSectionName;
                    var nestedClassContent = GenerateClassContent(new ClassDescriptor
                    {
                        className = nestedClassName,
                        itemDescriptor = tableSectionDescriptor,
                        fullConfigurationItemName = Convert.ToString(ComHelpers.Invoke(tableSection, "ПолноеИмя"))
                    }, tableSection, context);
                    nestedClasses.Add(GenerationHelpers.IncrementIndent(nestedClassContent));
                    var formattedProperty = GeneratorTemplates.propertyFormat.Apply(new FormatParameters()
                        .With("type", string.Format("List<{0}>", nestedClassName))
                        .With("field-name", ToCamel(tableSectionName))
                        .With("property-name", tableSectionName));
                    properties.Add(formattedProperty);
                }
            }
            return GeneratorTemplates.classFormat.Apply(new FormatParameters()
                .With("class-name", classDescriptor.className)
                .With("content", properties.NotNull().Concat(nestedClasses).JoinStrings("\r\n\r\n")));
        }

        private string FormatProperty(object attribute, GenerationContext context, string name)
        {
            var propertyName = Convert.ToString(ComHelpers.GetProperty(attribute, "Имя"));
            var type = ComHelpers.GetProperty(attribute, "Тип");
            var typesObject = ComHelpers.Invoke(type, "Типы");
            var typesCount = Convert.ToInt32(ComHelpers.Invoke(typesObject, "Количество"));
            if (typesCount == 0)
            {
                const string messageFormat = "no types for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, name, propertyName));
            }
            var isEnum = false;
            string propertyType;
            if (typesCount > 1)
            {
                for (var i = 0; i < typesCount; i++)
                {
                    var typeObject = ComHelpers.Invoke(typesObject, "Получить", i);
                    var stringPresentation = globalContext.String(typeObject);
                    if (!simpleTypesMap.TryGetValue(stringPresentation, out propertyType) &&
                        stringPresentation != "Число")
                    {
                        var configurationItem = FindByType(typeObject);
                        if (configurationItem != null)
                            context.EnqueueIfNeeded(configurationItem);
                    }
                }
                propertyType = "object";
            }
            else
            {
                var typeObject = ComHelpers.Invoke(typesObject, "Получить", 0);
                var stringPresentation = globalContext.String(typeObject);
                if (!simpleTypesMap.TryGetValue(stringPresentation, out propertyType))
                {
                    if (stringPresentation == "Число")
                    {
                        var квалификаторыЧисла = ComHelpers.GetProperty(type, "КвалификаторыЧисла");
                        var floatLength =
                            Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "РазрядностьДробнойЧасти"));
                        var digits = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "Разрядность"));
                        if (floatLength == 0)
                            propertyType = digits < 10 ? "int" : "long";
                        else
                            propertyType = "decimal";
                    }
                    else
                    {
                        try
                        {
                            var propertyItem = FindByType(typeObject);
                            if (propertyItem != null)
                            {
                                propertyType = FormatClassName(propertyItem.Name);
                                context.EnqueueIfNeeded(propertyItem);
                                isEnum = propertyItem.Name.Scope == ConfigurationScope.Перечисления;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new InvalidOperationException(
                                "shit: " + name + "." + propertyName + ", " + stringPresentation, e);
                        }
                    }
                }
            }
            if (propertyType != null)
            {
                return GeneratorTemplates.propertyFormat.Apply(new FormatParameters()
                    .With("type", propertyType + (isEnum ? "?" : ""))
                    .With("field-name", ToCamel(propertyName))
                    .With("property-name", propertyName));
            }
            return null;
        }

        private static string FormatClassName(ConfigurationName name)
        {
            return GetNamespaceName(name.Scope) + "." + name.Name;
        }

        private static void GenerateEnum(ConfigurationItem item, GenerationContext context)
        {
            var items = new List<string>();
            var values = ComHelpers.GetProperty(item.ComObject, "ЗначенияПеречисления");
            var count = Convert.ToInt32(ComHelpers.Invoke(values, "Количество"));
            for (var i = 0; i < count; i++)
            {
                var value = ComHelpers.Invoke(values, "Получить", i);
                items.Add("\t\t" + Convert.ToString(ComHelpers.GetProperty(value, "Имя")));
            }
            var enumContent = GeneratorTemplates.enumFormat.Apply(new FormatParameters()
                .With("name", item.Name.Name)
                .With("content", items.JoinStrings(",\r\n")));
            context.AddItem(item.Name.Scope, enumContent);
        }

        private static string ToCamel(string s)
        {
            return char.ToLower(s[0]) + s.Substring(1);
        }

        private class ConfigurationItemDescriptor
        {
            public string[] AttributePropertyNames { get; set; }
            public bool HasTableSections { get; set; }
        }

        private class ClassDescriptor
        {
            public ConfigurationItemDescriptor itemDescriptor;
            public string fullConfigurationItemName;
            public string className;
        }
    }
}