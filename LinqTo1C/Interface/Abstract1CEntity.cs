using LinqTo1C.Impl;

namespace LinqTo1C.Interface
{
    public abstract class Abstract1CEntity
    {
        public EntityController Controller { get; set; }

        protected Abstract1CEntity()
        {
            Controller = new DictionaryBasedEntityController();
        }
    }
}