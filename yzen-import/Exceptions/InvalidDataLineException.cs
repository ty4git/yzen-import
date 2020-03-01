using System;

namespace YzenImport.Exceptions
{
    class InvalidDataLineException : Exception
    {
        public InvalidDataLineException()
            : base("A file line with data differs from a title.")
        {
        }
    }
}
