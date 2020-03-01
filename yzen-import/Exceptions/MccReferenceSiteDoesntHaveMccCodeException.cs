using System;

namespace YzenImport.Exceptions
{
    class MccReferenceSiteDoesntHaveMccCodeException : Exception
    {
        public MccReferenceSiteDoesntHaveMccCodeException(string mccCodeValue)
            : base(@$"Site with MCC codes doesn't have information about MCC ""{mccCodeValue}"".")
        {
        }
    }
}
