using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Queries;

namespace Simple1C.Impl.Sql
{
    internal class PostgreeSqlSchemaCreator
    {
        private readonly PostgreeSqlSchemaStore store;
        private readonly GlobalContext globalContext;

        public PostgreeSqlSchemaCreator(PostgreeSqlSchemaStore store, GlobalContext globalContext)
        {
            this.store = store;
            this.globalContext = globalContext;
        }

        public void Recreate()
        {
            object comTable = null;
            LogHelpers.LogWithTiming("loading COM schema info",
                () => comTable = ComHelpers.Invoke(globalContext.ComObject(),
                    "ПолучитьСтруктуруХраненияБазыДанных", Missing.Value, true));

            TableMapping[] tableMappings = null;
            LogHelpers.LogWithTiming("extracting table mappings from COM schema info",
                () => tableMappings = ExtractTableMappingsFromCom(comTable));

            LogHelpers.LogWithTiming("writing table mappings to PostgreeSQL",
                () => store.WriteTableMappings(tableMappings));

            EnumMapping[] enumMappings = null;
            LogHelpers.LogWithTiming("extracting enum mappings from COM ",
                () => enumMappings = ExtractEnumMappingsFromCom());

            LogHelpers.LogWithTiming("writing enum mappings to PostgreeSQL",
                () => store.WriteEnumMappings(enumMappings));
        }

        private EnumMapping[] ExtractEnumMappingsFromCom()
        {
            var enumsManager = ComHelpers.GetProperty(globalContext.ComObject(), "Перечисления");
            var enumsMeta = ComHelpers.GetProperty(globalContext.Metadata, "Перечисления");
            var enumsCount = Call.Количество(enumsMeta);
            var result = new List<EnumMapping>();
            for (var i = 0; i < enumsCount; i++)
            {
                var enumMeta = Call.Получить(enumsMeta, i);
                var enumName = Call.Имя(enumMeta);
                var enumManager = ComHelpers.GetProperty(enumsManager, enumName);
                var enumValuesMeta = ComHelpers.GetProperty(enumMeta, "ЗначенияПеречисления");
                var valuesCount = Call.Количество(enumValuesMeta);
                for (var j = 0; j < valuesCount; j++)
                {
                    var enumValueMeta = Call.Получить(enumValuesMeta, j);
                    var enumValueName = Call.Имя(enumValueMeta);
                    object enumValue = null;
                    try
                    {
                        enumValue = ComHelpers.GetProperty(enumManager, enumValueName);
                    }
                    catch
                    {
                        //сраный 1С куда-то проебывает (DISP_E_MEMBERNOTFOUND)
                        //УсловияПримененияТребованийЗаконодательства.ЕстьТранспортныеСредства
                        //и только это значения этого енума, остальные на месте
                    }
                    if (enumValue != null)
                    {
                        var order = Convert.ToInt32(ComHelpers.Invoke(enumManager, "Индекс", enumValue));
                        result.Add(new EnumMapping
                        {
                            enumName = enumName,
                            enumValueName = enumValueName,
                            orderIndex = order
                        });
                    }
                }
            }
            return result.ToArray();
        }

