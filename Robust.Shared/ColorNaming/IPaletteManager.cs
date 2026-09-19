using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Shared.ColorNaming;

/// <summary>
/// Handles references to named palettes, resolving strings into Colors as stored in <see cref="PalettePrototype"/>.
/// </summary>
/// <remarks>
/// Colors are stored and referenced in the form <c>PaletteName.ColorName</c>, where <c>PaletteName</c> is the ID of the <see cref="PalettePrototype"/>,
/// and <c>ColorName</c> is the name of the color given in <see cref="PalettePrototype.Colors"/>.
/// Both values are case sensitive.
/// </remarks>
[NotContentImplementable]
public interface IPaletteManager
{
    /// <summary>
    /// Initializes the palette manager.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Looks up a named color from a string of the form "PaletteName.ColorName".
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if <paramref name="name"/> does not exist.
    /// </exception>
    Color GetQualifiedColor(string name);

    /// <summary>
    /// Looks up a named color from a string of the form "PaletteName.ColorName".
    /// </summary>
    /// <returns>True if <paramref name="color"/> contains the named color, false otherwise.</returns>
    bool TryGetQualifiedColor(string name, [NotNullWhen(true)] out Color? color);

    /// <summary>
    /// Clears and fills <paramref name="colors"/> of all colors within a given palette.
    /// </summary>
    /// <remarks>
    /// Order of colors is not guaranteed.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if <paramref name="palette"/> does not exist.
    /// </exception>
    /// <returns>The list of colors in <paramref name="palette"/>.</returns>
    IReadOnlyList<Color> GetPaletteColors(ProtoId<PalettePrototype> palette);

    /// <summary>
    /// Writes the colors given in <paramref name="palette"/> out into <paramref name="colors"/>, if possible.
    /// </summary>
    /// <remarks>
    /// Order of colors is not guaranteed.
    /// </remarks>
    /// <returns>True if the palette was found and written into <paramref name="colors"/>, false otherwise.</returns>
    bool TryGetPaletteColors(ProtoId<PalettePrototype> palette, out IReadOnlyList<Color> colors);
}
