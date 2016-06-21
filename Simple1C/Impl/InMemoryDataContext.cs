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
        private readonly Dictionary<Type, List<Dictionary<string, object>>> committed =
            new Dictionary<Type, List<Dictionary<string, object>>>();

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

        private static object CreateEntity(Type type, Dictionary<string, object> controller)
        {
            var result = (Abstract1CEntity) FormatterServices.GetUninitializedObject(type);
            result.Controller = new InMemoryEntityController(controller);
            return result;
        }

        public void Save<T>(T entity) where T : Abstract1CEntity
        {
            entity.Controller.MarkPotentiallyChangedAsChanged();
            Save(entity, false);
        }

        private Dictionary<string, object> Save(Abstract1CEntity entity, bool isTableSection)
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
                        changed[k] = ConvertList(inMemoryController, k, list);
                    var syncList = changed[k] as SyncList;
                    if (syncList != null)
                        changed[k] = ConvertList(inMemoryController, k, syncList.current);
                }
            }
            if (inMemoryController != null)
            {
                inMemoryController.Commit();
                return inMemoryController.CommittedData;
            }
            var result = changed ?? new Dictionary<string, object>();
            if (!isTableSection)
            {
                var configurationName = ConfigurationName.Get(entity.GetType());
                if (configurationName.Scope == ConfigurationScope.Справочники)
                    AssignNewGuid(entity, result, "Код");
                else if (configurationName.Scope == ConfigurationScope.Документы)
                    AssignNewGuid(entity, result, "Номер");
                Collection(entity.GetType()).Add(result);
            }
            entity.Controller = new InMemoryEntityController(result);
            entity.Controller.Revision++;
            return result;
        }

        private IList ConvertList(InMemoryEntityController inMemoryController, string key, IList newList)
        {
            var oldList = inMemoryController != null
                ? (List<Dictionary<string, object>>) inMemoryController.CommittedData.GetOrDefault(key)
                : null;
            oldList = oldList ?? new List<Dictionary<string, object>>();
            oldList.Clear();
            oldList.Capacity = newList.Count;
            foreach (var l in newList)
                oldList.Add(Save((Abstract1CEntity) l, true));
            return oldList;
        }

        private static void AssignNewGuid(Abstract1CEntity target, Dictionary<string, object> committed, string property)
        {
            if (committed.ContainsKey(property))
                return;
            var codeProperty = target.GetType().GetProperty(property);
            if (codeProperty != null)
            {
                var value = Guid.NewGuid().ToString();
                codeProperty.SetMethod.Invoke(target, new object[] {value});
                committed[property] = value;
            }
        }

        private List<Dictionary<string, object>> Collection(Type type)
        {
            return committed.GetOrAdd(type, t => new List<Dictionary<string, object>>());
        }

        private class InMemoryEntityController : DictionaryBasedEntityController
        {
            public Dictionary<string, object> CommittedData { get; private set; }

            public InMemoryEntityController(Dictionary<string, object> committedData)
            {
                CommittedData = committedData;
            }

            protected override object GetValue(string name, Type type)
            {
                var result = base.GetValue(name, type);
                if (result != null)
                    return result;
                result = CommittedData.GetOrDefault(name);
                if (result == null)
                    return null;
                if (typeof (IList).IsAssignableFrom(type))
                {
                    var oldList = (IList) result;
                    var itemType = type.GetGenericArguments()[0];
                    var newList = ListFactory.Create(itemType, null, oldList.Count);
                    foreach (Dictionary<string, object> l in oldList)
                        newList.Add(CreateEntity(itemType, l));
                    return newList;
                }
                return typeof (Abstract1CEntity).IsAssignableFrom(type)
                    ? CreateEntity(type, (Dictionary<string, object>) result)
                    : result;
            }

            public void Commit()
            {
                if (Changed != null)
                {
                    foreach (var p in Changed)
                        CommittedData[p.Key] = p.Value;
                    Revision++;
                    Changed = null;
                }
            }
        }
    }
}