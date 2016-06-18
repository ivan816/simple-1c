using System.Linq;
using LinqTo1C.Interface;

namespace LinqTo1C
{
    public interface IStore1C
    {
        IQueryable<T> Select<T>(string sourceName = null);
        void Save<T>(T entity) where T : Abstract1CEntity;
    }
}