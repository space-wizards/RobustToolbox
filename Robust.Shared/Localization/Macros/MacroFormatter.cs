using System;
using System.Collections.Generic;

namespace Robust.Shared.Localization.Macros
{
    public class MacroFormatter : ICustomFormatter
    {
        private readonly IDictionary<string, ITextMacro> Macros;

        public MacroFormatter(IDictionary<string, ITextMacro> macros)
        {
            Macros = macros;
        }

        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            IFormatProvider? fallbackProvider = GetFallbackFormatProvider(formatProvider);

            if (format == null || format == "")
                return string.Format(fallbackProvider, "{0}", arg);

            bool capitalized = char.IsUpper(format[0]);
            string lowerCasedFunctionName = char.ToLower(format[0]) + format.Substring(1);

            if (!Macros.TryGetValue(lowerCasedFunctionName, out var grammarFunction))
                return string.Format(fallbackProvider, "{0:" + format + '}', arg);

            return capitalized
                ? grammarFunction.CapitalizedFormat(arg)
                : grammarFunction.Format(arg);
        }

        private static IFormatProvider? GetFallbackFormatProvider(IFormatProvider? formatProvider)
        {
            if (formatProvider is MacroFormatProvider macroFormatProvider)
                return macroFormatProvider.CultureInfo;
            else
                return formatProvider;
        }
    }
}
