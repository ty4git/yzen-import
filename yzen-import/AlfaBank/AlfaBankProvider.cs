using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YzenImport.AlfaBank
{
    class AlfaBankProvider
    {
        private readonly MccsDynamicCache _mccsCache;

        public AlfaBankProvider(MccsDynamicCache mccsCache)
        {
            _mccsCache = mccsCache ?? throw new ArgumentNullException(nameof(mccsCache));
        }

        public async Task Process(string filename)
        {
            Console.WriteLine($"{nameof(AlfaBankProvider)}.{nameof(Process)}"); //todo: logging

            var content = await OperationsContent<Operation>.FromFile(filename);
            var extender = new MccExtender(_mccsCache);
            var extended = await extender.Extend(content);

            var outFile =
                $"{Path.GetFileNameWithoutExtension(filename)}.out.{DateTime.Now.ToString("HH-MM-ss")}.csv";
            await extended.ToFile(outFile);
        }

        public async Task<string> Merge(string[] filenames)
        {
            if (filenames is null || filenames.Length == 0)
            {
                throw new ArgumentNullException(nameof(filenames));
            }

            await ValidateHeaders(filenames);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding("windows-1251");

            var firstFile = filenames[0];
            var dir = Path.GetDirectoryName(firstFile);
            var outfile = Path.Combine(dir,
                $"{Path.GetFileNameWithoutExtension(firstFile)}.merged.{DateTime.Now.ToString("HH-mm-ss")}.csv");
            var copyHeader = true;
            foreach (var filename in filenames)
            {
                var lines = await File.ReadAllLinesAsync(filename, encoding);
                var outLines = copyHeader ? lines : lines.Skip(1);
                copyHeader = false;
                await File.AppendAllLinesAsync(outfile, outLines, encoding);
            }
            return outfile;
        }

        private async Task ValidateHeaders(string[] filenames)
        {
            var firstHeader = new string[] { };
            foreach (var filename in filenames)
            {
                var header = await OperationsContent<Operation>.GetHeader(filename);
                firstHeader = firstHeader.Length == 0 ? header : firstHeader;
                if (!Enumerable.SequenceEqual(firstHeader, header))
                {
                    throw new Exception($"Headers of files ({filenames[0]}, {filename}) are different");
                }
            }
        }
    }
}
