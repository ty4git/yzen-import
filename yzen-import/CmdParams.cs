using System.Collections.Generic;
using CommandLine;

namespace YzenImport
{
    internal class CmdParams
    {
        [Option('s', "spendings-data-file",
            Required = true,
            HelpText = "The source file that contains financial data with spendings.")]
        public IEnumerable<string> SpendingsDataFiles { get; private set; }
    }
}
