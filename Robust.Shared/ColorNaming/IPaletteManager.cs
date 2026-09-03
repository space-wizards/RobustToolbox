using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Robust.Shared.ColorNaming;

/// <summary>
/// Handles references to named palettes, resolving strings into Colors as stored in <see cref="PalettePrototype"/>
/// </summary>
/// <remarks>
/// Terminology:<br/>
/// "Kinds" are the types of prototypes there are, like <see cref="EntityPrototype"/>.<br/>
/// "Prototypes" are simply filled-in prototypes from YAML.<br/>
/// </remarks>
/// <seealso cref="IPrototype"/>
/// <seealso cref="IInheritingPrototype"/>
/// <seealso cref="PrototypeAttribute"/>
[NotContentImplementable]
public interface IPaletteManager
{
    /// <summary>
    /// Initializes the palette manager.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Returns the color from strings of the form "PaletteName.ColorName".
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the palette or color name cannot be found.
    /// </exception>
    Color GetQualifiedColor(string name);

    /// <summary>
    /// Looks up a color from strings of the form "PaletteName.ColorName".
    /// Returns whether or not the color could be found, writes the value out into <paramref name="color"/> if it can.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the palette or color name cannot be found.
    /// </exception>
    bool TryGetQualifiedColor(string name, [NotNullWhen(true)] out Color? color);

    /// <summary>
    /// Clears and fills <paramref name="colors"/> of all colors within a given palette.
    /// Order of colors is not guaranteed.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the kind of prototype is not registered.
    /// </exception>
    void GetPaletteColors(ProtoId<PalettePrototype> palette, List<Color> colors);

    /// <summary>
    /// Return a <see cref="List{Color}"/> of all prototypes of a certain kind.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the kind of prototype is not registered.
    /// </exception>
    bool TryGetPaletteColors(ProtoId<PalettePrototype> palette, List<Color> colors);

    /// <summary>
    /// Return a <see cref="List{Color}"/> of all colors within a given prototype.
    /// Order of colors is not guaranteed.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the kind of prototype is not registered.
    /// </exception>
    Color PickRandomColor(ProtoId<PalettePrototype> palette);

    /// <summary>
    /// Return a <see cref="List{Color}"/> of all colors within a given prototype.
    /// Order of colors is not guaranteed.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the kind of prototype is not registered.
    /// </exception>
    bool TryPickRandomColor(ProtoId<PalettePrototype> palette, [NotNullWhen(true)] out Color? color);
}
