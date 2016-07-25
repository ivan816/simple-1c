using System;
using System.Linq;

namespace Simple1C.Interface
{
    public interface IDataContext
    {
        Type GetTypeOrNull(string configurationName);
        IQueryable<T> Select<T>(string sourceName = null);
        void Save(object entity);
    }
}