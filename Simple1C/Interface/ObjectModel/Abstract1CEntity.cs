using Simple1C.Impl;

namespace Simple1C.Interface.ObjectModel
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