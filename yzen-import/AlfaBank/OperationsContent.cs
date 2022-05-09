using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace YzenImport.AlfaBank
{
    internal class OperationsContent<T> where T : Operation, new()
    {
        private readonly T[] _content;

        public static async Task<OperationsContent<T>> FromFile(string filename)
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                BadDataFound = null
            };
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("windows-1251");

            using var reader = new StreamReader(filename, encoding);
            using var csv = new CsvReader(reader, cfg);
            var rs = GetRecords(csv);
            var records = await rs.ToArrayAsync();
            var titles = csv.HeaderRecord.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            return new OperationsContent<T>(records);
        }

        private static IAsyncEnumerable<T> GetRecords(CsvReader csv)
        {
            IAsyncEnumerable<Operation> rs;
            switch (new T())
            {
                case ExtendedOperation _:
                    csv.Context.RegisterClassMap<ExtendedOperationMap>();
                    rs = csv.GetRecordsAsync<ExtendedOperation>();
                    break;
                case Operation _:
                    csv.Context.RegisterClassMap<OperationMap>();
                    rs = csv.GetRecordsAsync<Operation>();
                    break;
                default:
                    throw new InvalidOperationException();
            }
            return (IAsyncEnumerable<T>)rs;
        }

        public OperationsContent(T[] ops)
        {
            _content = ops;
        }

        public async Task ToFile(string filename)
        {
            using var writer = new StreamWriter(filename);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<ExtendedOperationMap>();
            await csv.WriteRecordsAsync(GetOperations());
        }

        public IEnumerable<T> GetOperations()
        {
            return _content;
        }
    }
}
