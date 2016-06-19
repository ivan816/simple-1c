using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl;

namespace Simple1C.Tests.TestEntities
{
    internal class TestObjectsManager
    {
        private readonly GlobalContext globalContext;
        private readonly EnumConverter enumConverter;
        private readonly string organizationInn;

        public TestObjectsManager(GlobalContext globalContext, EnumConverter enumConverter, string organizationInn)
        {
            this.globalContext = globalContext;
            this.enumConverter = enumConverter;
            this.organizationInn = organizationInn;
        }

        public dynamic ComObject
        {
            get { return globalContext.ComObject; }
        }

        public object CreateCounterparty(Counterpart counterpart)
        {
            var item = ComObject.Справочники.Контрагенты.CreateItem();
            item.ЮридическоеФизическоеЛицо = enumConverter.Convert(counterpart.LegalForm);
            item.ИНН = counterpart.Inn;
            if (counterpart.LegalForm == LegalForm.Organization)
                item.КПП = counterpart.Kpp;
            item.Наименование = counterpart.Name;
            item.НаименованиеПолное = counterpart.Name;
            item.ГосударственныйОрган = false;
            item.Write();
            return item;
        }

        public object CreateBankAccount(object owner, BankAccount bankAccount)
        {
            var item = ComObject.Справочники.БанковскиеСчета.CreateItem();
            item.НомерСчета = bankAccount.Number;
            item.Банк = GetBankByBic(bankAccount.Bank.Bik);
            item.Наименование = bankAccount.Name ?? GenerateBankAccountName(item.Банк, bankAccount.Number);
            if (bankAccount.CurrencyCode != null)
                item.ВалютаДенежныхСредств = GetCurrencyByCode(bankAccount.CurrencyCode);
            item.Владелец = owner;
            item.Write();
            return item;
        }

        public object CreateCounterpartContract(object counterpartReference, CounterpartyContract contract)
        {
            var item = ComObject.Справочники.ДоговорыКонтрагентов.CreateItem();
            item.ВидДоговора = enumConverter.Convert(contract.Kind);
            item.Организация = GetOrganization();
            item.Владелец = counterpartReference;
            item.Наименование = contract.Name;
            item.Комментарий = string.Format("test {0}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            if (!string.IsNullOrEmpty(contract.CurrencyCode))
                item.ВалютаВзаиморасчетов = GetCurrencyByCode(contract.CurrencyCode);
            item.Write();
            return item;
        }

        private static string GenerateBankAccountName(dynamic bank, string number)
        {
            return string.Format("{0}, {1}", number, bank.Наименование);
        }

        private object GetCurrencyByCode(string currencyCode)
        {
            return GetCatalogItemByCode("Валюты", currencyCode);
        }

        private object GetBankByBic(string bik)
        {
            return GetCatalogItemByCode("Банки", bik);
        }

        private object GetCatalogItemByKeyOrNull(string catalogName, string keyName, string keyValue)
        {
            const string queryFormat = @"
                ВЫБРАТЬ
	                catalog.Ссылка КАК Ссылка
                ИЗ
	                Справочник.{0} КАК catalog
                ГДЕ
	                catalog.{1} = &key";
            return globalContext.Execute(string.Format(queryFormat, catalogName, keyName), new Dictionary<string, object> {{"key", keyValue}}).Select(x => x["Ссылка"]).FirstOrDefault();
        }

        private object GetOrganization()
        {
            return GetCatalogItemByKey("Организации", "ИНН", organizationInn);
        }

        private object GetCatalogItemByCode(string catalogName, string code)
        {
            return GetCatalogItemByKey(catalogName, "Код", code);
        }

        private object GetCatalogItemByKey(string catalogName, string keyName, string keyValue)
        {
            var result = GetCatalogItemByKeyOrNull(catalogName, keyName, keyValue);
            if (result == null)
            {
                const string messageFormat = "catalog [{0}] item with {1} [{2}] not found";
                throw new InvalidOperationException(string.Format(messageFormat, catalogName, keyName, keyValue));
            }
            return result;
        }
    }
}