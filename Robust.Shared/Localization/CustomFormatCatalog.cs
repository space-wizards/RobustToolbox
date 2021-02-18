using System;
using System.Globalization;
using System.IO;
using NGettext;
using NGettext.Loaders;

namespace Robust.Shared.Localization
{
    /// <summary>
    /// Catalog with custom IFormatProvider
    /// </summary>
    public class CustomFormatCatalog : Catalog
    {
        public IFormatProvider? CustomFormatProvider;

        public CustomFormatCatalog()
            : base()
        { }

        public CustomFormatCatalog(CultureInfo cultureInfo)
            : base(cultureInfo)
        { }

        public CustomFormatCatalog(ILoader loader)
            : base(loader)
        { }

        public CustomFormatCatalog(Stream moStream)
               : base(moStream)
        { }

        public CustomFormatCatalog(ILoader loader, CultureInfo cultureInfo)
                : base(loader, cultureInfo)
        { }

        public CustomFormatCatalog(Stream moStream, CultureInfo cultureInfo)
                : base(moStream, cultureInfo)
        { }

        public CustomFormatCatalog(string domain, string localeDir)
                : base(domain, localeDir)
        { }

        public CustomFormatCatalog(string domain, string localeDir, CultureInfo cultureInfo)
                : base(domain, localeDir, cultureInfo)
        { }

        public override string GetString(string text, params object[] args)
        {
            return string.Format(GetFormatProviderOrDefault(), GetStringDefault(text, text), args);
        }

        public override string GetPluralString(string text, string pluralText, long n, params object[] args)
        {
            return string.Format(GetFormatProviderOrDefault(), GetPluralStringDefault(text, text, pluralText, n), args);
        }

        public override string GetParticularString(string context, string text, params object[] args)
        {
            return string.Format(GetFormatProviderOrDefault(), GetStringDefault(context + CONTEXT_GLUE + text, text), args);
        }

        public override string GetParticularPluralString(string context, string text, string pluralText, long n, params object[] args)
        {
            return string.Format(GetFormatProviderOrDefault(), GetPluralStringDefault(context + CONTEXT_GLUE + text, text, pluralText, n), args);
        }

        private IFormatProvider GetFormatProviderOrDefault()
        {
            return CustomFormatProvider ?? CultureInfo;
        }
    }
}
