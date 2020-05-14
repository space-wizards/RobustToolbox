using System;
using System.Globalization;

namespace Robust.Shared.Localization.Macros
{
    /// <summary>
    /// Override CultureInfo to use MacroFormatter in IFormatProvider.GetFormat
    /// </summary>
    public class MacroCultureInfoWrapper : CultureInfo
    {
        /// <summary>
        /// IFormatProvider that uses the base CultureInfo implementation of IFormatProvider
        /// </summary>
        private struct CultureFormatProvider : IFormatProvider
        {
            public MacroCultureInfoWrapper MacroCultureInfo;

            public object GetFormat(Type formatType)
            {
                return MacroCultureInfo.GetCultureFormat(formatType);
            }
        }

        private readonly MacroFormatter Formatter;

        public MacroCultureInfoWrapper(MacroFormatter formatter, int culture)
            : base(culture)
        {
            this.Formatter = formatter;
        }

        public MacroCultureInfoWrapper(MacroFormatter formatter, string name)
            : base(name)
        {
            this.Formatter = formatter;
        }

        public MacroCultureInfoWrapper(MacroFormatter formatter, int culture, bool useUserOverride)
            : base(culture, useUserOverride)
        {
            this.Formatter = formatter;
        }

        public MacroCultureInfoWrapper(MacroFormatter formatter, string name, bool useUserOverride)
            : base(name, useUserOverride)
        {
            this.Formatter = formatter;
        }

        public override object GetFormat(Type formatType)
        {
            return Formatter;
        }

        /// <summary>
        /// Get the underlying "original" IFormatProvider implemented by CultureInfo
        /// </summary>
        public IFormatProvider GetCultureFormatProvider()
        {
            return new CultureFormatProvider
            {
                MacroCultureInfo = this,
            };
        }

        private object GetCultureFormat(Type formatType)
        {
            return base.GetFormat(formatType);
        }
    }
}
