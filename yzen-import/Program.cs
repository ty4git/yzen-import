using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;
using YzenImport.AlfaBank;

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
                    var mccsCache = await MccsDynamicCache.FromFile();
                    var alfaProvider = new AlfaBankProvider(cmdParams.SpendingsDataFile, mccsCache);

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await alfaProvider.Process();

                    stopWatch.Stop();
                    Console.WriteLine($"{nameof(Program)}.{nameof(Main)}: {stopWatch.Elapsed}");

                    await mccsCache.UpdateMccCodesFile();
                });
        }
    }
}
