namespace LinqTo1C.Impl
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