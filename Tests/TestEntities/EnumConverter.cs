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
            return globalContext.ComObject;
        }

        private dynamic EnumValue(string enumName, string enumValue)
        {
            var enumsObject = ComObject().Перечисления;
            var enumObject = ComHelpers.GetProperty(enumsObject, enumName);
            return ComHelpers.GetProperty(enumObject, enumValue);
        }
    }
}