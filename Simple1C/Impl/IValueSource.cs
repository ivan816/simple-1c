using System;

namespace Simple1C.Impl
{
    public interface IValueSource
    {
        object GetBackingStorage();
        bool Writable { get; }
        bool TryLoadValue(string name, Type type, out object result);
    }
}