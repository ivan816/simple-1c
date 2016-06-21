using System;

namespace Simple1C.Tests.TestEntities
{
    public class AccountingDocument
    {
        public Counterpart Counterpart { get; set; }
        public CounterpartyContract CounterpartContract { get; set; }
        public bool IsCreatedByEmployee { get; set; }
        public bool SumIncludesNds { get; set; }
        public DateTime Date { get; set; }
        public string Number { get; set; }
        public string Comment { get; set; }
        public IncomingOperationKind OperationKind { get; set; }
        public NomenclatureItem[] Items { get; set; }
    }
}