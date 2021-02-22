using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

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
        ///     Gets a language approrpiate string represented by the supplied messageId.
        /// </summary>
        /// <param name="messageId">Unique Identifier for a translated message.</param>
        /// <returns>
        ///     The language appropriate message if available, otherwise the messageId is returned.
        /// </returns>
        public static string GetString(string messageId)
        {
            return LocalizationManager.GetString(messageId);
        }

        public static bool TryGetString(string messageId, [NotNullWhen(true)] out string? message)
        {
            return LocalizationManager.TryGetString(messageId, out message);
        }

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that supports arguments.
        /// </summary>
        public static string GetString(string messageId, params (string,object)[] args)
        {
            return LocalizationManager.GetString(messageId, args);
        }

        public static bool TryGetString(
            string messageId,
            [NotNullWhen(true)] out string? value,
            params (string, object)[] args)
        {
            return LocalizationManager.TryGetString(messageId, out value, args);
        }

        /// <summary>
        ///     Load data for a culture.
        /// </summary>
        /// <param name="resourceManager"></param>
        /// <param name="macroFactory"></param>
        /// <param name="culture"></param>
        public static void LoadCulture(IResourceManager resourceManager, CultureInfo culture)
        {
            LocalizationManager.LoadCulture(resourceManager, culture);
        }


        /// <summary>
        /// Remnants of the old Localization system.
        /// It exists to prevent source errors and allow existing game text to *mostly* work
        /// </summary>
        [Obsolete]
        [StringFormatMethod("text")]
        public static string GetString(string text, params object[] args)
        {
            return LocalizationManager.GetString(text, args);
        }
    }
}
