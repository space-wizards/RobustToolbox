using System.Collections.Generic;
using System.Globalization;

namespace Robust.Client.Graphics;

/// <summary>
/// Provides access to fonts installed on the user's operating system.
/// </summary>
/// <remarks>
/// <para>
/// Different operating systems ship different fonts, so you should generally not rely on any one
/// specific font being available. This system is primarily provided for allowing user preference.
/// </para>
/// </remarks>
/// <seealso cref="ISystemFontFace"/>
[NotContentImplementable]
public interface ISystemFontManager
{
    /// <summary>
    /// Whether access to system fonts is currently supported on this platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// The list of font face available from the operating system.
    /// </summary>
    IEnumerable<ISystemFontFace> SystemFontFaces { get; }
}

/// <summary>
/// A single font face, provided by the user's operating system.
/// </summary>
/// <seealso cref="ISystemFontManager"/>
[NotContentImplementable]
public interface ISystemFontFace
{
    /// <summary>
    /// The PostScript name of the font face.
    /// This is generally the closest to an unambiguous unique identifier as you're going to get.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, "Arial-ItalicMT"
    /// </para>
    /// </remarks>
    string PostscriptName { get; }

    /// <summary>
    /// The full name of the font face, localized to the current locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, "Arial Cursiva"
    /// </para>
    /// </remarks>
    /// <seealso cref="GetLocalizedFullName"/>
    string FullName { get; }

    /// <summary>
    /// The family name of the font face, localized to the current locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, "Arial"
    /// </para>
    /// </remarks>
    /// <seealso cref="GetLocalizedFamilyName"/>
    string FamilyName { get; }

    /// <summary>
    /// The face name (or "style name") of the font face, localized to the current locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For example, "Cursiva"
    /// </para>
    /// </remarks>
    /// <seealso cref="GetLocalizedFaceName"/>
    string FaceName { get; }

    /// <summary>
    /// Get the <see cref="FullName"/>, localized to a specific locale.
    /// </summary>
    /// <param name="culture">The locale to fetch the localized string for.</param>
    string GetLocalizedFullName(CultureInfo culture);

    /// <summary>
    /// Get the <see cref="FamilyName"/>, localized to a specific locale.
    /// </summary>
    /// <param name="culture">The locale to fetch the localized string for.</param>
    string GetLocalizedFamilyName(CultureInfo culture);

    /// <summary>
    /// Get the <see cref="FaceName"/>, localized to a specific locale.
    /// </summary>
    /// <param name="culture">The locale to fetch the localized string for.</param>
    string GetLocalizedFaceName(CultureInfo culture);

    /// <summary>
    /// The weight of the font face.
    /// </summary>
    FontWeight Weight { get; }

    /// <summary>
    /// The slant of the font face.
    /// </summary>
    FontSlant Slant { get; }

    /// <summary>
    /// The width of the font face.
    /// </summary>
    FontWidth Width { get; }

    /// <summary>
    /// Load the font face so that it can be used in-engine.
    /// </summary>
    /// <param name="size">The size to load the font at.</param>
    /// <returns>A font object that can be used to render text.</returns>
    Font Load(int size);
}

/// <summary>
/// Engine-internal API for <see cref="ISystemFontManager"/>.
/// </summary>
internal interface ISystemFontManagerInternal : ISystemFontManager
{
    void Initialize();
    void Shutdown();
}
