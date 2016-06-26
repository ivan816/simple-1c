using System;

namespace Simple1C.Impl
{
    public interface IValueSource
    {
        object GetBackingStorage();
        bool TryLoadValue(string name, Type type, out object result);
    }
}