using YzenImport.Exceptions;

namespace YzenImport.Helpers
{
    static class Converters
    {
        public static int ConvertMccCodeValue(string mccCodeValueRaw)
        {
            if (int.TryParse(mccCodeValueRaw, out var mccCodeValue))
            {
                return mccCodeValue;
            }

            throw new InvalidMccCodeFormatException(mccCodeValueRaw);
        }
    }
}
