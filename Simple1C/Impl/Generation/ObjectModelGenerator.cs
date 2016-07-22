using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation.Rendering;
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
            {"Уникальный идентификатор", "Guid?"}
        };

        private static readonly string[] standardPropertiesToExclude =
        {
            "ИмяПредопределенныхДанных",
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
            var classContext = new ClassGenerationContext
            {
                configurationName = item.Name,
                target = new ClassModel
                {
                    Name = item.Name.Name,
                    ConfigurationScope = item.Name.Scope
                },
                comObject = item.ComObject,
                generationContext = context,
                descriptor = MetadataAccessor.GetDescriptor(item.Name.Scope),
                configurationItemFullName = item.Name.Fullname
            };
            EmitClass(classContext);
            var fileTemplate = new ClassFileTemplate
            {
                Model = new ClassFileModel
                {
                    Namespace = GetNamespaceName(item.Name.Scope),
                    MainClass = classContext.target
                }
            };
            context.Write(item.Name, fileTemplate.TransformText());
        }

        private void EmitClass(ClassGenerationContext classContext)
        {
            var standardAttributes = ComHelpers.GetProperty(classContext.comObject, "СтандартныеРеквизиты");
            var isChartOfAccounts = Convert.ToString(ComHelpers.GetProperty(classContext.comObject, "Имя")) ==
                                    "Хозрасчетный";
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Convert.ToString(ComHelpers.GetProperty(attr, "Имя"));
                if (isChartOfAccounts && name != "Код" && name != "Наименование")
                    continue;
                if (standardPropertiesToExclude.Contains(name))
                    continue;
                EmitProperty(attr, classContext);
            }
            foreach (var propertyName in classContext.descriptor.AttributePropertyNames)
            {
                var attributes = ComHelpers.GetProperty(classContext.comObject, propertyName);
                var attributesCount = Convert.ToInt32(ComHelpers.Invoke(attributes, "Количество"));
                for (var i = 0; i < attributesCount; ++i)
                {
                    var attr = ComHelpers.Invoke(attributes, "Получить", i);
                    EmitProperty(attr, classContext);
                }
            }
            if (classContext.configurationName.HasValue && classContext.configurationName.Value.HasReference)
                classContext.EmitProperty(new PropertyModel
                {
                    Type = "Guid?",
                    PropertyName = EntityHelpers.idPropertyName
                });
            if (classContext.descriptor.HasTableSections)
            {
                var tableSections = ComHelpers.GetProperty(classContext.comObject, "ТабличныеЧасти");
                var tableSectionsCount = Convert.ToInt32(ComHelpers.Invoke(tableSections, "Количество"));
                for (var i = 0; i < tableSectionsCount; i++)
                {
                    var tableSection = ComHelpers.Invoke(tableSections, "Получить", i);
                    var tableSectionName = Convert.ToString(ComHelpers.GetProperty(tableSection, "Имя"));
                    var nestedClassContext = new ClassGenerationContext
                    {
                        configurationItemFullName = Convert.ToString(ComHelpers.Invoke(tableSection, "ПолноеИмя")),
                        comObject = tableSection,
                        descriptor = tableSectionDescriptor,
                        generationContext = classContext.generationContext,
                        target = new ClassModel
                        {
                            Name = "ТабличнаяЧасть" + tableSectionName
                        }
                    };
                    EmitClass(nestedClassContext);
                    classContext.EmitNestedClass(nestedClassContext.target);
                    classContext.EmitProperty(new PropertyModel
                    {
                        Type = string.Format("List<{0}>", nestedClassContext.target.Name),
                        PropertyName = tableSectionName
                    });
                }
            }
        }

        private void EmitProperty(object attribute, ClassGenerationContext classContext)
        {
            var propertyName = Convert.ToString(ComHelpers.GetProperty(attribute, "Имя"));
            var type = ComHelpers.GetProperty(attribute, "Тип");
            var typesObject = ComHelpers.Invoke(type, "Типы");
            var typesCount = Convert.ToInt32(ComHelpers.Invoke(typesObject, "Количество"));
            if (typesCount == 0)
            {
                const string messageFormat = "no types for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, classContext.configurationItemFullName,
                    propertyName));
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
                            classContext.generationContext.EnqueueIfNeeded(configurationItem);
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
                            classContext.generationContext.EnqueueIfNeeded(propertyItem);
                            isEnum = propertyItem.Name.Scope == ConfigurationScope.Перечисления;
                        }
                    }
                }
            }
            if (propertyType != null)
                classContext.target.Properties.Add(new PropertyModel
                {
                    Type = propertyType + (isEnum ? "?" : ""),
                    PropertyName = propertyName
                });
        }

        private string FormatClassName(ConfigurationName name)
        {
            return GetNamespaceName(name.Scope) + "." + name.Name;
        }

        private void GenerateEnum(ConfigurationItem item, GenerationContext context)
        {
            var model = new EnumFileModel
            {
                Name = item.Name.Name,
                Namespace = GetNamespaceName(item.Name.Scope)
            };
            var values = ComHelpers.GetProperty(item.ComObject, "ЗначенияПеречисления");
            var count = Convert.ToInt32(ComHelpers.Invoke(values, "Количество"));
            for (var i = 0; i < count; i++)
            {
                var value = ComHelpers.Invoke(values, "Получить", i);
                model.Items.Add(new EnumItemModel
                {
                    Name = Convert.ToString(ComHelpers.GetProperty(value, "Имя")),
                    Synonym = Convert.ToString(ComHelpers.GetProperty(value, "Синоним"))
                        .Replace("\"", "\\\"")
                });
            }
            var enumFileTemplate = new EnumFileTemplate {Model = model};
            context.Write(item.Name, enumFileTemplate.TransformText());
        }

        private class ClassGenerationContext
        {
            public ConfigurationItemDescriptor descriptor;
            public string configurationItemFullName;
            public object comObject;
            public GenerationContext generationContext;
            public ConfigurationName? configurationName;
            public ClassModel target;

            public void EmitProperty(PropertyModel property)
            {
                target.Properties.Add(property);
            }

            public void EmitNestedClass(ClassModel classModel)
            {
                target.NestedClasses.Add(classModel);
            }
        }
    }
}