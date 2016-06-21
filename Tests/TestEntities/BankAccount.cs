namespace Simple1C.Tests.TestEntities
{
    public class BankAccount
    {
        public string Number { get; set; }
        public Bank Bank { get; set; }
        public string Name { get; set; }
        public string CurrencyCode { get; set; }
    }
}