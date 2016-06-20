using System;
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
            Save((Abstract1CEntity) entity);
        }

        private Dictionary<string, object> Save(Abstract1CEntity entity)
        {
            if (entity == null)
                return null;
            var changed = entity.Controller.Changed;
            if (changed != null)
            {
                var keys = changed.Keys.ToArray();
                foreach (var k in keys)
                {
                    var abstract1CEntity = changed[k] as Abstract1CEntity;
                    if (abstract1CEntity != null)
                        changed[k] = Save(abstract1CEntity);
                }
            }
            var inMemoryController = entity.Controller as InMemoryEntityController;
            if (inMemoryController != null)
            {
                inMemoryController.Commit();
                return inMemoryController.CommittedData;
            }
            var configurationName = ConfigurationName.Get(entity.GetType());
            var result = changed ?? new Dictionary<string, object>();
            if (configurationName.Scope == ConfigurationScope.Справочники)
                AssignNewGuid(entity, result, "Код");
            else if (configurationName.Scope == ConfigurationScope.Документы)
                AssignNewGuid(entity, result, "Номер");
            Collection(entity.GetType()).Add(result);
            entity.Controller = new InMemoryEntityController(result);
            return result;
        }

        private static void AssignNewGuid(Abstract1CEntity target, Dictionary<string, object> committed, string property)
        {
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
                var result = base.GetValue(name, type) ?? CommittedData.GetOrDefault(name);
                if (result == null)
                    return null;
                return typeof (Abstract1CEntity).IsAssignableFrom(type)
                    ? CreateEntity(type, (Dictionary<string, object>) result)
                    : result;
            }

            public void Commit()
            {
                foreach (var p in Changed)
                    CommittedData[p.Key] = p.Value;
                Changed = null;
            }
        }
    }
}