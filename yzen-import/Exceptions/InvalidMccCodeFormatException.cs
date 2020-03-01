using System;

namespace YzenImport.Exceptions
{
    class InvalidMccCodeFormatException : Exception
    {
        public InvalidMccCodeFormatException(string mccCodeValueRaw)
            : base(@$"MCC code ""{mccCodeValueRaw}"" has incorrect value or format. It should contain only digits.")
        {
        }
    }
}
