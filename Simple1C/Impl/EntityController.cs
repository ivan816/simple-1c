using System;
using System.Collections;
using System.Collections.Generic;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    public abstract class EntityController
    {
        private Dictionary<string, ObservedValue> observedValues;
        public uint Revision { get; set; }

        //грязный хак, подумать, как избавитсья
        public object ComObject { get; protected set; }
        public bool TrackChanges { get; set; }

        protected EntityController()
        {
            Revision = 1;
            TrackChanges = true;
        }

        public T GetValue<T>(ref Requisite<T> requisite, string name)
        {
            if (Revision > requisite.revision)
            {
                T result;
                if (requisite.revision == 0)
                {
                    object resultObject;
                    if (!TryLoadValue(name, typeof (T), out resultObject))
                        resultObject = default(T);
                    result = (T) resultObject;
                    if (result == null && typeof (IList).IsAssignableFrom(typeof (T)))
                    {
                        var listItemType = typeof (T).GetGenericArguments()[0];
                        result = (T) ListFactory.Create(listItemType, null, 1);
                    }
                    requisite.value = result;
                }
                else
                    result = requisite.value;
                requisite.revision = Revision;
                var needTrack = typeof (Abstract1CEntity).IsAssignableFrom(typeof (T)) ||
                                typeof (IList).IsAssignableFrom(typeof (T));
                if (needTrack)
                {
                    if (observedValues == null)
                        observedValues = new Dictionary<string, ObservedValue>();
                    var list = result as IList;
                    observedValues[name] = new ObservedValue
                    {
                        value = result,
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
            requisite.revision = Revision;
            if (TrackChanges)
                MarkAsChanged(name, value);
        }

        internal void MarkPotentiallyChangedAsChanged()
        {
            if (observedValues == null)
                return;
            foreach (var item in observedValues)
            {
                if (Changed != null && Changed.ContainsKey(item.Key))
                    continue;
                var value = item.Value;
                var entity = value.value as Abstract1CEntity;
                if (entity != null)
                {
                    entity.Controller.MarkPotentiallyChangedAsChanged();
                    if (entity.Controller.Changed != null)
                        MarkAsChanged(item.Key, item.Value.value);
                    continue;
                }
                var list = value.value as IList;
                if (list != null)
                {
                    foreach (Abstract1CEntity e in list)
                        e.Controller.MarkPotentiallyChangedAsChanged();
                    MarkAsChanged(item.Key, new SyncList
                    {
                        current = list,
                        original = value.originalList
                    });
                }
            }
            observedValues = null;
        }

        protected void MarkAsChanged(string name, object value)
        {
            if (Changed == null)
                Changed = new Dictionary<string, object>();
            Changed[name] = value;
        }

        protected abstract bool TryLoadValue(string name, Type type, out object result);
        protected internal Dictionary<string, object> Changed { get; protected set; }
    }
}