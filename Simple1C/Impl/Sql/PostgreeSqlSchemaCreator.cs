using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                () => comTable = ComHelpers.Invoke(globalContext.ComObject(), "ПолучитьСтруктуруХраненияБазыДанных"));

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
                dbTableName = PatchDbTableName(dbTableName);
                var purpose = tableRow.GetString("Назначение");
                ConfigurationItemDescriptor descriptor;
                object comObject;
                var propertyMappings = new List<PropertyMapping>();
                TableType tableType;
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
                    propertyMappings.Add(new PropertyMapping("Ссылка",
                        GetTableSectionIdColumnNameByTableName(dbTableName), null));
                    propertyMappings.Add(new PropertyMapping("ОбластьДанныхОсновныеДанные",
                        mainTableMapping.GetByPropertyName("ОбластьДанныхОсновныеДанные").ColumnName, null));
                    tableType = TableType.TableSection;
                }
                else
                    continue;
                var propertyTypes = comObject == null
                    ? new Dictionary<string, string>()
                    : GetPropertyTypes(comObject, descriptor);
                var columnRows = new ValueTable(tableRow["Поля"]);
                foreach (var m in columnRows)
                {
                    var queryColumnName = m.GetString("ИмяПоля");
                    var dbColumnName = m.GetString("ИмяПоляХранения");
                    if (string.IsNullOrEmpty(queryColumnName) || string.IsNullOrEmpty(dbColumnName))
                        continue;
                    var typeName = propertyTypes.GetOrDefault(queryColumnName);
                    dbColumnName = PatchColumnName(dbColumnName, typeName);
                    var propertyMapping = new PropertyMapping(queryColumnName, dbColumnName, typeName);
                    propertyMappings.Add(propertyMapping);
                }
                if (descriptor != null && descriptor.HasTableSections)
                {
                    var tableSections = ComHelpers.GetProperty(comObject, "ТабличныеЧасти");
                    foreach (var tableSection in (IEnumerable) tableSections)
                    {
                        var tableFullname = Call.ПолноеИмя(tableSection);
                        var tableQueryTable = TableSectionFullNameToQueryName(tableFullname);
                        var propertyMapping = new PropertyMapping(Call.Имя(tableSection), null, tableQueryTable);
                        propertyMappings.Add(propertyMapping);
                    }
                }
                var tableMapping = new TableMapping(queryTableName, dbTableName, tableType, propertyMappings.ToArray());
                result.Add(tableMapping);
                tableMappingByQueryName.Add(queryTableName, tableMapping);
                if ((i + 1) % 50 == 0)
                    Console.Out.WriteLine("processed [{0}] from [{1}], {2}%",
                        i + 1, tableRowsToProcess.Length, (double)(i + 1) / tableRowsToProcess.Length* 100);
            }
            return result.ToArray();
        }

        private static string PatchColumnName(string fieldName, string typeName)
        {
            if (fieldName == "ID")
                return "_idrref";
            var b = new StringBuilder(fieldName);
            b[0] = char.ToLower(b[0]);
            b.Insert(0, '_');
            if (!string.IsNullOrEmpty(typeName))
                b.Append("rref");
            return b.ToString();
        }

        private static string PatchDbTableName(string dbTableName)
        {
            var b = new StringBuilder(dbTableName);
            b[0] = char.ToLower(b[0]);
            b.Insert(0, '_');
            b.Replace('.', '_');
            return b.ToString();
        }

        private Dictionary<string, string> GetPropertyTypes(object comObject, ConfigurationItemDescriptor descriptor)
        {
            var attributes = MetadataHelpers.GetAttributes(comObject, descriptor);
            var result = new Dictionary<string, string>();
            foreach (var a in attributes)
            {
                var type = ComHelpers.GetProperty(a, "Тип");
                var typesObject = ComHelpers.Invoke(type, "Типы");
                var typesCount = Call.Количество(typesObject);
                if (typesCount != 1)
                    continue;
                var typeObject = Call.Получить(typesObject, 0);
                var stringPresentation = globalContext.String(typeObject);
                if (MetadataHelpers.simpleTypesMap.ContainsKey(stringPresentation))
                    continue;
                var propertyComObject = Call.НайтиПоТипу(globalContext.Metadata, typeObject);
                if (propertyComObject == null)
                    continue;
                result.Add(Call.Имя(a), Call.ПолноеИмя(propertyComObject));
            }
            return result;
        }

        private static string TableSectionFullNameToQueryName(string s)
        {
            var lastDot = s.LastIndexOf('.');
            var lastPrevDot = s.LastIndexOf('.', lastDot - 1);
            return s.Substring(0, lastPrevDot) + '.' + s.Substring(lastDot + 1);
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