using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Localization.Macros;
using Robust.Shared.Serialization;

namespace Robust.Shared.Localization
{
    // ReSharper disable once CommentTypo
    /// <summary>
    ///     Provides facilities to automatically translate in-game text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The translation API is similar to GNU gettext.
    ///     You pass a string through it (most often the English version),
    ///     and when the game is ran in another language with adequate translation, the translation is returned instead.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Loc"/>
    [PublicAPI]
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
        [StringFormatMethod("text")]
        string GetString(string text, params object[] args);

        /// <summary>
        ///     Gets a string inside a context or category.
        /// </summary>
        string GetParticularString(string context, string text);

        /// <summary>
        ///     Gets a string inside a context or category with formatting.
        /// </summary>
        [StringFormatMethod("text")]
        string GetParticularString(string context, string text, params object[] args);

        string GetPluralString(string text, string pluralText, long n);

        [StringFormatMethod("pluralText")]
        string GetPluralString(string text, string pluralText, long n, params object[] args);

        string GetParticularPluralString(string context, string text, string pluralText, long n);

        [StringFormatMethod("pluralText")]
        string GetParticularPluralString(string context, string text, string pluralText, long n, params object[] args);

        /// <summary>
        ///     Default culture used by other methods when no culture is explicitly specified.
        ///     Changing this also changes the current thread's culture.
        /// </summary>
        CultureInfo? DefaultCulture { get; set; }

        /// <summary>
        ///     Load data for a culture.
        /// </summary>
        /// <param name="resourceManager"></param>
        /// <param name="textMacroFactory"></param>
        /// <param name="culture"></param>
        void LoadCulture(IResourceManager resourceManager, ITextMacroFactory textMacroFactory, CultureInfo culture);
    }

    internal interface ILocalizationManagerInternal : ILocalizationManager
    {
        void AddLoadedToStringSerializer(IRobustMappedStringSerializer serializer);
    }
}
