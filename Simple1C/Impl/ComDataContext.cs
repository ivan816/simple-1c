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
            var entitiesToSave = new List<Abstract1CEntity>();
            entity.Controller.PrepareToSave(entity, entitiesToSave);
            foreach (var e in entitiesToSave)
                Save(e, null, new Stack<object>());
        }

        private void Save(Abstract1CEntity source, object comObject, Stack<object> pending)
        {
            var changeLog = source.Controller.Changed;
            if (changeLog == null && source.Controller is ComBasedEntityController)
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
            if(changeLog != null)
                foreach (var p in changeLog)
                {
                    var value = p.Value;
                    if (p.Key == "Проведен" && configurationName.HasValue &&
                        configurationName.Value.Scope == ConfigurationScope.Документы)
                    {
                        newPostingValue = (bool?) value;
                        continue;
                    }
                    pending.Push(p.Key);
                    SaveProperty(p.Key, p.Value, comObject, pending);
                    pending.Pop();
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

        private void SaveProperty(string name, object value, object comObject, Stack<object> pending)
        {
            var list = value as IList;
            if (list != null)
            {
                var tableSection = ComHelpers.GetProperty(comObject, name);
                ComHelpers.Invoke(tableSection, "Очистить");
                foreach (Abstract1CEntity item in (IList) value)
                    Save(item, ComHelpers.Invoke(tableSection, "Добавить"), pending);
                return;
            }
            var syncList = value as SyncList;
            if (syncList != null)
            {
                var tableSection = ComHelpers.GetProperty(comObject, name);
                foreach (var cmd in syncList.commands)
                    switch (cmd.CommandType)
                    {
                        case SyncList.CommandType.Delete:
                            var deleteCommand = (SyncList.DeleteCommand) cmd;
                            ComHelpers.Invoke(tableSection, "Удалить", deleteCommand.index);
                            break;
                        case SyncList.CommandType.Insert:
                            var insertCommand = (SyncList.InsertCommand) cmd;
                            var newItemComObject = ComHelpers.Invoke(tableSection, "Вставить", insertCommand.index);
                            pending.Push(insertCommand.index);
                            Save(insertCommand.item, newItemComObject, pending);
                            pending.Pop();
                            break;
                        case SyncList.CommandType.Move:
                            var moveCommand = (SyncList.MoveCommand) cmd;
                            ComHelpers.Invoke(tableSection, "Сдвинуть", moveCommand.from, moveCommand.delta);
                            break;
                        case SyncList.CommandType.Update:
                            var updateCommand = (SyncList.UpdateCommand) cmd;
                            pending.Push(updateCommand.index);
                            Save(updateCommand.item, ComHelpers.Invoke(tableSection, "Получить", updateCommand.index),
                                pending);
                            pending.Pop();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                return;
            }
            object valueToSet;
            var abstractEntity = value as Abstract1CEntity;
            if (abstractEntity != null)
            {
                Save(abstractEntity, null, pending);
                valueToSet = ((ComBasedEntityController) abstractEntity.Controller).ComObject;
            }
            else if (value != null && value.GetType().IsEnum)
                valueToSet = enumMapper.MapTo1C(value);
            else
                valueToSet = value;
            ComHelpers.SetProperty(comObject, name, valueToSet);
        }

        private void Write(object comObject, ConfigurationName name, bool? posting)
        {
            var writeModeName = posting.HasValue ? (posting.Value ? "Posting" : "UndoPosting") : "Write";
            var writeMode = globalContext.РежимЗаписиДокумента();
            var writeModeValue = ComHelpers.GetProperty(writeMode, writeModeName);
            try
            {
                ComHelpers.Invoke(comObject, "Write", writeModeValue);
            }
            catch (TargetInvocationException e)
            {
                const string messageFormat = "error writing document [{0}] with mode [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, name.Fullname, writeModeName),
                    e.InnerException);
            }
        }

        private static void UpdateIfExists(Abstract1CEntity target, object source, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName);
            if (property == null)
                return;
            var oldTrackChanges = target.Controller.TrackChanges;
            target.Controller.TrackChanges = false;
            try
            {
                property.SetValue(target, ComHelpers.GetProperty(source, propertyName));
            }
            finally
            {
                target.Controller.TrackChanges = oldTrackChanges;
            }
        }

        private object CreateNewObject(ConfigurationName configurationName)
        {
            if (configurationName.Scope == ConfigurationScope.Справочники)
            {
                var catalogs = globalContext.Справочники();
                var catalogManager = ComHelpers.GetProperty(catalogs, configurationName.Name);
                return ComHelpers.Invoke(catalogManager, "CreateItem");
            }
            if (configurationName.Scope == ConfigurationScope.Документы)
            {
                var documents = globalContext.Документы();
                var documentManager = ComHelpers.GetProperty(documents, configurationName.Name);
                return ComHelpers.Invoke(documentManager, "CreateDocument");
            }
            const string messageFormat = "unexpected entityType [{0}]";
            throw new InvalidOperationException(string.Format(messageFormat, configurationName.Name));
        }

        private IEnumerable Execute(BuiltQuery builtQuery)
        {
            var queryText = builtQuery.QueryText;
            var parameters =
                builtQuery.Parameters.Select(x => new KeyValuePair<string, object>(x.Key, ConvertParameterValue(x)));
            var selection = globalContext.Execute(queryText, parameters).Select();
            while (selection.Next())
                yield return comObjectMapper.MapFrom1C(selection["Ссылка"], builtQuery.EntityType);
        }
        private object ConvertParameterValue(KeyValuePair<string, object> x)
        {
            return x.Value != null && x.Value.GetType().IsEnum ? enumMapper.MapTo1C(x.Value) : x.Value;
        }
    }
}