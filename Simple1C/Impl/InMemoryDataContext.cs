using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal class InMemoryDataContext : IDataContext
    {
        private readonly Dictionary<Type, List<InMemoryEntity>> committed =
            new Dictionary<Type, List<InMemoryEntity>>();

        private readonly TypeMapper typeMapper;

        public InMemoryDataContext(Assembly mappingsAssembly)
        {
            typeMapper = new TypeMapper(mappingsAssembly);
        }

        public Type GetTypeOrNull(string configurationName)
        {
            return typeMapper.GetTypeOrNull(configurationName);
        }

        public IQueryable<T> Select<T>(string sourceName = null)
        {
            return Collection(typeof (T))
                .Select(x => (T) CreateEntity(typeof (T), x))
                .AsQueryable();
        }

        private static object CreateEntity(Type type, InMemoryEntity entity)
        {
            var result = (Abstract1CEntity) FormatterServices.GetUninitializedObject(type);
            result.Controller = new InMemoryEntityController(entity);
            return result;
        }

        public void Save<T>(T entity) where T : Abstract1CEntity
        {
            var entitiesToSave = new List<Abstract1CEntity>();
            entity.Controller.PrepareToSave(entity, entitiesToSave);
            foreach (var e in entitiesToSave)
                Save(e, false);
        }

        private InMemoryEntity Save(Abstract1CEntity entity, bool isTableSection)
        {
            if (entity == null)
                return null;
            var changed = entity.Controller.Changed;
            var inMemoryController = entity.Controller as InMemoryEntityController;
            if (changed != null)
            {
                var keys = changed.Keys.ToArray();
                foreach (var k in keys)
                {
                    var abstract1CEntity = changed[k] as Abstract1CEntity;
                    if (abstract1CEntity != null)
                    {
                        changed[k] = Save(abstract1CEntity, false);
                        continue;
                    }
                    var list = changed[k] as IList;
                    if (list != null)
                    {
                        var newList = GetList(inMemoryController, k);
                        ApplyList(newList, list);
                        changed[k] = newList;
                    }
                    var syncList = changed[k] as SyncList;
                    if (syncList != null)
                    {
                        var newList = GetList(inMemoryController, k);
                        ApplySyncList(newList, syncList);
                        changed[k] = newList;
                    }
                }
            }
            if (inMemoryController != null)
            {
                inMemoryController.Commit();
                return inMemoryController.CommittedData;
            }
            var result = changed ?? new Dictionary<string, object>();
            var inMemoryEntity = new InMemoryEntity(entity.GetType(), result);
            if (!isTableSection)
            {
                var configurationName = ConfigurationName.Get(entity.GetType());
                if (configurationName.Scope == ConfigurationScope.Справочники)
                    AssignNewGuid(entity, result, "Код");
                else if (configurationName.Scope == ConfigurationScope.Документы)
                    AssignNewGuid(entity, result, "Номер");
                Collection(entity.GetType()).Add(inMemoryEntity);
            }
            entity.Controller = new InMemoryEntityController(inMemoryEntity);
            entity.Controller.Revision++;
            return inMemoryEntity;
        }

        private static List<InMemoryEntity> GetList(InMemoryEntityController inMemoryController, string key)
        {
            var oldList = inMemoryController != null
                ? (List<InMemoryEntity>)inMemoryController.CommittedData.Properties.GetOrDefault(key)
                : null;
            return oldList ?? new List<InMemoryEntity>();
        }

        private void ApplySyncList(List<InMemoryEntity> target, SyncList syncList)
        {
            foreach (var cmd in syncList.commands)
            {
                switch (cmd.CommandType)
                {
                    case SyncList.CommandType.Delete:
                        var deleteCommand = (SyncList.DeleteCommand)cmd;
                        target.RemoveAt(deleteCommand.index);
                        break;
                    case SyncList.CommandType.Insert:
                        var insertCommand = (SyncList.InsertCommand) cmd;
                        target.Insert(insertCommand.index, Save(insertCommand.item, true));
                        break;
                    case SyncList.CommandType.Move:
                        var moveCommand = (SyncList.MoveCommand) cmd;
                        var item = target[moveCommand.from];
                        target.RemoveAt(moveCommand.from);
                        target.Insert(moveCommand.from + moveCommand.delta, item);
                        break;
                    case SyncList.CommandType.Update:
                        var updateCommand = (SyncList.UpdateCommand)cmd;
                        Save(updateCommand.item, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void ApplyList(List<InMemoryEntity> target, IList newList)
        {
            target.Clear();
            target.Capacity = newList.Count;
            foreach (var l in newList)
                target.Add(Save((Abstract1CEntity)l, true));
        }

        private static void AssignNewGuid(Abstract1CEntity target, Dictionary<string, object> committed, string property)
        {
            if (committed.ContainsKey(property))
                return;
            var codeProperty = target.GetType().GetProperty(property);
            if (codeProperty == null)
                return;
            var value = Guid.NewGuid().ToString();
            committed[property] = value;
            var oldTrackChanges = target.Controller.TrackChanges;
            target.Controller.TrackChanges = false;
            try
            {
                codeProperty.SetMethod.Invoke(target, new object[] {value});
            }
            finally
            {
                target.Controller.TrackChanges = oldTrackChanges;
            }
        }

        private List<InMemoryEntity> Collection(Type type)
        {
            return committed.GetOrAdd(type, t => new List<InMemoryEntity>());
        }

        private class InMemoryEntityController : DictionaryBasedEntityController
        {
            public InMemoryEntity CommittedData { get; private set; }

            public InMemoryEntityController(InMemoryEntity committedData)
            {
                CommittedData = committedData;
            }

            protected override bool TryLoadValue(string name, Type type, out object result)
            {
                if (base.TryLoadValue(name, type, out result))
                    return true;
                if (!CommittedData.Properties.TryGetValue(name, out result))
                    return false;
                result = Convert(type, result);
                return true;
            }

            private static object Convert(Type type, object value)
            {
                if (type == typeof(object))
                {
                    var entity = value as InMemoryEntity;
                    return entity != null
                           && typeof (Abstract1CEntity).IsAssignableFrom(entity.Type)
                        ? CreateEntity(entity.Type, entity)
                        : value;
                }
                if (typeof (IList).IsAssignableFrom(type))
                {
                    var oldList = (IList) value;
                    var itemType = type.GetGenericArguments()[0];
                    var newList = ListFactory.Create(itemType, null, oldList.Count);
                    foreach (InMemoryEntity l in oldList)
                        newList.Add(CreateEntity(itemType, l));
                    return newList;
                }
                return typeof (Abstract1CEntity).IsAssignableFrom(type)
                    ? CreateEntity(type, (InMemoryEntity) value)
                    : value;
            }

            public void Commit()
            {
                if (Changed != null)
                {
                    foreach (var p in Changed)
                        CommittedData.Properties[p.Key] = p.Value;
                    Revision++;
                    Changed = null;
                }
            }
        }

        private class InMemoryEntity
        {
            public InMemoryEntity(Type type, Dictionary<string, object> properties)
            {
                Type = type;
                Properties = properties;
            }

            public Type Type { get; private set; }
            public Dictionary<string, object> Properties { get; private set; }
        }
    }
}