using LinqTo1C.Impl;

namespace LinqTo1C
{
    public class Store1CFactory
    {
        public IStore1C Create(object globalContext)
        {
            var globalContextWrap = new GlobalContext(globalContext);
            var enumMapper = new EnumMapper(globalContextWrap);
            return new Store1C(globalContextWrap, enumMapper, new ComObjectMapper(enumMapper));
        }
    }
}