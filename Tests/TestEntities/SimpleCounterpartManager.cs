using Simple1C.Impl;

namespace Simple1C.Tests.TestEntities
{
    internal class SimpleCounterpartManager
    {
        private readonly GlobalContext globalContext;

        public SimpleCounterpartManager(GlobalContext globalContext)
        {
            this.globalContext = globalContext;
        }

        public dynamic ComObject
        {
            get { return globalContext.ComObject; }
        }

        public void Create(Counterpart counterpart)
        {
            var item = ComObject.—правочники. онтрагенты.CreateItem();
            var legalFormEnum = ComObject.ѕеречислени€.ёридическое‘изическоеЋицо;
            item.ёридическое‘изическоеЋицо = counterpart.LegalForm == LegalForm.Ip
                ? legalFormEnum.‘изическоеЋицо
                : legalFormEnum.ёридическоеЋицо;
            item.»ЌЌ = counterpart.Inn;
            if (counterpart.LegalForm == LegalForm.Organization)
                item. ѕѕ = counterpart.Kpp;
            item.Ќаименование = counterpart.Name;
            item.Ќаименованиеѕолное = counterpart.Name;
            item.√осударственныйќрган = false;
            item.Write();
        }
    }
}