using System;
using System.IO;
using System.Threading.Tasks;
using YzenImport.Exceptions;

namespace YzenImport.AlfaBank
{
    class AlfaBankProvider
    {
        private readonly string _spendingsFileName;
        private readonly MccsDynamicCache _mccsCache;

        public AlfaBankProvider(string spendingsFileName, MccsDynamicCache mccsCache)
        {
            _spendingsFileName = spendingsFileName ?? throw new ArgumentNullException(nameof(spendingsFileName));
            _mccsCache = mccsCache ?? throw new ArgumentNullException(nameof(mccsCache));
            ValidateFileExtension(_spendingsFileName);
        }

        public async Task Process()
        {
            Console.WriteLine($"{nameof(AlfaBankProvider)}.{nameof(Process)}"); //todo: logging

            var content = await OperationsContent<Operation>.FromFile(_spendingsFileName);
            var extender = new MccExtender(_mccsCache);
            var extended = await extender.Extend(content);

            var filename =
                $"{Path.GetFileNameWithoutExtension(_spendingsFileName)}-out" +
                $"{Path.GetExtension(_spendingsFileName)}";
            await extended.ToFile(filename);
        }

        private void ValidateFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (extension != ".csv")
            {
                throw new InvalidFileExtensionException();
            }
        }
    }
}