        private TableMapping[] ExtractTableMappingsFromCom(object comTable)
        {
            var tableRowsToProcess = new ValueTable(comTable)
                .OrderByDescending(delegate(ValueTableRow tableRow)
                {
                    var purpose = tableRow.GetString("Назначение");
                    if (purpose == "Основная")
                        return 10;
                    if (purpose == "ТабличнаяЧасть")
                        return 5;
                    return 0;
                })
                .ToArray();
            var tableMappingByQueryName = new Dictionary<string, TableMapping>();
            var result = new List<TableMapping>();
            for (var i = 0; i < tableRowsToProcess.Length; i++)
            {
                var tableRow = tableRowsToProcess[i];
                var queryTableName = tableRow.GetString("ИмяТаблицы");
                if (string.IsNullOrEmpty(queryTableName))
                    continue;
                var dbTableName = tableRow.GetString("ИмяТаблицыХранения");
                if (string.IsNullOrEmpty(dbTableName))
                    continue;
                var purpose = tableRow.GetString("Назначение");
                ConfigurationItemDescriptor descriptor;
                object comObject;
                TableType tableType;
                var additionalProperties = new List<PropertyMapping>();
                if (purpose == "Основная")
                {
                    var configurationName = ConfigurationName.ParseOrNull(queryTableName);
                    if (!configurationName.HasValue)
                        continue;
                    tableType = TableType.Main;
                    descriptor = MetadataHelpers.GetDescriptorOrNull(configurationName.Value.Scope);
                    if (descriptor == null)
                        comObject = null;
                    else
                    {
                        var configurationItem = globalContext.FindByName(configurationName.Value);
                        comObject = configurationItem.ComObject;
                    }
                }
                else if (purpose == "ТабличнаяЧасть")
                {
                    descriptor = MetadataHelpers.tableSectionDescriptor;
                    var fullname = TableSectionQueryNameToFullName(queryTableName);
                    comObject = ComHelpers.Invoke(globalContext.Metadata, "НайтиПоПолномуИмени", fullname);
                    if (comObject == null)
                        continue;
                    var mainQueryName = TableMapping.GetMainQueryNameByTableSectionQueryName(queryTableName);
                    TableMapping mainTableMapping;
                    if (!tableMappingByQueryName.TryGetValue(mainQueryName, out mainTableMapping))
                        continue;
                    if(!mainTableMapping.HasProperty("ОбластьДанныхОсновныеДанные"))
                        continue;
                    var refBinding = new SingleColumnBinding(GetTableSectionIdColumnNameByTableName(dbTableName), null);
                    additionalProperties.Add(new PropertyMapping("Ссылка",
                        PropertyKind.Single, refBinding, null));
                    var areaBinding = new SingleColumnBinding(
                        mainTableMapping.GetByPropertyName("ОбластьДанныхОсновныеДанные").SingleBinding.ColumnName,
                        null);
                    additionalProperties.Add(new PropertyMapping("ОбластьДанныхОсновныеДанные",
                        PropertyKind.Single, areaBinding, null));
                    tableType = TableType.TableSection;
                }
                else
                    continue;
                var propertyDescriptors = comObject == null
                    ? new Dictionary<string, PropertyDescriptor>()
                    : MetadataHelpers.GetAttributes(comObject, descriptor)
                        .ToDictionary(Call.Имя, GetPropertyDescriptor);
                var propertyMappings = new ValueTable(tableRow["Поля"])
                    .Select(x => new
                    {
                        queryName = x.GetString("ИмяПоля"),
                        dbName = x.GetString("ИмяПоляХранения")
                    })
                    .Where(x => !string.IsNullOrEmpty(x.queryName))
                    .Where(x => !string.IsNullOrEmpty(x.dbName))
                    .GroupBy(x => x.queryName, (x, y) => new {queryName = x, columns = y.Select(z => z.dbName).ToArray()})
                    .Select(x =>
                    {
                        var propertyDescriptor = propertyDescriptors.GetOrDefault(x.queryName);
                        if (propertyDescriptor == null || propertyDescriptor.propertyKind == PropertyKind.Single)
                        {
                            if (x.columns.Length != 1)
                            {
                                const string messageFormat = "unexpected db columns [{0}] for [{1}]";
                                throw new InvalidOperationException(string.Format(messageFormat,
                                    x.columns.JoinStrings(","), x.queryName));
                            }
                            var binding = new SingleColumnBinding(x.columns[0],
                                propertyDescriptor == null ? null : propertyDescriptor.types.Single());
                            return new PropertyMapping(x.queryName, PropertyKind.Single, binding, null);
                        }
                        if (propertyDescriptor.propertyKind == PropertyKind.UnionReferences)
                        {
                            var binding = new UnionReferencesBinding(
                                GetColumnBySuffixOrNull("_type", x.columns),
                                GetColumnBySuffixOrNull("_rtref", x.columns),
                                GetColumnBySuffixOrNull("_rrref", x.columns),
                                propertyDescriptor.types);
                            return new PropertyMapping(x.queryName, PropertyKind.Single, null, binding);
                        }
                        return new PropertyMapping(x.queryName, PropertyKind.Union, null, null);
                    })
                    .Union(additionalProperties)
                    .ToArray();
                var tableMapping = new TableMapping(queryTableName, dbTableName, tableType, propertyMappings);
                result.Add(tableMapping);
                tableMappingByQueryName.Add(queryTableName, tableMapping);
                if ((i + 1) % 50 == 0)
                    Console.Out.WriteLine("processed [{0}] from [{1}], {2}%",
                        i + 1, tableRowsToProcess.Length, (double)(i + 1) / tableRowsToProcess.Length* 100);
            }
            return result.ToArray();
        }

        private static string GetColumnBySuffixOrNull(string suffix, string[] candidates)
        {
            return candidates.SingleOrDefault(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        private class PropertyDescriptor
        {
            public PropertyKind propertyKind;
            public string[] types;
        }

        private PropertyDescriptor GetPropertyDescriptor(object a)
        {
            var type = ComHelpers.GetProperty(a, "Тип");
            var typesObject = ComHelpers.Invoke(type, "Типы");
            var typesCount = Call.Количество(typesObject);
            if (typesCount == 0)
                throw new InvalidOperationException("assertion failure");
            var types = new string[typesCount];
            for (var i = 0; i < typesCount; i++)
            {
                var typeObject = Call.Получить(typesObject, i);
                var stringPresentation = globalContext.String(typeObject);
                if (MetadataHelpers.simpleTypesMap.ContainsKey(stringPresentation))
                    continue;
                var propertyComObject = Call.НайтиПоТипу(globalContext.Metadata, typeObject);
                if (propertyComObject == null)
                    return null;
                types[i] = Call.ПолноеИмя(propertyComObject);
            }
            if (types.Length == 1)
                return types[0] == null
                    ? null
                    : new PropertyDescriptor
                    {
                        propertyKind = PropertyKind.Single,
                        types = types
                    };
            return types.Any(x => x == null)
                ? new PropertyDescriptor {propertyKind = PropertyKind.Union}
                : new PropertyDescriptor {propertyKind = PropertyKind.UnionReferences, types = types};
        }

        private static string TableSectionQueryNameToFullName(string s)
        {
            var lastDot = s.LastIndexOf('.');
            return s.Substring(0, lastDot) + ".ТабличнаяЧасть." + s.Substring(lastDot + 1);
        }
        
        private static string GetTableSectionIdColumnNameByTableName(string s)
        {
            var separator = s.LastIndexOf('_');
            return s.Substring(0, separator) + "_idrref";
        }
    }
}