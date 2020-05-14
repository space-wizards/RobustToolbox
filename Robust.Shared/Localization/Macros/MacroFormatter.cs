using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Localization.Macros
{
    public class MacroFormatter : ICustomFormatter
    {
        private readonly Dictionary<string, ITextMacro> Macros = new Dictionary<string, ITextMacro>();

        public MacroFormatter(Dictionary<string, ITextMacro> macros)
        {
            Macros = macros;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var subFormatProvider = (formatProvider as MacroCultureInfoWrapper)?.GetCultureFormatProvider() ?? formatProvider;

            if (format == null || format == "")
                return string.Format(subFormatProvider, "{0}", arg);

            bool capitalized = char.IsUpper(format[0]);
            string lowerCasedFunctionName = format.ToLower();

            if (!Macros.TryGetValue(lowerCasedFunctionName, out var grammarFunction))
                return string.Format(subFormatProvider, "{0:" + format + '}', arg);

            return capitalized
                ? grammarFunction.CapitalizedFormat(arg)
                : grammarFunction.Format(arg);
        }
    }
}
