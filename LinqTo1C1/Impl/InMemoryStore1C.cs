using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqTo1C.Impl
{
    public class InMemoryStore1C : IStore1C
    {
        private readonly List<object> items = new List<object>();

        public IQueryable<T> Select<T>(string sourceName)
        {
            return items.OfType<T>().ToList().AsQueryable();
        }

        public void Save<T>(T entity) where T : Abstract1CEntity
        {
            throw new NotImplementedException();
        }
    }
}