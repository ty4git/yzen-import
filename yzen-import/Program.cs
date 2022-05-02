using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using YzenImport.AlfaBank;
using YzenImport.Exceptions;
using YzenImport.Helpers;

namespace YzenImport
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var result = await Parser.Default
                .ParseArguments<CmdParams>(args)
                .WithParsedAsync(async cmdParams =>
                {
                    var mccCodesCache = await GetMccCache();
                    var alfaProvider = new AlfaBankProvider(cmdParams.SpendingsDataFile, mccCodesCache);

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await alfaProvider.Process();

                    stopWatch.Stop();
                    Console.WriteLine($"{nameof(Program)}.{nameof(Main)}: {stopWatch.Elapsed}");

                    await UpdateMccCodesFile(mccCodesCache);
                });
        }

        private static async Task<Dictionary<int, string>> GetMccCache()
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

            return mccCodes;
        }

        private static async Task UpdateMccCodesFile(IReadOnlyDictionary<int, string> mccCodesCache)
        {
            var mccCodesFileName = Constants.MccsFileName;
            var mccCodesRaw = await File.ReadAllLinesAsync(mccCodesFileName);
            var titleRow = mccCodesRaw.First();

            var orderedMccCodeValues = mccCodesCache.Keys.OrderBy(key => key).ToArray();
            var orderedMccCodeRaws = orderedMccCodeValues
                .Select(mccCodeValue =>
                {
                    var mccCodeName = mccCodesCache[mccCodeValue];
                    var mccCodeRaw = string.Join(Constants.Semicolon, mccCodeValue, mccCodeName);
                    return mccCodeRaw;
                })
                .ToArray();

            await File.WriteAllLinesAsync(mccCodesFileName, new[] { titleRow }.Concat(orderedMccCodeRaws));
        }
    }
}
