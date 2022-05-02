using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YzenImport.Exceptions;
using YzenImport.Helpers;

namespace YzenImport.AlfaBank
{
    class AlfaBankProvider
    {
        private static readonly string MCC = "MCC";
        private readonly string _spendingsFileName;
        private readonly Dictionary<int, string> _mccStaticCache;

        public AlfaBankProvider(string spendingsFileName, Dictionary<int, string> mccCodesCache)
        {
            _spendingsFileName = spendingsFileName ?? throw new ArgumentNullException(nameof(spendingsFileName));
            _mccStaticCache = mccCodesCache ?? throw new ArgumentNullException(nameof(mccCodesCache));
            ValidateFileExtension(_spendingsFileName);
        }

        private class SpendingsContent
        {
            private readonly IEnumerable<string> _content;
            private string[] _titles;
            private readonly Spending[] _cnt;

            public static async Task<SpendingsContent> FromFile(string filename)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var encoding = Encoding.GetEncoding("windows-1251");
                var raw = await File.ReadAllLinesAsync(filename, encoding);
                var cnt = new SpendingsContent(raw);
                return cnt;
            }

            public static async Task<SpendingsContent> FromFile2(string filename)
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
                csv.Context.RegisterClassMap<SpendingMap>();
                var rs = csv.GetRecordsAsync<Spending>();
                var records = await rs.ToArrayAsync();
                return new SpendingsContent(records);
            }

            public SpendingsContent(string[] content)
                : this(content.AsEnumerable())
            {
            }

            public SpendingsContent(IEnumerable<string> content)
            {
                ValidateExistingOfContent(content);
                _content = content;
            }

            public SpendingsContent(Spending[] spendings)
            {
                _cnt = spendings;
            }

            public IEnumerable<string> AllRows => _content;

            private void ValidateExistingOfContent(IEnumerable<string> content)
            {
                if (content.Count() < 2)
                {
                    throw new InvalidFileContentException();
                }
            }

            public IEnumerable<string> GetExtendedTitles()
            {
                var titleItems = GetTitles();
                var extendedTitle = titleItems.Concat(new[] { "MCC", "Category" });
                return extendedTitle;
            }

            public string ExtendRow(string[] rowFields, int mcc, string category)
            {
                var newVs = new[] { mcc.ToString(), category };
                var newRow = rowFields.Concat(newVs);
                return string.Join(Constants.Semicolon, newRow);
            }

            public IEnumerable<string> GetTitles()
            {
                if (_titles != null)
                {
                    return _titles;
                }

                _titles = _content.First().Split(Constants.Semicolon, StringSplitOptions.RemoveEmptyEntries);
                return _titles;
            }

