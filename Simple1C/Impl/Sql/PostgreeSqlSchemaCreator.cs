using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Com;
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
            var enumerations = ComHelpers.GetProperty(globalContext.ComObject(), "Перечисления");
            var enumerationsMetadata = ComHelpers.GetProperty(globalContext.Metadata, "Перечисления");
            var count = Call.Количество(enumerationsMetadata);
            var result = new List<EnumMapping>();
            for (var i = 0; i < count; i++)
            {
                var enumMeta = Call.Получить(enumerationsMetadata, i);
                var enumName = Call.Имя(enumMeta);
                var enumManager = ComHelpers.GetProperty(enumerations, enumName);
                var enumValuesMeta = ComHelpers.GetProperty(enumMeta, "ЗначенияПеречисления");
                var valuesCount = Call.Количество(enumerationsMetadata);
                for (var j = 0; j < valuesCount; j++)
                {
                    var enumValueMeta = Call.Получить(enumValuesMeta, i);
                    var enumValueName = Call.Имя(enumValueMeta);
                    var enumValue = ComHelpers.GetProperty(enumManager, enumValueName);
                    var order = Convert.ToInt32(ComHelpers.Invoke(enumManager, "Индекс", enumValue));
                    result.Add(new EnumMapping
                    {
                        enumName = enumName,
                        enumValueName = enumValueName,
                        order = order
                    });
                }
            }
            return result.ToArray();
        }

        private TableMapping[] ExtractTableMappingsFromCom(object comTable)
        {
            var tableMappings = new ValueTable(comTable);
            var result = new TableMapping[tableMappings.Count];
            for (var i = 0; i < tableMappings.Count; i++)
            {
                var tableMapping = tableMappings[i];
                var queryTableName = tableMapping.GetString("ИмяТаблицы");
                if (string.IsNullOrEmpty(queryTableName))
                    continue;
                var attributes = GetAttributes(globalContext, queryTableName);
                var dbTableName = tableMapping.GetString("ИмяТаблицыХранения");
                if (string.IsNullOrEmpty(dbTableName))
                    continue;
                var colunMappings = new ValueTable(tableMapping["Поля"]);
                var propertyMappings = new PropertyMapping[colunMappings.Count];
                for (var j = 0; j < colunMappings.Count; j++)
                {
                    var columnMapping = colunMappings.Get(j);
                    var queryColumnName = columnMapping.GetString("ИмяПоля");
                    if (string.IsNullOrEmpty(queryColumnName))
                        continue;
                    var dbColumnName = columnMapping.GetString("ИмяПоляХранения");
                    if (string.IsNullOrEmpty(dbColumnName))
                        continue;
                    var attribute = attributes == null ? null : attributes.GetOrDefault(queryColumnName);
                    var typename = attribute == null ? null : attribute();
                    propertyMappings[j] = new PropertyMapping(queryColumnName, dbColumnName,
                        string.IsNullOrEmpty(typename) ? "" : " " + typename);
                }
                result[i] = new TableMapping(queryTableName, dbTableName, propertyMappings);
                if ((i + 1)%50 == 0)
                    Console.Out.WriteLine("processed [{0}] from [{1}], {2}%",
                        i + 1, tableMappings.Count, (double) (i + 1)/tableMappings.Count*100);
            }
            return result;
        }

        private static Dictionary<string, Func<string>> GetAttributes(GlobalContext globalContext, string fullname)
        {
            var configurationName = ConfigurationName.ParseOrNull(fullname);
            if (configurationName == null)
                return null;
            if (!configurationName.Value.HasReference)
                return null;
            var configurationItem = globalContext.FindByName(configurationName.Value);
            var descriptor = MetadataHelpers.GetDescriptor(configurationName.Value.Scope);
            var attributes = MetadataHelpers.GetAttributes(configurationItem.ComObject, descriptor);
            return attributes.ToDictionary(Call.Имя, delegate(object o)
            {
                Func<string> result = delegate
                {
                    var type = ComHelpers.GetProperty(o, "Тип");
                    var typesObject = ComHelpers.Invoke(type, "Типы");
                    var typesCount = Call.Количество(typesObject);
                    if (typesCount != 1)
                        return null;
                    var typeObject = Call.Получить(typesObject, 0);
                    var stringPresentation = globalContext.String(typeObject);
                    if (simpleTypesMap.ContainsKey(stringPresentation))
                        return null;
                    var comObject = Call.НайтиПоТипу(globalContext.Metadata, typeObject);
                    if (comObject == null)
                        return null;
                    return Call.ПолноеИмя(comObject);
                };
                return result;
            });
        }

        private static readonly Dictionary<string, string> simpleTypesMap = new Dictionary<string, string>
        {
            {"Строка", "string"},
            {"Булево", "bool"},
            {"Дата", "DateTime?"},
            {"Уникальный идентификатор", "Guid?"},
            {"Хранилище значения", null},
            {"Описание типов", "Type[]"}
        };
    }
}