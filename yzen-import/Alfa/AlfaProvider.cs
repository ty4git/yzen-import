using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YzenImport.Exceptions;
using YzenImport.Helpers;

namespace YzenImport.Alfa
{
    class AlfaProvider
    {
        private static readonly string MCC = "MCC";
        private readonly string _sourceFileName;
        private readonly Dictionary<int, string> _mccCodesCache;

        public AlfaProvider(string fileName, Dictionary<int, string> mccCodesCache)
        {
            _sourceFileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _mccCodesCache = mccCodesCache ?? throw new ArgumentNullException(nameof(mccCodesCache));
            Validate(_sourceFileName);
        }

        public async Task Process()
        {
            var content = await File.ReadAllLinesAsync(_sourceFileName);
            ValidateContent(content);

            var title = content.First();
            var titleItems = title.Split(Constants.Semicolon);
            var titleWithMcc = titleItems.Concat(new[] { "MCC", "Category" });
            var titleWithMccRaw = string.Join(Constants.Semicolon, titleWithMcc);

            var dataLinesWithMcc = await ExpandWithMccCodes(content.Skip(1).ToArray(), titleItems);
            var contentWithMcc = new[] { titleWithMccRaw }.Concat(dataLinesWithMcc).ToArray();

            var tartgetFileName = $"{Path.GetFileNameWithoutExtension(_sourceFileName)}-out" +
                $"{Path.GetExtension(_sourceFileName)}";
            await File.WriteAllLinesAsync(tartgetFileName, contentWithMcc);
        }

        private async Task<string[]> ExpandWithMccCodes(string[] dataLines, string[] titleItems)
        {
            var mccCodesCache = new ConcurrentDictionary<int, string>(_mccCodesCache);
            var dataLineTasks = dataLines
                .AsParallel()
                .AsOrdered()
                .Select(async line =>
                {
                    var lineItems = line.Split(Constants.Semicolon);
                    ValidateLine(lineItems, titleItems);

                    var words = lineItems.SelectMany(item => item.Split(" ", StringSplitOptions.RemoveEmptyEntries));
                    var mccCode = words.FirstOrDefault(word => word.StartsWith(MCC, StringComparison.InvariantCultureIgnoreCase));

                    var categoryName = "";
                    if (mccCode != null)
                    {
                        var mccCodeValueRaw = new string(mccCode.Skip(MCC.Count()).ToArray());
                        var mccCodeValue = Converters.ConvertMccCodeValue(mccCodeValueRaw);
                        var mccCodeName = await GetMccCodeName(mccCodeValue, mccCodesCache);
                        categoryName = $"{mccCodeName} ({mccCode})";
                    }

                    var extraMccItems = new[] { mccCode, categoryName };
                    var lineWithMcc = lineItems.Concat(extraMccItems).ToArray();
                    var lineWithMccRaw = string.Join(Constants.Semicolon, lineWithMcc);
                    return lineWithMccRaw;
                })
                .ToArray();

            var dataLinesWithMcc = await Task.WhenAll(dataLineTasks);

            foreach (var cachedMccCode in mccCodesCache)
            {
                _mccCodesCache.TryAdd(cachedMccCode.Key, cachedMccCode.Value);
            }

            return dataLinesWithMcc;
        }

        private void Validate(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (extension != ".csv")
            {
                throw new InvalidFileExtensionException();
            }
        }

        private void ValidateContent(string[] content)
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

        private async Task<string> GetMccCodeName(int mccCodeValue, ConcurrentDictionary<int, string> mccCodesCache)
        {
            if (mccCodesCache.TryGetValue(mccCodeValue, out var mccCodeName))
            {
                return mccCodeName;
            }

            var mccCodesUri = "https://mcc-codes.ru/code";
            var getMccCodeUri = new Uri($"{mccCodesUri}/{mccCodeValue}");
            var httpClient = new HttpClient();
            var mccCodeHtmlRaw = await httpClient.GetAsync(getMccCodeUri)
                .Result.Content.ReadAsStringAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(mccCodeHtmlRaw);
            var mccCodeNodes = htmlDocument.DocumentNode.Descendants("h1")
                .ToArray();

            if (mccCodeNodes == null || !mccCodeNodes.Any() || mccCodeNodes.Count() > 1)
            {
                throw new MccReferenceSiteDoesntHaveMccCodeException(mccCodeValue.ToString());
            }

            var mccCodeNode = mccCodeNodes.First();

            var mccCodeContent = mccCodeNode.InnerText.Split(":").Select(value => value.Trim()).ToArray();
            var valueIndex = 0;
            if (mccCodeContent.Count() != 2 && Converters.ConvertMccCodeValue(mccCodeContent[valueIndex]) != mccCodeValue)
            {
                throw new InvalidMccCodeFormatException(mccCodeContent[valueIndex]);
            }

            var nameIndex = 1;
            mccCodeName = mccCodeContent[nameIndex];

            mccCodesCache.TryAdd(mccCodeValue, mccCodeName);

            return mccCodeName;
        }
    }
}
