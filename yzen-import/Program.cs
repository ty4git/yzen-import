using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YzenImport.Alfa;
using YzenImport.Exceptions;
using YzenImport.Helpers;

namespace YzenImport
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var parameters = ParseParams(args);
            var sourceParameter = parameters.First(param => param.Name == Constants.ParameterNames.Source);
            var mccCodesCache = await GetMccCodesCache();
            var alfaProvider = new AlfaProvider(sourceParameter.Value, mccCodesCache);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await alfaProvider.Process();

            stopWatch.Stop();
            Console.WriteLine(stopWatch.Elapsed);

            await UpdateMccCodesFile(mccCodesCache);
        }

        private static (string Name, string Value)[] ParseParams(string[] paramsRaw)
        {
            var paramPrefix = Constants.ParameterNames.Prefix;
            var sourceParamName = Constants.ParameterNames.Source;

            var sourceParam = paramsRaw
                .SkipWhile(arg => !arg.StartsWith($"{paramPrefix}{sourceParamName}"))
                .Take(2)
                .ToArray();

            if (!sourceParam.Any())
            {
                throw new InvalidParameterException(sourceParamName);
            }

            var paramValue = 1;
            return new[] { (Name: sourceParamName, Value: sourceParam[paramValue]) };
        }

        private static async Task<Dictionary<int, string>> GetMccCodesCache()
        {
            var mccCodesFileName = Constants.MccCodesFileName;
            var mccCodesRaw = await File.ReadAllLinesAsync(mccCodesFileName);

            var headerRow = 1;
            var mccCodes = mccCodesRaw.Skip(headerRow)
                .Select(mccCodeRaw =>
                {
                    var mccCodeItems = mccCodeRaw.Split(Constants.Semicolon);
                    if (mccCodeItems.Count() > 2)
                    {
                        throw new InvalidMccCodesFileException(mccCodesFileName);
                    }

                    var mccCodeValue = Converters.ConvertMccCodeValue(mccCodeItems[0]);
                    var mccCodeName = mccCodeItems[1];
                    return (Value: mccCodeValue, Name: mccCodeName);
                })
                .ToDictionary(
                    mccCode => mccCode.Value,
                    mccCode => mccCode.Name);

            return mccCodes;
        }

        private static async Task UpdateMccCodesFile(IReadOnlyDictionary<int, string> mccCodesCache)
        {
            var mccCodesFileName = Constants.MccCodesFileName;
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
