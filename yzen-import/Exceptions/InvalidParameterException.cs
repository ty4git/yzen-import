using System;

namespace YzenImport.Exceptions
{
    class InvalidParameterException : Exception
    {
        public InvalidParameterException(string paramName)
            : base(@$"Parameter ""{paramName}"" should exist.")
        {
        }
    }
}
