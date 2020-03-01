using System;

namespace YzenImport.Exceptions
{
    class InvalidMccCodesFileException : Exception
    {
        public InvalidMccCodesFileException(string fileName)
            : base(@$"File with the MCC codes ""{fileName}"" is incorrect.")
        {
        }
    }
}
