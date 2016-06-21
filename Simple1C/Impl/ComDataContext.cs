using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Simple1C.Impl.Com;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Queriables;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal class ComDataContext : IDataContext
    {
        private readonly GlobalContext globalContext;
        private readonly EnumMapper enumMapper;
        private readonly ComObjectMapper comObjectMapper;
        private readonly IQueryProvider queryProvider;
        private readonly TypeMapper typeMapper;

        public ComDataContext(object globalContext, Assembly mappingsAssembly)
        {
            this.globalContext = new GlobalContext(globalContext);
            enumMapper = new EnumMapper(this.globalContext);
            typeMapper = new TypeMapper(mappingsAssembly);
            comObjectMapper = new ComObjectMapper(enumMapper, typeMapper);
            queryProvider = RelinqHelpers.CreateQueryProvider(typeMapper, Execute);
        }

        public Type GetTypeOrNull(string configurationName)
        {
            return typeMapper.GetTypeOrNull(configurationName);
        }

        public IQueryable<T> Select<T>(string sourceName = null)
        {
            return new RelinqQueryable<T>(queryProvider, sourceName);
        }

        public void Save<T>(T entity)
            where T : Abstract1CEntity
        {
            entity.Controller.MarkPotentiallyChangedAsChanged();
            Update(entity, null, new Stack<object>());
        }

        private void Update(Abstract1CEntity source, object comObject, Stack<object> pending)
        {
            var changeLog = source.Controller.Changed;
            if (changeLog == null)
                return;
            if (pending.Contains(source))
            {
                const string messageFormat = "cycle detected for entity type [{0}]: [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, source.GetType().Name,
                    pending
                        .Reverse()
                        .Select(x => x is Abstract1CEntity ? x.GetType().Name : x)
                        .JoinStrings("->")));
            }
            pending.Push(source);
            ConfigurationName? configurationName;
            if (comObject == null)
            {
                var comBasedEntityController = source.Controller as ComBasedEntityController;
                configurationName = ConfigurationName.Get(source.GetType());
                comObject = comBasedEntityController == null
                    ? CreateNewObject(configurationName.Value)
                    : ComHelpers.Invoke(comBasedEntityController.ComObject, "ПолучитьОбъект");
            }
            else
                configurationName = null;
            bool? newPostingValue = null;
            foreach (var p in changeLog)
            {
                var value = p.Value;
                if (p.Key == "Проведен" && configurationName.HasValue &&
                    configurationName.Value.Scope == ConfigurationScope.Документы)
                {
                    newPostingValue = (bool?) value;
                    continue;
                }
                bool needSet;
                pending.Push(p.Key);
                var valueToSet = ProcessProperty(p.Key, p.Value, comObject, pending, out needSet);
                pending.Pop();
                if (needSet)
                    ComHelpers.SetProperty(comObject, p.Key, valueToSet);
            }
            var oldRevision = source.Controller.Revision;
            if (configurationName.HasValue)
            {
                if (!newPostingValue.HasValue && configurationName.Value.Scope == ConfigurationScope.Документы)
                {
                    var oldPostingValue = Convert.ToBoolean(ComHelpers.GetProperty(comObject, "Проведен"));
                    if (oldPostingValue)
                    {
                        Write(comObject, configurationName.Value, false);
                        newPostingValue = true;
                    }
                }
                Write(comObject, configurationName.Value, newPostingValue);
                var comObjectReference = ComHelpers.GetProperty(comObject, "Ссылка");
                source.Controller = new ComBasedEntityController(comObjectReference, comObjectMapper);
                switch (configurationName.Value.Scope)
                {
                    case ConfigurationScope.Справочники:
                        UpdateIfExists(source, comObject, "Код");
                        break;
                    case ConfigurationScope.Документы:
                        UpdateIfExists(source, comObject, "Номер");
                        break;
                }
            }
            else
            {
                source.Controller = new ComBasedEntityController(comObject, comObjectMapper);
                UpdateIfExists(source, comObject, "НомерСтроки");
            }
            source.Controller.Revision = oldRevision + 1;
            pending.Pop();
        }

        private object ProcessProperty(string name, object value, object comObject, Stack<object> pending,
            out bool needSet)
        {
            var list = value as IList;
            if (list != null)
            {
                var tableSection = ComHelpers.GetProperty(comObject, name);
                ComHelpers.Invoke(tableSection, "Очистить");
                foreach (Abstract1CEntity item in (IList) value)
                    Update(item, ComHelpers.Invoke(tableSection, "Добавить"), pending);
                needSet = false;
                return null;
            }
            var syncList = value as SyncList;
            if (syncList != null)
            {
                UpdateSyncList(syncList, ComHelpers.GetProperty(comObject, name), pending);
                needSet = false;
                return null;
            }
            needSet = true;
            var abstractEntity = value as Abstract1CEntity;
            if (abstractEntity != null)
            {
                Update(abstractEntity, null, pending);
                return ((ComBasedEntityController) abstractEntity.Controller).ComObject;
            }
            if (value != null && value.GetType().IsEnum)
                return enumMapper.MapTo1C(value);
            return value;
        }

        private void Write(object comObject, ConfigurationName name, bool? posting)
        {
            var writeModeName = posting.HasValue
                ? (posting.Value ? "Posting" : "UndoPosting")
                : "Write";
            var writeMode = ComHelpers.GetProperty(globalContext.ComObject, "РежимЗаписиДокумента");
            var writeModeValue = ComHelpers.GetProperty(writeMode, writeModeName);
            try
            {
                ComHelpers.Invoke(comObject, "Write", writeModeValue);
            }
            catch (TargetInvocationException e)
            {
                const string messageFormat = "error writing document [{0}] with mode [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    name.Fullname, writeModeName), e.InnerException);
            }
        }

        private void UpdateSyncList(SyncList syncList, object tableSection, Stack<object> pending)
        {
            var original = syncList.original;
            if (original != null)
                for (var i = original.Count - 1; i >= 0; i--)
                {
                    var item = original[i];
                    if (syncList.current.IndexOf(item) < 0)
                    {
                        ComHelpers.Invoke(tableSection, "Удалить", i);
                        original.RemoveAt(i);
                    }
                }
            else
                original = new List<object>();
            for (var i = 0; i < syncList.current.Count; i++)
            {
                var item = (Abstract1CEntity) syncList.current[i];
                var originalIndex = original.IndexOf(item);
                if (originalIndex < 0)
                {
                    var newItemComObject = ComHelpers.Invoke(tableSection, "Вставить", i);
                    pending.Push(i);
                    Update(item, newItemComObject, pending);
                    pending.Pop();
                    original.Insert(i, null);
                }
                else
                {
                    if (originalIndex != i)
                    {
                        ComHelpers.Invoke(tableSection, "Сдвинуть", originalIndex, i - originalIndex);
                        original.RemoveAt(originalIndex);
                        original.Insert(i, null);
                    }
                    if (item.Controller.Changed != null)
                    {
                        pending.Push(i);
                        Update(item, ComHelpers.Invoke(tableSection, "Получить", i), pending);
                        pending.Pop();
                    }
                }
            }
        }

        private static void UpdateIfExists(Abstract1CEntity target, object source, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName);
            if (property != null)
            {
                target.Controller.TrackChanges = false;
                try
                {
                    property.SetValue(target, ComHelpers.GetProperty(source, propertyName));
                }
                finally
                {
                    target.Controller.TrackChanges = true;
                }
            }
        }

        private object CreateNewObject(ConfigurationName configurationName)
        {
            if (configurationName.Scope == ConfigurationScope.Справочники)
            {
                var catalogs = ComHelpers.GetProperty(globalContext.ComObject, "Справочники");
                var catalogManager = ComHelpers.GetProperty(catalogs, configurationName.Name);
                return ComHelpers.Invoke(catalogManager, "CreateItem");
            }
            if (configurationName.Scope == ConfigurationScope.Документы)
            {
                var documents = ComHelpers.GetProperty(globalContext.ComObject, "Документы");
                var documentManager = ComHelpers.GetProperty(documents, configurationName.Name);
                return ComHelpers.Invoke(documentManager, "CreateDocument");
            }
            const string messageFormat = "unexpected entityType [{0}]";
            throw new InvalidOperationException(string.Format(messageFormat, configurationName.Name));
        }

        private IEnumerable Execute(BuiltQuery builtQuery)
        {
            var queryText = builtQuery.QueryText;
            var parameters = builtQuery.Parameters
                .Select(x => new KeyValuePair<string, object>(x.Key, ConvertParameterValue(x)));
            var resultTable = globalContext.Execute(queryText, parameters);
            return resultTable.Select(x => comObjectMapper.MapFrom1C(x["Ссылка"],
                builtQuery.EntityType));
        }

        private object ConvertParameterValue(KeyValuePair<string, object> x)
        {
            return x.Value != null && x.Value.GetType().IsEnum
                ? enumMapper.MapTo1C(x.Value)
                : x.Value;
        }
    }
}