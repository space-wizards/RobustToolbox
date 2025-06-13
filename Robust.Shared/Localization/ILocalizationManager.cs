using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
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
        ///     Gets a language appropriate string represented by the supplied messageId.
        /// </summary>
        /// <param name="messageId">Unique Identifier for a translated message.</param>
        /// <returns>
        ///     The language appropriate message if available, otherwise the messageId is returned.
        /// </returns>
        string GetString(string messageId);

        /// <summary>
        ///     Checks if the specified id has been registered, without checking its arguments.
        /// </summary>
        /// <param name="messageId">Unique Identifier for a translated message.</param>
        /// <returns>true if it exists, even if it requires any parameters to be passed.</returns>
        bool HasString(string messageId);

        /// <summary>
        ///     Try- version of <see cref="GetString(string)"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        bool TryGetString(string messageId, [NotNullWhen(true)] out string? value);

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that supports arguments.
        /// </summary>
        string GetString(string messageId, params (string, object)[] args);

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that supports arguments.
        /// </summary>
        string GetString(string messageId, (string, object) arg);

        /// <summary>
        ///     Version of <see cref="GetString(string)"/> that supports arguments.
        /// </summary>
        string GetString(string messageId, (string, object) arg, (string, object) arg2);

        /// <summary>
        ///     Try- version of <see cref="GetString(string, (string, object)[])"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        bool TryGetString(string messageId, [NotNullWhen(true)] out string? value, (string, object) arg);

        /// <summary>
        ///     Try- version of <see cref="GetString(string, (string, object)[])"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        bool TryGetString(string messageId, [NotNullWhen(true)] out string? value, (string, object) arg1, (string, object) arg2);

        /// <summary>
        ///     Try- version of <see cref="GetString(string, (string, object)[])"/>
        /// </summary>
        /// <remarks>
        ///     Does not log a warning if the message does not exist.
        ///     Does however log errors if any occur while formatting.
        /// </remarks>
        bool TryGetString(string messageId, [NotNullWhen(true)] out string? value, params (string, object)[] keyArgs);

        /// <summary>
        ///     Default culture used by other methods when no culture is explicitly specified.
        ///     Changing this also changes the current thread's culture.
        /// </summary>
        CultureInfo? DefaultCulture { get; set; }

        /// <summary>
        /// Checks if the culture is loaded, if not,
        /// loads it via <see cref="ILocalizationManager.LoadCulture"/>
        /// and then set it as <see cref="ILocalizationManager.DefaultCulture"/>.
        /// </summary>
        void SetCulture(CultureInfo culture);

        /// <summary>
        /// Checks to see if the culture has been loaded.
        /// </summary>
        bool HasCulture(CultureInfo culture);

        /// <summary>
        ///     Load data for a culture.
        /// </summary>
        /// <param name="culture"></param>
        void LoadCulture(CultureInfo culture);

        /// <summary>
        /// Loads <see cref="CultureInfo"/> obtained from <see cref="CVars.LocCultureName"/>,
        /// they are different for client and server, and also can be saved.
        /// </summary>
        CultureInfo SetDefaultCulture();

        /// <summary>
        /// Returns all locale directories from the game's resources.
        /// </summary>
        List<CultureInfo> GetFoundCultures();

        /// <summary>
        ///     Sets culture to be used in the absence of the main one.
        /// </summary>
        void SetFallbackCluture(params CultureInfo[] culture);

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
        /// Initializes the <see cref="LocalizationManager"/>.
        /// </summary>
        void Initialize()
        {
        }
    }

    internal interface ILocalizationManagerInternal : ILocalizationManager
    {
        void AddLoadedToStringSerializer(IRobustMappedStringSerializer serializer);
    }
}
