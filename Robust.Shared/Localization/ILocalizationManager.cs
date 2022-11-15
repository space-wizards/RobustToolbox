using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
using Linguini.Bundle;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;

namespace Robust.Shared.Localization
{
    // ReSharper disable once CommentTypo
    /// <summary>
    ///     Provides facilities to obtain language appropriate in-game text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     Translation is handled using Project Fluent (https://www.projectfluent.org/)
    ///     You pass a Fluent 'identifier' as a string and the localization manager will fetch the message
    ///     matching that identifier from the currently loaded language's Fluent files.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Loc"/>
    [PublicAPI]
    public interface ILocalizationManager
    {
        /// <summary>
        ///     Gets a language approrpiate string represented by the supplied messageId.
        /// </summary>
        /// <param name="messageId">Unique Identifier for a translated message.</param>
        /// <returns>
        ///     The language appropriate message if available, otherwise the messageId is returned.
        /// </returns>
        [Obsolete("Use `TryGetString(FText, out string?)` instead")]
        string GetString(string messageId);

        /// <summary>
        ///     Try- version of <see cref="GetString(string)"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        [Obsolete("Use `TryGetString(FText, out string?)` instead")]
        bool TryGetString(string messageId, [NotNullWhen(true)] out string? value);

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that supports arguments.
        /// </summary>
        [Obsolete("Use `GetString(FText)` instead")]
        string GetString(string messageId, params (string, object)[] args);

        /// <summary>
        ///     Try- version of <see cref="GetString(string, ValueTuple{string, object}[])"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        [Obsolete("Use `GetString(FText, out string?)` instead")]
        bool TryGetString(string messageId, [NotNullWhen(true)] out string? value, params (string, object)[] keyArgs);

        /*
         * FText interface
         */
        /// <summary>
        /// Will retrieve currently selected language or fallback set to <see cref="CultureInfo.InvariantCulture"/>
        /// </summary>
        /// <returns>Currently selected language</returns>
        CultureInfo GetSelectedLang();

        /// <summary>
        /// Will set currently selected to <see cref="CVars.UILang"/> language code or fallback to
        /// to <see cref="CultureInfo.InvariantCulture"/>. It will also update Ui and current thread culture.
        /// </summary>
        /// <param name="uiLang"><see cref="CVarDef"/> of string type that represents the UiLang. Defaults to <see cref="CVars.UILang"/></param>
        void SetSelectedLang(CVarDef<string>? uiLang = null);

        /// <summary>
        ///     Gets a language appropriate string represented by the supplied messageId.
        /// </summary>
        /// <param name="messageId">Unique Identifier for a translated message.</param>
        /// <returns>
        ///     The language appropriate message if available, otherwise the messageId is returned.
        /// </returns>
        string GetString(FText messageId);


        /// <summary>
        ///     Try- version of <see cref="GetString(FText, ValueTuple{string, object}[])"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        bool TryGetString(FText message, [NotNullWhen(true)] out string? bundle);

        /// <summary>
        ///     Default culture used by other methods when no culture is explicitly specified.
        ///     Changing this also changes the current thread's culture.
        /// </summary>
        CultureInfo? DefaultCulture { get; set; }

        /// <summary>
        ///     Load data for a culture.
        /// </summary>
        /// <param name="culture"></param>
        void LoadCulture(CultureInfo culture);

        /// <summary>
        ///     Immediately reload ALL localizations from resources.
        /// </summary>
        void ReloadLocalizations();

        /// <summary>
        ///     Add a function that can be called from Fluent localizations.
        /// </summary>
        /// <param name="culture">The culture to add the function instance for.</param>
        /// <param name="name">The name of the function.</param>
        /// <param name="function">The function itself.</param>
        void AddFunction(CultureInfo culture, string name, LocFunction function);

        /// <summary>
        ///     Gets localization data for an entity prototype.
        /// </summary>
        EntityLocData GetEntityData(string prototypeId);

        /// <summary>
        ///     Lists all available localizations for applications. List is generated based on folders present in
        ///    `/Resources/Locale/` folder. This action is potentially slow so it will cache its results between runs.
        /// </summary>
        /// <param name="forceUpdate">If true will rerun scan for existing localizations. Defaults to false</param>
        /// <returns></returns>
        public IEnumerable<CultureInfo> GetAvailableLocalizations(bool forceUpdate = false);
    }

    internal interface ILocalizationManagerInternal : ILocalizationManager
    {
        void AddLoadedToStringSerializer(IRobustMappedStringSerializer serializer);
    }
}
