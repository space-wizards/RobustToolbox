using System;
using System.Globalization;

namespace Robust.Shared.Localization.Macros
{
    public class MacroFormatProvider : IFormatProvider
    {
        public MacroFormatter Formatter;

        public CultureInfo CultureInfo;

        public MacroFormatProvider(MacroFormatter formatter, CultureInfo cultureInfo)
        {
            Formatter = formatter;
            CultureInfo = cultureInfo;
        }

        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return Formatter;
            else
                return CultureInfo.GetFormat(formatType);
        }
    }
}
