using System;

namespace YzenImport.Exceptions
{
    class InvalidMccCodesFileException : Exception
    {
        public InvalidMccCodesFileException(string fileName, string mcc)
            : base(@$"MCC code file (""{fileName}"") has invalid data ({mcc})")
        {
        }
    }
}
