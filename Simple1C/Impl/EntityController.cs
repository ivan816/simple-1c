using System.Collections;
using System.Collections.Generic;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    public class EntityController
    {
        private Dictionary<string, ObservedValue> observedValues;
        private uint revision;
        internal bool TrackChanges { get; set; }
        internal Dictionary<string, object> Changed { get; private set; }
        public IValueSource ValueSource { get; private set; }

        public EntityController(IValueSource valueSource)
        {
            revision = 1;
            TrackChanges = true;
            ValueSource = valueSource;
        }

        public T GetValue<T>(ref Requisite<T> requisite, string name)
        {
            if (revision > requisite.revision)
            {
                if (requisite.revision == 0)
                {
                    object resultObject;
                    if (ValueSource == null || !ValueSource.TryLoadValue(name, typeof (T), out resultObject))
                        resultObject = default(T);
                    if (resultObject == null && typeof (IList).IsAssignableFrom(typeof (T)))
                    {
                        var listItemType = typeof (T).GetGenericArguments()[0];
                        resultObject = (T) ListFactory.Create(listItemType, null, 1);
                    }
                    requisite.value = (T) resultObject;
                }
                requisite.revision = revision;
                var needTrack = typeof(Abstract1CEntity).IsAssignableFrom(typeof(T)) ||
                                (typeof(T).IsGenericType &&
                                 typeof(T).GetGenericTypeDefinition() == typeof(List<>) &&
                                 typeof(Abstract1CEntity).IsAssignableFrom(typeof(T).GetGenericArguments()[0]));
                if (needTrack)
                {
                    if (observedValues == null)
                        observedValues = new Dictionary<string, ObservedValue>();
                    var list = requisite.value as IList;
                    observedValues[name] = new ObservedValue
                    {
                        value = requisite.value,
                        originalList = list == null
                            ? null
                            : ListFactory.Create(typeof (T).GetGenericArguments()[0], list, 0)
                    };
                }
            }
            return requisite.value;
        }

        public void SetValue<T>(ref Requisite<T> requisite, string name, object value)
        {
            requisite.value = (T) value;
            requisite.revision = revision;
            if (TrackChanges)
                MarkAsChanged(name, value);
        }

        internal void PrepareToSave(Abstract1CEntity owner, List<Abstract1CEntity> entitiesToSave)
        {
            if (observedValues != null)
            {
                foreach (var item in observedValues)
                {
                    if (Changed != null && Changed.ContainsKey(item.Key))
                        continue;
                    var value = item.Value;
                    var entity = value.value as Abstract1CEntity;
                    if (entity != null)
                    {
                        entity.Controller.PrepareToSave(entity, entitiesToSave);
                        continue;
                    }
                    var list = value.value as IList;
                    if (list != null)
                    {
                        foreach (Abstract1CEntity e in list)
                            e.Controller.PrepareToSave(e, entitiesToSave);
                        var syncList = SyncList.Compare(value.originalList, list);
                        if (syncList.Commands.Count > 0)
                            MarkAsChanged(item.Key, syncList);
                    }
                }
                observedValues = null;
            }
            if (!EntityHelpers.IsTableSection(owner.GetType()) && IsDirty)
                entitiesToSave.Add(owner);
        }

        internal bool IsNew
        {
            get { return ValueSource == null; }
        }

        internal bool IsDirty
        {
            get { return IsNew || Changed != null; }
        }

        internal void ResetDirty(IValueSource newValueSource)
        {
            ValueSource = newValueSource;
            revision++;
            Changed = null;
        }

        private void MarkAsChanged(string name, object value)
        {
            if (Changed == null)
                Changed = new Dictionary<string, object>();
            Changed[name] = value;
        }
    }
}