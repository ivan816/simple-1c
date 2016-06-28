using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Com;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;

namespace Simple1C.Impl.Generation
{
    internal class ObjectModelGenerator
    {
        private static readonly Dictionary<string, string> simpleTypesMap = new Dictionary<string, string>
        {
            {"Строка", "string"},
            {"Булево", "bool"},
            {"Дата", "DateTime?"},
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

        

        private readonly GlobalContext globalContext;
        private readonly object metadata;
        private readonly IEnumerable<string> itemNames;
        private readonly string namespaceRoot;
        private readonly string targetDirectory;

        public ObjectModelGenerator(object globalContext, IEnumerable<string> itemNames,
            string namespaceRoot, string targetDirectory)
        {
            this.globalContext = new GlobalContext(globalContext);
            metadata = this.globalContext.Metadata;
            this.itemNames = itemNames;
            this.namespaceRoot = namespaceRoot;
            this.targetDirectory = targetDirectory;
        }

        public IEnumerable<string> Generate()
        {
            var generationContext = new GenerationContext(targetDirectory);
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
            return generationContext.GetWrittenFiles();
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

        private string GetNamespaceName(ConfigurationScope scope)
        {
            return namespaceRoot + "." + scope;
        }

        private void GenerateClass(ConfigurationItem item, GenerationContext context)
        {
            var classContent = GenerateClassContent(MetadataAccessor.GetDescriptor(item.Name.Scope),
                item.Name.Fullname, item.ComObject, context);
            var fileContent = GeneratorTemplates.classFormat.Apply(new FormatParameters()
                .With("namespace-name", GetNamespaceName(item.Name.Scope))
                .With("configuration-scope", item.Name.Scope.ToString())
                .With("class-name", item.Name.Name)
                .With("content", classContent));
            context.Write(item.Name.Scope, item.Name.Name, fileContent);
        }

        private string GenerateClassContent(ConfigurationItemDescriptor descriptor,
            string configurationItemFullName, object comObject, GenerationContext context)
        {
            var properties = new List<string>();
            var standardAttributes = ComHelpers.GetProperty(comObject, "СтандартныеРеквизиты");
            var isChartOfAccounts = Convert.ToString(ComHelpers.GetProperty(comObject, "Имя")) == "Хозрасчетный";
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Convert.ToString(ComHelpers.GetProperty(attr, "Имя"));
                if (isChartOfAccounts && name != "Код" && name != "Наименование")
                    continue;
                if (standardPropertiesToExclude.Contains(name))
                    continue;
                properties.Add(FormatProperty(attr, configurationItemFullName, context));
            }
            foreach (var propertyName in descriptor.AttributePropertyNames)
            {
                var attributes = ComHelpers.GetProperty(comObject, propertyName);
                var attributesCount = Convert.ToInt32(ComHelpers.Invoke(attributes, "Количество"));
                for (var i = 0; i < attributesCount; ++i)
                {
                    var attr = ComHelpers.Invoke(attributes, "Получить", i);
                    properties.Add(FormatProperty(attr, configurationItemFullName, context));
                }
            }
            var nestedClasses = new List<string>();
            if (descriptor.HasTableSections)
            {
                var tableSections = ComHelpers.GetProperty(comObject, "ТабличныеЧасти");
                var tableSectionsCount = Convert.ToInt32(ComHelpers.Invoke(tableSections, "Количество"));
                for (var i = 0; i < tableSectionsCount; i++)
                {
                    var tableSection = ComHelpers.Invoke(tableSections, "Получить", i);
                    var tableSectionName = Convert.ToString(ComHelpers.GetProperty(tableSection, "Имя"));
                    var nestedClassContent = GenerateClassContent(tableSectionDescriptor,
                        Convert.ToString(ComHelpers.Invoke(tableSection, "ПолноеИмя")),
                        tableSection, context);
                    var nestedClassName = "ТабличнаяЧасть" + tableSectionName;
                    var nestedClass = GeneratorTemplates.nestedClassFormat.Apply(new FormatParameters()
                        .With("class-name", nestedClassName)
                        .With("content", nestedClassContent));
                    nestedClasses.Add(GenerationHelpers.IncrementIndent(nestedClass));
                    var formattedProperty = GeneratorTemplates.propertyFormat.Apply(new FormatParameters()
                        .With("type", string.Format("List<{0}>", nestedClassName))
                        .With("field-name", ToCamel(tableSectionName))
                        .With("property-name", tableSectionName));
                    properties.Add(formattedProperty);
                }
            }
            return properties.NotNull().Concat(nestedClasses).JoinStrings("\r\n\r\n");
        }

        private string FormatProperty(object attribute, string configurationItemFullName, GenerationContext context)
        {
            var propertyName = Convert.ToString(ComHelpers.GetProperty(attribute, "Имя"));
            var type = ComHelpers.GetProperty(attribute, "Тип");
            var typesObject = ComHelpers.Invoke(type, "Типы");
            var typesCount = Convert.ToInt32(ComHelpers.Invoke(typesObject, "Количество"));
            if (typesCount == 0)
            {
                const string messageFormat = "no types for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, configurationItemFullName, propertyName));
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
                        var propertyItem = FindByType(typeObject);
                        if (propertyItem != null)
                        {
                            propertyType = FormatClassName(propertyItem.Name);
                            context.EnqueueIfNeeded(propertyItem);
                            isEnum = propertyItem.Name.Scope == ConfigurationScope.Перечисления;
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

        private string FormatClassName(ConfigurationName name)
        {
            return GetNamespaceName(name.Scope) + "." + name.Name;
        }

        private void GenerateEnum(ConfigurationItem item, GenerationContext context)
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
                .With("namespace-name", GetNamespaceName(item.Name.Scope))
                .With("name", item.Name.Name)
                .With("content", items.JoinStrings(",\r\n")));
            context.Write(ConfigurationScope.Перечисления, item.Name.Name, enumContent);
        }

        private static string ToCamel(string s)
        {
            return char.ToLower(s[0]) + s.Substring(1);
        }
    }
}