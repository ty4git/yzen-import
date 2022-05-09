using CsvHelper.Configuration;

namespace YzenImport.AlfaBank
{
    internal class Operation
    {
        public string BankAccountName { get; set; }
        public string BankAccountNumber { get; set; }
        public string Currency { get; set; }
        public string Date { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public string Income { get; set; }
        public string Outcome { get; set; }
    }

    internal class OperationMap : ClassMap<Operation>
    {
        public OperationMap()
        {
            Map(x => x.BankAccountName).Name("Тип счёта").Index(0);
            Map(x => x.BankAccountNumber).Name("Номер счета");
            Map(x => x.Currency).Name("Валюта");
            Map(x => x.Date).Name("Дата операции");
            Map(x => x.Reference).Name("Референс проводки");
            Map(x => x.Description).Name("Описание операции");
            Map(x => x.Income).Name("Приход");
            Map(x => x.Outcome).Name("Расход");
        }
    }

    internal class ExtendedOperation : Operation
    {
        public ExtendedOperation()
        {
        }
        public ExtendedOperation(Operation op)
        {
            BankAccountName = op.BankAccountName;
            BankAccountNumber = op.BankAccountNumber;
            Currency = op.Currency;
            Date = op.Date;
            Reference = op.Reference;
            Description = op.Description;
            Income = op.Income;
            Outcome = op.Outcome;
        }

        public string MCC { get; set; }
        public string Category { get; set; }
    }

    internal class ExtendedOperationMap : ClassMap<ExtendedOperation>
    {
        public ExtendedOperationMap()
        {
            MemberMaps.AddMembers(new OperationMap());

            Map(x => x.MCC).Name("MCC");
            Map(x => x.Category).Name("Category");
        }
    }
}
