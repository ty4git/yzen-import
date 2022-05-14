using System;
using System.Diagnostics;
using System.Linq;
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

                    var alfa = new AlfaBankProvider(mccsCache);

                    var spendingsFiles = cmdParams.SpendingsDataFiles.ToArray();
                    var merged = await alfa.Merge(spendingsFiles);
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    await alfa.Process(merged);

                    stopWatch.Stop();
                    Console.WriteLine($"{nameof(Program)}.{nameof(Main)}: {stopWatch.Elapsed}");

                    await mccsCache.UpdateMccCodesFile();
                });
        }
    }
}
