using System;
using CsvHelper.Configuration;

namespace YzenImport.AlfaBank
{
    internal class Spending
    {
        public string BankAccountName { get; set; }
        public string BankAccountNumber { get; set; }
        public string Currency { get; set; }
        public string OperationDate { get; set; }
        public string OperationReference { get; set; }
        public string OperationDescription { get; set; }
        public string Income { get; set; }
        public string Outcome { get; set; }
    }

    internal class SpendingMap : ClassMap<Spending>
    {
        public SpendingMap()
        {
            Map(x => x.BankAccountName).Name("Тип счёта");
            Map(x => x.BankAccountNumber).Name("Номер счета");
            Map(x => x.Currency).Name("Валюта");
            Map(x => x.OperationDate).Name("Дата операции");
            Map(x => x.OperationReference).Name("Референс проводки");
            Map(x => x.OperationDescription).Name("Описание операции");
            Map(x => x.Income).Name("Приход");
            Map(x => x.Outcome).Name("Расход");
        }
    }
}
