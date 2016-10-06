using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Queries;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal class PostgreeSqlSchemaCreator
    {
        private readonly PostgreeSqlSchemaStore store;
        private readonly PostgreeSqlDatabase database;
        private readonly GlobalContext globalContext;

        public PostgreeSqlSchemaCreator(PostgreeSqlSchemaStore store,
            PostgreeSqlDatabase database,
            GlobalContext globalContext)
        {
            this.store = store;
            this.database = database;
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

            LogHelpers.LogWithTiming("creating helper functions", CreateHelperFunctions);
        }

        private void CreateHelperFunctions()
        {
            const string sql =
                @"CREATE OR REPLACE FUNCTION simple1c__to_guid(bytea) RETURNS varchar(36) LANGUAGE plpgsql IMMUTABLE LEAKPROOF STRICT AS $$
DECLARE
	guid_text varchar(50);
BEGIN
	guid_text := replace(cast($1 as varchar(50)), '\\x', '');
	guid_text := substring(guid_text from 25 for 8) || '-' ||
				substring(guid_text from 21 for 4) || '-' ||
				substring(guid_text from 17 for 4) || '-' ||
				substring(guid_text from 1 for 4) || '-' ||
              	substring(guid_text from 5 for 12);
	return guid_text;
END
$$;

CREATE OR REPLACE FUNCTION simple1c__date_from_guid(varchar(36)) RETURNS timestamp LANGUAGE plpgsql IMMUTABLE LEAKPROOF STRICT AS $$
DECLARE
	guid_text varchar(50);
	ticks bigint;
	seconds bigint;
BEGIN
	guid_text := replace(cast($1 as varchar(50)), '-', '');
	guid_text := substring(guid_text from 14 for 3) ||
							substring(guid_text from 9 for 4) ||
							substring(guid_text from 1 for 8);
	ticks := CAST(CAST(('x' || CAST(guid_text AS text)) AS bit(60)) AS BIGINT);
	if ticks < 60000000000000000 or ticks > 400000000000000000 then
		return null;
	end if;
	seconds := ticks / 10000000;
	return timestamp '1582-10-15 04:00:00' + seconds * interval '1 second' + interval '1 hour';
END
$$;";
            database.ExecuteNonQuery(sql);
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
                if (!string.IsNullOrEmpty(queryTableName))
                {
                    var tableMapping = queryTableName == "РегистрБухгалтерии.Хозрасчетный.Остатки"
                        ? CreateAccountingBalancesTableMapping(tableRow)
                        : CreateDefaultTableMapping(tableRow, tableMappingByQueryName);
                    if (tableMapping != null)
                    {
                        result.Add(tableMapping);
                        tableMappingByQueryName.Add(queryTableName, tableMapping);
                    }
                }
                if ((i + 1)%50 == 0)
                    Console.Out.WriteLine("processed [{0}] from [{1}], {2}%",
                        i + 1, tableRowsToProcess.Length, (double) (i + 1)/tableRowsToProcess.Length*100);
            }
            return result.ToArray();
        }

        private TableMapping CreateDefaultTableMapping(ValueTableRow tableRow,
            Dictionary<string, TableMapping> tableMappingByQueryName)
        {
            var queryTableName = tableRow.GetString("ИмяТаблицы");
            if (string.IsNullOrEmpty(queryTableName))
                return null;
            var dbTableName = tableRow.GetString("ИмяТаблицыХранения");
            if (string.IsNullOrEmpty(dbTableName))
                return null;
            var purpose = tableRow.GetString("Назначение");
            ConfigurationItemDescriptor descriptor;
            object comObject;
            TableType tableType;
            if (purpose == "Основная")
            {
                var configurationName = ConfigurationName.ParseOrNull(queryTableName);
                if (!configurationName.HasValue)
                    return null;
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
                    return null;
                tableType = TableType.TableSection;
            }
            else
                return null;
            var propertyDescriptors = comObject == null
                ? new Dictionary<string, string[]>()
                : MetadataHelpers.GetAttributes(comObject, descriptor)
                    .ToDictionary(Call.Имя, GetPropertyTypes);
            var propertyMappings = new ValueTable(tableRow["Поля"])
                .Select(x => new
                {
                    queryName = x.GetString("ИмяПоля"),
                    dbName = x.GetString("ИмяПоляХранения")
                })
                .Where(x => !string.IsNullOrEmpty(x.queryName))
                .Where(x => !string.IsNullOrEmpty(x.dbName))
                .GroupBy(x => x.queryName,
                    (x, y) => new
                    {
                        queryName = x,
                        columns = y.Select(z => z.dbName).ToArray()
                    }, StringComparer.OrdinalIgnoreCase)
                .Select(x =>
                {
                    var propertyTypes = propertyDescriptors.GetOrDefault(x.queryName);
                    if (propertyTypes == null || propertyTypes.Length == 1)
                    {
                        if (x.columns.Length != 1)
                            return null;
                        var nestedTableName = propertyTypes == null ? null : propertyTypes[0];
                        var singleLayout = new SingleLayout(x.columns[0], nestedTableName);
                        return new PropertyMapping(x.queryName, singleLayout, null);
                    }
                    var unionLayout = new UnionLayout(
                        GetColumnBySuffixOrNull("_type", x.columns),
                        GetColumnBySuffixOrNull("_rtref", x.columns),
                        GetColumnBySuffixOrNull("_rrref", x.columns),
                        propertyTypes);
                    return new PropertyMapping(x.queryName, null, unionLayout);
                })
                .NotNull()
                .ToList();
            if (tableType == TableType.TableSection)
            {
                if (!HasProperty(propertyMappings, PropertyNames.id))
                {
                    var refLayout = new SingleLayout(GetTableSectionIdColumnNameByTableName(dbTableName), null);
                    propertyMappings.Add(new PropertyMapping(PropertyNames.id, refLayout, null));
                }
                if (!HasProperty(propertyMappings, PropertyNames.area))
                {
                    var mainQueryName = TableMapping.GetMainQueryNameByTableSectionQueryName(queryTableName);
                    TableMapping mainTableMapping;
                    if (!tableMappingByQueryName.TryGetValue(mainQueryName, out mainTableMapping))
                        return null;
                    PropertyMapping mainAreaProperty;
                    if (!mainTableMapping.TryGetProperty(PropertyNames.area, out mainAreaProperty))
                        return null;
                    propertyMappings.Add(mainAreaProperty);
                }
            }
            return new TableMapping(queryTableName, dbTableName, tableType, propertyMappings.ToArray());
        }

        private static bool HasProperty(List<PropertyMapping> properties, string name)
        {
            foreach (var p in properties)
                if (p.PropertyName.EqualsIgnoringCase(name))
                    return true;
            return false;
        }

        private static TableMapping CreateAccountingBalancesTableMapping(ValueTableRow tableRow)
        {
            var queryTableName = tableRow.GetString("ИмяТаблицы");
            if (string.IsNullOrEmpty(queryTableName))
                return null;
            var dbTableName = tableRow.GetString("ИмяТаблицыХранения");
            if (string.IsNullOrEmpty(dbTableName))
                return null;
            var fieldsTable = new ValueTable(tableRow["Поля"]);
            var propertyMappings = new PropertyMapping[fieldsTable.Count];
            for (var i = 0; i < fieldsTable.Count; i++)
            {
                var fieldRow = fieldsTable[i];
                var queryName = fieldRow.GetString("ИмяПоля");
                var dbName = fieldRow.GetString("ИмяПоляХранения");
                if (string.IsNullOrWhiteSpace(dbName))
                    throw new InvalidOperationException("assertion failure");
                if (string.IsNullOrWhiteSpace(queryName))
                {
                    if (dbName == "_Period")
                        queryName = "Период";
                    else if (dbName == "_Splitter")
                        queryName = "Разделитель";
                    else
                    {
                        const string messageFormat = "unexpected empty query name for field, db name [{0}]";
                        throw new InvalidOperationException(string.Format(messageFormat, dbName));
                    }
                }
                string nestedTableName;
                if (queryName == "Счет")
                    nestedTableName = "ПланСчетов.Хозрасчетный";
                else if (queryName == "Организация")
                    nestedTableName = "Справочник.Организации";
                else if (queryName == "Валюта")
                    nestedTableName = "Справочник.Валюты";
                else if (queryName == "Подразделение")
                    nestedTableName = "Справочник.ПодразделенияОрганизаций";
                else
                    nestedTableName = null;
                if (dbName.ContainsIgnoringCase("Turnover"))
                {
                    queryName += "Оборот";
                    if (dbName.Contains("Dt"))
                        queryName += "Дт";
                    else if (dbName.Contains("Ct"))
                        queryName += "Кт";
                }
                var singleLayout = new SingleLayout(dbName, nestedTableName);
                propertyMappings[i] = new PropertyMapping(queryName, singleLayout, null);
            }
            return new TableMapping(queryTableName, dbTableName, TableType.Main, propertyMappings);
        }

        private static string GetColumnBySuffixOrNull(string suffix, string[] candidates)
        {
            return candidates.SingleOrDefault(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        private string[] GetPropertyTypes(object a)
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
                    return null;
                var propertyComObject = Call.НайтиПоТипу(globalContext.Metadata, typeObject);
                if (propertyComObject == null)
                    return null;
                types[i] = Call.ПолноеИмя(propertyComObject);
            }
            return types;
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