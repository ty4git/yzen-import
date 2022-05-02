using System;

namespace YzenImport.Exceptions
{
    class InvalidMccCodeFormatException : Exception
    {
        public InvalidMccCodeFormatException(string mccRaw)
            : base(@$"MCC code ""{mccRaw}"" has incorrect value or format. It should contain only digits.")
        {
        }
    }
}
