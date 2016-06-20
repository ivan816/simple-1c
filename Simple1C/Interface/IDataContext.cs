using System;
using System.Linq;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Interface
{
    public interface IDataContext
    {
        Type GetTypeOrNull(string configurationName);
        IQueryable<T> Select<T>(string sourceName = null);
        void Save<T>(T entity) where T : Abstract1CEntity;
    }
}