using System;
using System.Diagnostics;

namespace Generator
{
    public class EntryPoint
    {
        private static readonly string[] itemNames =
        {
            "Справочник.Банки",
            "Справочник.Валюты",
            "Справочник.БанковскиеСчета",
            "Справочник.ДоговорыКонтрагентов",
            "Справочник.КлассификаторБанковРФ",
            "Справочник.Контрагенты",
            "Справочник.Организации",
            "Справочник.ПодразделенияОрганизаций",
            "Справочник.ФизическиеЛица",
            "РегистрСведений.ОтветственныеЛицаОрганизаций",
            "Документ.ПоступлениеТоваровУслуг",
            "Документ.ПоступлениеНаРасчетныйСчет",
            "Документ.СписаниеСРасчетногоСчета"
        };

        private const string namespaceRoot = "Knopka.Application._1C.Mapper.Model";

        private const string fileFullPath =
            @"..\Application\1C\Mapper\Model\auto-generated.cs";

        public static void Main(string[] args)
        {
            var s = Stopwatch.StartNew();
            var generator = new LinqTo1C.Impl.Generation.Generator(null,
                itemNames, namespaceRoot, fileFullPath);
            generator.Generate();
            s.Stop();
            Console.Out.WriteLine("generation done, took {0} millis", s.ElapsedMilliseconds);
        }
    }
}