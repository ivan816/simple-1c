using System;
using System.Collections;
using System.Collections.Generic;
using LinqTo1C.Interface;

namespace LinqTo1C.Impl
{
    public abstract class EntityController
    {
        private Dictionary<string, TrackedValue> trackedValues;
        public uint Revision { get; set; }

        protected EntityController()
        {
            Revision = 1;
        }

        public T GetValue<T>(ref Requisite<T> requisite, string name)
        {
            if (Revision > requisite.revision)
            {
                T result;
                if (requisite.revision == 0)
                {
                    result = (T) GetValue(name, typeof (T));
                    requisite.value = result;
                }
                else
                    result = requisite.value;
                requisite.revision = Revision;
                var needTrack = typeof (Abstract1CEntity).IsAssignableFrom(typeof (T)) ||
                                typeof (IList).IsAssignableFrom(typeof (T));
                if (needTrack)
                {
                    if (trackedValues == null)
                        trackedValues = new Dictionary<string, TrackedValue>();
                    var list = result as IList;
                    trackedValues[name] = new TrackedValue
                    {
                        observedValue = result,
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
            if (name != "Код" && name != "Номер" && name != "НомерСтроки")
                MarkAsChanged(name, value);
        }

        public void MarkPotentiallyChangedAsChanged()
        {
            if (trackedValues == null)
                return;
            foreach (var p in trackedValues)
            {
                if (Changed != null && Changed.ContainsKey(p.Key))
                    continue;
                var value = p.Value;
                var entity = value.observedValue as Abstract1CEntity;
                if (entity != null)
                {
                    entity.Controller.MarkPotentiallyChangedAsChanged();
                    if (entity.Controller.Changed != null)
                        MarkAsChanged(p.Key, p.Value.observedValue);
                    continue;
                }
                var list = value.observedValue as IList;
                if (list != null)
                {
                    foreach (Abstract1CEntity item in list)
                        item.Controller.MarkPotentiallyChangedAsChanged();
                    MarkAsChanged(p.Key, new SyncList
                    {
                        current = list,
                        original = value.originalList
                    });
                }
            }
            trackedValues = null;
        }

        protected void MarkAsChanged(string name, object value)
        {
            if (Changed == null)
                Changed = new Dictionary<string, object>();
            Changed[name] = value;
        }

        protected abstract object GetValue(string name, Type type);
        public Dictionary<string, object> Changed { get; private set; }
    }
}