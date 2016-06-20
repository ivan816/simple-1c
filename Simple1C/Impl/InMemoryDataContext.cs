using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal class InMemoryDataContext: IDataContext
    {
        private readonly Dictionary<Type, List<object>> store = new Dictionary<Type, List<object>>();
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
                .Cast<T>()
                .AsQueryable();
        }

        public void Save<T>(T entity) where T : Abstract1CEntity
        {
            entity.Controller.MarkPotentiallyChangedAsChanged();
            Save((Abstract1CEntity) entity);
        }

        private void Save(Abstract1CEntity entity)
        {
            if (entity == null || entity.Controller.Changed == null)
                return;
            foreach (var change in entity.Controller.Changed)
            {
                var abstract1CEntity = change.Value as Abstract1CEntity;
                Save(abstract1CEntity);
            }
            var entityType = entity.GetType();
            var configurationName = ConfigurationName.Get(entityType);
            if (configurationName.Scope == ConfigurationScope.Справочники)
                GenerateProperty(entity, entityType, "Код");
            else if (configurationName.Scope == ConfigurationScope.Документы)
                GenerateProperty(entity, entityType, "Номер");
            Collection(entityType)
                .Add(entity);
        }

        private static void GenerateProperty(Abstract1CEntity entity, Type entityType, string property)
        {
            var codeProperty = entityType.GetProperty(property);
            if (codeProperty != null)
                codeProperty.SetMethod.Invoke(entity, new object[] {Guid.NewGuid().ToString()});
        }

        private List<object> Collection(Type type)
        {
            return store.GetOrAdd(type, t => new List<object>());
        }
    }
}