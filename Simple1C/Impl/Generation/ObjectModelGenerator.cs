using System;
using System.Collections;
using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation.Rendering;
using Simple1C.Interface;

namespace Simple1C.Impl.Generation
{
    internal class ObjectModelGenerator
    {
        private readonly GlobalContext globalContext;
        private readonly IEnumerable<string> itemNames;
        private readonly string namespaceRoot;
        private readonly string targetDirectory;

        public ObjectModelGenerator(object globalContext, IEnumerable<string> itemNames,
            string namespaceRoot, string targetDirectory)
        {
            this.globalContext = new GlobalContext(globalContext);
            this.itemNames = itemNames;
            this.namespaceRoot = namespaceRoot;
            this.targetDirectory = targetDirectory;
        }

        public IEnumerable<string> Generate()
        {
            var generationContext = new GenerationContext(targetDirectory);
            foreach (var itemName in itemNames)
            {
                var item = globalContext.FindByName(ConfigurationName.Parse(itemName));
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
                    case ConfigurationScope.ПланыВидовХарактеристик:
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

        private ConfigurationItem FindByType(object typeObject)
        {
            var comObject = Call.НайтиПоТипу(globalContext.Metadata, typeObject);
            var fullName = Call.ПолноеИмя(comObject);
            var name = ConfigurationName.ParseOrNull(fullName);
            return name.HasValue
                ? new ConfigurationItem(name.Value, comObject)
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
                descriptor = MetadataHelpers.GetDescriptor(item.Name.Scope),
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
            if (classContext.target.ConfigurationScope.HasValue)
            {
                var synonym = Call.Синоним(classContext.comObject);
                classContext.target.Synonym = GenerateHelpers.EscapeString(synonym);
                if (classContext.target.ConfigurationScope.Value != ConfigurationScope.РегистрыСведений)
                {
                    var presentation = Call.StringProp(classContext.comObject, "ObjectPresentation");
                    classContext.target.ObjectPresentation = GenerateHelpers.EscapeString(presentation);
                }
            }
            var name = classContext.configurationName;
            var attributes = MetadataHelpers.GetAttributes(classContext.comObject, classContext.descriptor);
            foreach (var attr in attributes)
            {
                var propertyName = Call.Имя(attr);
                var propertyTypeDescriptor = GetTypeDescriptor(attr,
                    classContext.configurationItemFullName + "." + propertyName,
                    classContext.generationContext);
                if (!propertyTypeDescriptor.HasValue)
                    continue;
                var propertyModel = new PropertyModel
                {
                    Type = propertyTypeDescriptor.Value.dotnetType,
                    PropertyName = propertyName,
                    MaxLength = propertyTypeDescriptor.Value.maxLength
                };
                classContext.target.Properties.Add(propertyModel);
            }
            if (name.HasValue && name.Value.HasReference)
                classContext.EmitProperty(new PropertyModel
                {
                    Type = "Guid?",
                    PropertyName = EntityHelpers.idPropertyName
                });
            if (classContext.descriptor.HasStandardTableSections)
                EmitTableSections(classContext, "СтандартныеТабличныеЧасти", MetadataHelpers.standardTableSectionDescriptor, false);
            if (classContext.descriptor.HasTableSections)
                EmitTableSections(classContext, "ТабличныеЧасти", MetadataHelpers.tableSectionDescriptor, true);
        }

        private void EmitTableSections(ClassGenerationContext classContext, string tableSectionsName,
            ConfigurationItemDescriptor descriptor, bool hasFullname)
        {
            var tableSections = ComHelpers.GetProperty(classContext.comObject, tableSectionsName);
            foreach (var tableSection in (IEnumerable) tableSections)
            {
                var tableSectionName = Call.Имя(tableSection);
                var nestedClassContext = new ClassGenerationContext
                {
                    configurationItemFullName = hasFullname ? Call.ПолноеИмя(tableSection) : Call.Имя(tableSection),
                    comObject = tableSection,
                    descriptor = descriptor,
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

        private TypeDescriptor? GetTypeDescriptor(object metadataItem, string fullname, GenerationContext context)
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
                    if (stringPresentation != "Число" && !MetadataHelpers.simpleTypesMap.ContainsKey(stringPresentation))
                    {
                        var configurationItem = FindByType(typeObject);
                        if (configurationItem != null)
                            context.EnqueueIfNeeded(configurationItem);
                    }
                }
                return new TypeDescriptor {dotnetType = "object"};
            }
            typeObject = Call.Получить(typesObject, 0);
            stringPresentation = globalContext.String(typeObject);
            string typeName;
            if (MetadataHelpers.simpleTypesMap.TryGetValue(stringPresentation, out typeName))
            {
                if (typeName == null)
                    return null;
                int? maxLength;
                if (typeName == "string")
                {
                    var квалификаторыСтроки = ComHelpers.GetProperty(type, "КвалификаторыСтроки");
                    maxLength = Call.IntProp(квалификаторыСтроки, "Длина");
                    if (maxLength == 0)
                        maxLength = null;
                }
                else
                    maxLength = null;
                return new TypeDescriptor {dotnetType = typeName, maxLength = maxLength};
            }
            if (stringPresentation == "Число")
            {
                var квалификаторыЧисла = ComHelpers.GetProperty(type, "КвалификаторыЧисла");
                var floatLength = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "РазрядностьДробнойЧасти"));
                var digits = Convert.ToInt32(ComHelpers.GetProperty(квалификаторыЧисла, "Разрядность"));
                return new TypeDescriptor
                {
                    dotnetType = floatLength == 0 ? (digits < 10 ? "int" : "long") : "decimal"
                };
            }
            var propertyItem = FindByType(typeObject);
            if (propertyItem == null)
                return null;
            context.EnqueueIfNeeded(propertyItem);
            typeName = FormatClassName(propertyItem.Name);
            if (propertyItem.Name.Scope == ConfigurationScope.Перечисления)
                typeName = typeName + "?";
            return new TypeDescriptor {dotnetType = typeName};
        }

        private string FormatClassName(ConfigurationName name)
        {
            return GetNamespaceName(name.Scope) + "." + name.Name;
        }

        private void EmitConstants(GenerationContext context)
        {
            var constants = ComHelpers.GetProperty(globalContext.Metadata, "Константы");
            var constantsCount = Call.Количество(constants);
            for (var i = 0; i < constantsCount; i++)
            {
                var constant = Call.Получить(constants, i);
                var typeDescriptor = GetTypeDescriptor(constant, Call.ПолноеИмя(constant), context);
                if (!typeDescriptor.HasValue)
                    continue;
                var configurationName = new ConfigurationName(ConfigurationScope.Константы,
                    Call.Имя(constant));
                var template = new ConstantFileTemplate
                {
                    Model = new ConstantFileModel
                    {
                        Type = typeDescriptor.Value.dotnetType,
                        Name = configurationName.Name,
                        Synonym = GenerateHelpers.EscapeString(Call.Синоним(constant)),
                        Namespace = GetNamespaceName(configurationName.Scope),
                        MaxLength = typeDescriptor.Value.maxLength
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

        private struct TypeDescriptor
        {
            public string dotnetType;
            public int? maxLength;
        }
    }
}