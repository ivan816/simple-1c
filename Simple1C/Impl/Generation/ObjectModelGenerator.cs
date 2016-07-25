using System;
using System.Collections;
using System.Collections.Generic;
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
            {"Уникальный идентификатор", "Guid?"},
            {"Хранилище значения", null}
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
            EmitConstants(generationContext);
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
                        EmitEnum(item, generationContext);
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
            var fullName = Call.ПолноеИмя(comObject);
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
            var isChartOfAccounts = Call.Имя(classContext.comObject) == "Хозрасчетный";
            if (classContext.target.ConfigurationScope.HasValue)
            {
                var synonym = Call.Синоним(classContext.comObject);
                classContext.target.Synonym = GenerateHelpers.EscapeString(synonym);
                if (classContext.target.ConfigurationScope.Value != ConfigurationScope.РегистрыСведений)
                {
                    var presentation =
                        Convert.ToString(ComHelpers.GetProperty(classContext.comObject, "ObjectPresentation"));
                    classContext.target.ObjectPresentation = GenerateHelpers.EscapeString(presentation);
                }
            }
            foreach (var attr in (IEnumerable) standardAttributes)
            {
                var name = Call.Имя(attr);
                if (isChartOfAccounts && name != "Код" && name != "Наименование")
                    continue;
                if (standardPropertiesToExclude.Contains(name))
                    continue;
                EmitProperty(attr, classContext);
            }
            foreach (var propertyName in classContext.descriptor.AttributePropertyNames)
            {
                var attributes = ComHelpers.GetProperty(classContext.comObject, propertyName);
                var attributesCount = Call.Количество(attributes);
                for (var i = 0; i < attributesCount; ++i)
                    EmitProperty(Call.Получить(attributes, i), classContext);
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
                var tableSectionsCount = Call.Количество(tableSections);
                for (var i = 0; i < tableSectionsCount; i++)
                {
                    var tableSection = Call.Получить(tableSections, i);
                    var tableSectionName = Call.Имя(tableSection);
                    var nestedClassContext = new ClassGenerationContext
                    {
                        configurationItemFullName = Call.ПолноеИмя(tableSection),
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
            var propertyName = Call.Имя(attribute);
            var propertyType = GetDotNetTypeOrNull(attribute,
                classContext.configurationItemFullName + "." + propertyName,
                classContext.generationContext);
            if (propertyType != null)
                classContext.target.Properties.Add(new PropertyModel
                {
                    Type = propertyType,
                    PropertyName = propertyName
                });
        }

        private string GetDotNetTypeOrNull(object metadataItem, string fullname, GenerationContext context)
        {
            var type = ComHelpers.GetProperty(metadataItem, "Тип");
            var typesObject = ComHelpers.Invoke(type, "Типы");
            var typesCount = Call.Количество(typesObject);
            if (typesCount == 0)
            {
                const string messageFormat = "no types for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, fullname));
            }
            object typeObject;
            string stringPresentation;
            if (typesCount > 1)
            {
                for (var i = 0; i < typesCount; i++)
                {
                    typeObject = Call.Получить(typesObject, i);
                    stringPresentation = globalContext.String(typeObject);
                    if (stringPresentation != "Число" &&
                        !simpleTypesMap.ContainsKey(stringPresentation))
                    {
                        var configurationItem = FindByType(typeObject);
                        if (configurationItem != null)
                            context.EnqueueIfNeeded(configurationItem);
                    }
                }
                return "object";
            }
            typeObject = Call.Получить(typesObject, 0);
            stringPresentation = globalContext.String(typeObject);
            string result;
            if (simpleTypesMap.TryGetValue(stringPresentation, out result))
                return result;
            if (stringPresentation == "Число")
            {
                var квалификаторыЧисла = ComHelpers.GetProperty(type, "КвалификаторыЧисла");
                var floatLength = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "РазрядностьДробнойЧасти"));
                var digits = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "Разрядность"));
                return floatLength == 0 ? (digits < 10 ? "int" : "long") : "decimal";
            }
            var propertyItem = FindByType(typeObject);
            if (propertyItem == null)
                return null;
            context.EnqueueIfNeeded(propertyItem);
            result = FormatClassName(propertyItem.Name);
            if (propertyItem.Name.Scope == ConfigurationScope.Перечисления)
                result = result + "?";
            return result;
        }

        private string FormatClassName(ConfigurationName name)
        {
            return GetNamespaceName(name.Scope) + "." + name.Name;
        }

        private void EmitConstants(GenerationContext context)
        {
            var constants = ComHelpers.GetProperty(metadata, "Константы");
            var constantsCount = Call.Количество(constants);
            for (var i = 0; i < constantsCount; i++)
            {
                var constant = Call.Получить(constants, i);
                var type = GetDotNetTypeOrNull(constant, Call.ПолноеИмя(constant), context);
                if (type == null)
                    continue;
                var configurationName = new ConfigurationName(ConfigurationScope.Константы,
                    Call.Имя(constant));
                var template = new ConstantFileTemplate
                {
                    Model = new ConstantFileModel
                    {
                        Type = type,
                        Name = configurationName.Name,
                        Synonym = GenerateHelpers.EscapeString(Call.Синоним(constant)),
                        Namespace = GetNamespaceName(configurationName.Scope)
                    }
                };
                context.Write(configurationName, template.TransformText());
            }
        }

        private void EmitEnum(ConfigurationItem item, GenerationContext context)
        {
            var model = new EnumFileModel
            {
                Name = item.Name.Name,
                Namespace = GetNamespaceName(item.Name.Scope)
            };
            var values = ComHelpers.GetProperty(item.ComObject, "ЗначенияПеречисления");
            var count = Call.Количество(values);
            for (var i = 0; i < count; i++)
            {
                var value = Call.Получить(values, i);
                model.Items.Add(new EnumItemModel
                {
                    Name = Call.Имя(value),
                    Synonym = GenerateHelpers.EscapeString(Call.Синоним(value))
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