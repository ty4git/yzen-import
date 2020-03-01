using System;

namespace YzenImport.Exceptions
{
    class InvalidFileContentException : Exception
    {
        public InvalidFileContentException()
            : base("The file should contain 2 lines or greater. First line is title and other are data.")
        {
        }
    }
}
