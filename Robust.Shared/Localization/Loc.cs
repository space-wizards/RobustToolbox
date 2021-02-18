using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization.Macros;

namespace Robust.Shared.Localization
{
    /// <summary>
    /// Static convenience wrapper for the <see cref="ILocalizationManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors the API of <see cref="ILocalizationManager"/>, but statically.
    /// This makes it more convenient to use.
    /// </para>
    /// <para>
    /// This API follows the same thread locality requirements as <see cref="IoCManager"/>
    /// </para>
    /// </remarks>
    [PublicAPI]
    public static class Loc
    {
        private static ILocalizationManager LocalizationManager => IoCManager.Resolve<ILocalizationManager>();

        /// <summary>
        ///     Gets a string translated for the current culture.
        /// </summary>
        /// <param name="text">The string to get translated.</param>
        /// <returns>
        ///     The translated string if a translation is available, otherwise the string is returned.
        /// </returns>
        public static string GetString(string text)
        {
            return LocalizationManager.GetString(text);
        }

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that also runs string formatting.
        /// </summary>
        [StringFormatMethod("text")]
        public static string GetString(string text, params object[] args)
        {
            return LocalizationManager.GetString(text, args);
        }

        /// <summary>
        ///     Gets a string inside a context or category.
        /// </summary>
        public static string GetParticularString(string context, string text)
        {
            return LocalizationManager.GetParticularString(context, text);
        }

        /// <summary>
        ///     Gets a string inside a context or category with formatting.
        /// </summary>
        [StringFormatMethod("text")]
        public static string GetParticularString(string context, string text, params object[] args)
        {
            return LocalizationManager.GetParticularString(context, text, args);

        }

        public static string GetPluralString(string text, string pluralText, long n)
        {
            return LocalizationManager.GetPluralString(text, pluralText, n);

        }

        [StringFormatMethod("pluralText")]
        public static string GetPluralString(string text, string pluralText, long n, params object[] args)
        {
            return LocalizationManager.GetPluralString(text, pluralText, n, args);
        }

        public static string GetParticularPluralString(string context, string text, string pluralText, long n)
        {
            return LocalizationManager.GetParticularString(context, text, pluralText, n);
        }

        [StringFormatMethod("pluralText")]
        public static string GetParticularPluralString(string context, string text, string pluralText, long n,
            params object[] args)
        {
            return LocalizationManager.GetParticularString(context, text, pluralText, n, args);
        }

        /// <summary>
        ///     Load data for a culture.
        /// </summary>
        /// <param name="resourceManager"></param>
        /// <param name="macroFactory"></param>
        /// <param name="culture"></param>
        public static void LoadCulture(IResourceManager resourceManager, ITextMacroFactory macroFactory, CultureInfo culture)
        {
            LocalizationManager.LoadCulture(resourceManager, macroFactory, culture);
        }
    }
}