            public IEnumerable<string> GetDataRows()
            {
                return _content.Skip(1);
            }
        }

        private class MccExtender
        {
            private readonly AlfaBankProvider _provider;
            private readonly MccsDynamicCache _mccsCache;

            public MccExtender(AlfaBankProvider provider)
            {
                _provider = provider;
                _mccsCache = new MccsDynamicCache(provider._mccStaticCache);
            }

            public async Task<SpendingsContent> Extend(SpendingsContent spendings)
            {
                var rows = spendings.GetDataRows();

                var extendingTasks = rows
                    .AsParallel()
                    .AsOrdered()
                    .Select(async row => await ExtendRow(row, spendings))
                    .ToArray();

                var extendedRows = await Task.WhenAll(extendingTasks);

                foreach (var cachedMcc in _mccsCache.Copy())
                {
                    _provider._mccStaticCache.TryAdd(cachedMcc.Key, cachedMcc.Value);
                }

                var titles = spendings.GetExtendedTitles();
                var newCnt = new SpendingsContent(
                    new[] { string.Join(Constants.Semicolon, titles) }.Concat(extendedRows).ToArray());
                return newCnt;
            }

            private async Task<string> ExtendRow(string row, SpendingsContent spendings)
            {
                var rowFields = row.Split(Constants.Semicolon, StringSplitOptions.RemoveEmptyEntries);
                ValidateLine(rowFields, spendings.GetTitles());

                var mcc = ExtractMcc(rowFields);
                if (mcc is null)
                {
                    var invalidMcc = -1;
                    return spendings.ExtendRow(rowFields, invalidMcc, $"Warning: MCC not found.");
                }

                var mccDesc = await _mccsCache.Get(mcc.Value);
                var category = $"{mccDesc} (MCC: {mcc})";

                return spendings.ExtendRow(rowFields, mcc.Value, category);
            }

            private int? ExtractMcc(string[] rowFields)
            {
                var mccs = rowFields
                    .SelectMany(item => item.Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(word => word.StartsWith(MCC, StringComparison.OrdinalIgnoreCase))
                    .Select(possibleMcc => Converters.TryConvertMcc(string.Concat(possibleMcc.Skip(MCC.Length))))
                    .Where(mccInfo => mccInfo.Found)
                    .Select(mccInfo => mccInfo.Mcc)
                    .ToArray();

                if (mccs.Length == 0)
                {
                    return null;
                }

                if (mccs.Length > 1)
                {
                    throw new Exception($"There are 2 or more MCC ({mccs[0]}, {mccs[1]})"); //todo: print in out file or other file with errors
                }

                return mccs.Single();
            }

            private void ValidateLine(string[] dataItems, IEnumerable<string> titles)
            {
                if (titles.Count() != dataItems.Length)
                {
                    throw new InvalidDataLineException();
                }
            }
        }

        private class MccsDynamicCache
        {
            private readonly ConcurrentDictionary<int, string> _cache;
            private static HttpClient _httpClient;
            public MccsDynamicCache(Dictionary<int, string> cache)
            {
                _cache = new ConcurrentDictionary<int, string>(cache);
                _httpClient = new HttpClient();
            }

            public IDictionary<int, string> Copy()
            {
                return _cache.ToDictionary(x => x.Key, x => x.Value);
            }

            //todo: should return string?
            public async Task<string> Get(int mcc)
            {
                if (_cache.TryGetValue(mcc, out var mccDesc))
                {
                    return mccDesc;
                }

                var netMccDesc = await LoadByNet(mcc);
                _cache.TryAdd(mcc, netMccDesc);
                return netMccDesc;
            }

            private async Task<string> LoadByNet(int mcc)
            {
                Console.WriteLine($"{nameof(MccsDynamicCache)}.{nameof(LoadByNet)}()");

                var mccUriBase = "https://mcc-codes.ru/code";
                var mccUri = new Uri($"{mccUriBase}/{mcc}");
                var getMccResult = await _httpClient.GetAsync(mccUri);

                if (!getMccResult.IsSuccessStatusCode)
                {
                    throw new Exception($"MCC ({mcc}) not found at {mccUri}");
                }

                var mccHtmlPage = new HtmlDocument();
                mccHtmlPage.LoadHtml(await getMccResult.Content.ReadAsStringAsync());
                var mccCodeNodes = mccHtmlPage.DocumentNode.Descendants("h1").ToArray();

                if (mccCodeNodes is null || !mccCodeNodes.Any() || mccCodeNodes.Count() > 1)
                {
                    throw new MccReferenceSiteDoesntHaveMccCodeException(mcc.ToString());
                }

                var mccHtmlNode = mccCodeNodes.Single();

                var mccInfo = mccHtmlNode.InnerText.Split(":").Select(value => value.Trim()).ToArray();
                if (mccInfo.Length != 2)
                {
                    throw new InvalidMccCodeFormatException(mcc.ToString());
                }

                var (netMccRaw, netMccDesc) = (mccInfo[0], mccInfo[1]);
                var (found, netMcc) = Converters.TryConvertMcc(netMccRaw);
                if (netMcc != mcc)
                {
                    throw new InvalidMccCodeFormatException(netMcc.ToString());
                }

                return netMccDesc;
            }
        }

        public async Task Process()
        {
            Console.WriteLine($"{nameof(AlfaBankProvider)}.{nameof(Process)}"); //todo: logging

            var cnt = await SpendingsContent.FromFile(_spendingsFileName);
            var cnt2 = await SpendingsContent.FromFile2(_spendingsFileName);


            var extender = new MccExtender(this);
            var extended = await extender.Extend(cnt);

            var tartgetFileName =
                $"{Path.GetFileNameWithoutExtension(_spendingsFileName)}-out" +
                $"{Path.GetExtension(_spendingsFileName)}";

            await File.WriteAllLinesAsync(tartgetFileName, extended.AllRows);
        }

        private void ValidateFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (extension != ".csv")
            {
                throw new InvalidFileExtensionException();
            }
        }

        private void ValidateContentExists(string[] content)
        {
            if (content.Length < 2)
            {
                throw new InvalidFileContentException();
            }
        }

        private void ValidateLine(string[] lineItems, string[] titleItems)
        {
            if (titleItems.Length != lineItems.Length)
            {
                throw new InvalidDataLineException();
            }
        }
    }
}
