using System.Linq;
using LinqTo1C.Impl;

namespace LinqTo1C
{
    public interface IStore1C
    {
        IQueryable<T> Select<T>(string sourceName);
        void Save<T>(T entity) where T : Abstract1CEntity;
    }
}