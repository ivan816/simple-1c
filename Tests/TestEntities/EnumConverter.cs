using System;
using System.ComponentModel;
using Simple1C.Impl;
using Simple1C.Impl.Com;

namespace Simple1C.Tests.TestEntities
{
    internal class EnumConverter
    {
        private readonly GlobalContext globalContext;

        public EnumConverter(GlobalContext globalContext)
        {
            this.globalContext = globalContext;
        }

        public object ConvertTo1C<T>(T enumValue, string enumName)
            where T: struct
        {
            dynamic self = this;
            return self.Convert(enumValue);
        }

        public object Convert(LegalForm legalForm)
        {
            switch (legalForm)
            {
                case LegalForm.Ip:
                    return EnumValue("ЮридическоеФизическоеЛицо", "ФизическоеЛицо");
                case LegalForm.Organization:
                    return EnumValue("ЮридическоеФизическоеЛицо", "ЮридическоеЛицо");
                default:
                    throw new InvalidEnumArgumentException(string.Format("unexpected value [{0}]", legalForm));
            }
        }
        
        public object Convert(NdsRate ndsRate)
        {
            switch (ndsRate)
            {
                case NdsRate.NoNds:
                    return EnumValue("СтавкиНДС", "БезНДС");
                case NdsRate.Nds10:
                    return EnumValue("СтавкиНДС", "НДС10");
                case NdsRate.Nds18:
                    return EnumValue("СтавкиНДС", "НДС18");
                case NdsRate.Nds20:
                    return EnumValue("СтавкиНДС", "НДС20");
                case NdsRate.Nds0:
                    return EnumValue("СтавкиНДС", "НДС0");
                case NdsRate.Nds10110:
                    return EnumValue("СтавкиНДС", "НДС10_110");
                case NdsRate.Nds18118:
                    return EnumValue("СтавкиНДС", "НДС18_118");
                case NdsRate.Nds20120:
                    return EnumValue("СтавкиНДС", "НДС20_120");
                default:
                    throw new ArgumentOutOfRangeException("ndsRate", ndsRate, null);
            }
        }

        public object Convert(IncomingOperationKind incomingOperationKind)
        {
            switch (incomingOperationKind)
            {
                case IncomingOperationKind.Goods:
                    return EnumValue("ВидыОперацийПоступлениеТоваровУслуг", "Товары");
                case IncomingOperationKind.Services:
                    return EnumValue("ВидыОперацийПоступлениеТоваровУслуг", "Услуги");
                case IncomingOperationKind.BuyingCommission:
                    return EnumValue("ВидыОперацийПоступлениеТоваровУслуг", "ПокупкаКомиссия");
                default:
                    throw new ArgumentOutOfRangeException("incomingOperationKind", incomingOperationKind, null);
            }
        }

        public object Convert(AdvanceWay advanceWay)
        {
            switch (advanceWay)
            {
                case AdvanceWay.Automatically:
                    return EnumValue("СпособыЗачетаАвансов", "Автоматически");
                case AdvanceWay.ByDocument:
                    return EnumValue("СпособыЗачетаАвансов", "ПоДокументу");
                case AdvanceWay.DontTakeIntoAccount:
                    return EnumValue("СпособыЗачетаАвансов", "НеЗачитывать");
                default:
                    throw new ArgumentOutOfRangeException("advanceWay", advanceWay, null);
            }
        }

        public object Convert(CounterpartContractKind value)
        {
            switch (value)
            {
                case CounterpartContractKind.Outgoing:
                    return EnumValue("ВидыДоговоровКонтрагентов", "СПоставщиком");
                case CounterpartContractKind.Incoming:
                    return EnumValue("ВидыДоговоровКонтрагентов", "СПокупателем");
                case CounterpartContractKind.Others:
                    return EnumValue("ВидыДоговоровКонтрагентов", "Прочее");
                case CounterpartContractKind.OutgoingWithComitent:
                    return EnumValue("ВидыДоговоровКонтрагентов", "СКомитентомНаЗакупку");
                case CounterpartContractKind.OutgoingWithAgency:
                    return EnumValue("ВидыДоговоровКонтрагентов", "СКомиссионеромНаЗакупку");
                case CounterpartContractKind.IncomingWithComitent:
                    return EnumValue("ВидыДоговоровКонтрагентов", "СКомитентом");
                case CounterpartContractKind.IncomingWithAgency:
                    return EnumValue("ВидыДоговоровКонтрагентов", "СКомиссионером");
                default:
                    throw new ArgumentOutOfRangeException("value", value, null);
            }
        }

        private dynamic ComObject()
        {
            return globalContext.ComObject();
        }

        private dynamic EnumValue(string enumName, string enumValue)
        {
            var enumsObject = ComObject().Перечисления;
            var enumObject = ComHelpers.GetProperty(enumsObject, enumName);
            return ComHelpers.GetProperty(enumObject, enumValue);
        }
    }
}