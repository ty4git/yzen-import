﻿using YzenImport.Exceptions;

namespace YzenImport.Helpers
{
    static class Converters
    {
        public static (bool Found, int Mcc) TryConvertMcc(string mccRaw)
        {
            if (int.TryParse(mccRaw, out var mcc))
            {
                return (true, mcc);
            }

            return (false, default);
        }

        public static int? ToMcc(string raw)
        {
            return int.TryParse(raw, out var mcc) ? (int?)mcc : null;
        }
    }
}
