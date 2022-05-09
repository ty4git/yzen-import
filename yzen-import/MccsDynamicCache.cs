using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using YzenImport.Exceptions;
using YzenImport.Helpers;

namespace YzenImport
{
    internal class MccsDynamicCache
    {
        private static HttpClient _httpClient;
        private readonly ConcurrentDictionary<int, string> _cache;

        public static async Task<MccsDynamicCache> FromFile()
        {
            var mccFileName = Constants.MccsFileName;
            var mccCodesRaw = await File.ReadAllLinesAsync(mccFileName);

            var headerRow = 1;
            var mccCodes = mccCodesRaw.Skip(headerRow)
                .Select(mccRaw =>
                {
                    var mccInfo = mccRaw.Split(Constants.Semicolon);
                    if (mccInfo.Length == 0 || mccInfo.Count() > 2)
                    {
                        throw new InvalidMccCodesFileException(mccFileName, mccRaw);
                    }

                    var (found, mcc) = Converters.TryConvertMcc(mccInfo[0]);
                    if (!found)
                    {
                        throw new Exception($"MCC ({mcc}) is invalid format. Should be only digits.");
                    }

                    var mccDesc = mccInfo[1];
                    return (Value: mcc, Desc: mccDesc);
                })
                .ToDictionary(
                    mccCode => mccCode.Value,
                    mccCode => mccCode.Desc);

            return new MccsDynamicCache(mccCodes);
        }

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

        public async Task UpdateMccCodesFile()
        {
            var mccCodesFileName = Constants.MccsFileName;
            var mccCodesRaw = await File.ReadAllLinesAsync(mccCodesFileName);
            var titleRow = mccCodesRaw.First();

            var orderedMccs = _cache.Keys.OrderBy(key => key).ToArray();
            var orderedMccCodeRaws = orderedMccs
                .Select(mcc =>
                {
                    var mccCodeName = _cache[mcc];
                    var mccCodeRaw = string.Join(Constants.Semicolon, mcc, mccCodeName);
                    return mccCodeRaw;
                })
                .ToArray();

            await File.WriteAllLinesAsync(mccCodesFileName, new[] { titleRow }.Concat(orderedMccCodeRaws));
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
}
