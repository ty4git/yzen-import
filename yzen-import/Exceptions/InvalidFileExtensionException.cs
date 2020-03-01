using System;

namespace YzenImport.Exceptions
{
    class InvalidFileExtensionException : Exception
    {
        public InvalidFileExtensionException()
            : base(@"The file extension should be "".csv"".")
        {
        }
    }
}
