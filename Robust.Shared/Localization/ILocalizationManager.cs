using System.Globalization;

namespace Robust.Shared.Localization
{
    public interface ILocalizationManager
    {
        /// <summary>
        ///     Gets a string translated for the current culture.
        /// </summary>
        /// <param name="text">The string to get translated.</param>
        /// <returns>
        ///     The translated string if a translation is available, otherwise the string is returned.
        /// </returns>
        string GetString(string text);

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that also runs string formatting.
        /// </summary>
        string GetString(string text, params object[] args);

        /// <summary>
        ///     Gets a string inside a context or category.
        /// </summary>
        string GetParticularString(string context, string text);

        /// <summary>
        ///     Gets a string inside a context or category with formatting.
        /// </summary>
        string GetParticularString(string context, string text, params object[] args);

        string GetPluralString(string text, string pluralText, long n);

        string GetPluralString(string text, string pluralText, long n, params object[] args);

        string GetParticularPluralString(string context, string text, string pluralText, long n);

        string GetParticularPluralString(string context, string text, string pluralText, long n, params object[] args);

        /// <summary>
        ///     Default culture used by other methods when no culture is explicitly specified.
        ///     Changing this also changes the current thread's culture.
        /// </summary>
        CultureInfo DefaultCulture { get; set; }

        /// <summary>
        ///     Load data for a culture.
        /// </summary>
        /// <param name="culture"></param>
        void LoadCulture(CultureInfo culture);
    }
}
