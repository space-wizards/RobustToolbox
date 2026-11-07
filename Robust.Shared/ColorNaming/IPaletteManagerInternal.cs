namespace Robust.Shared.ColorNaming;

/// <summary>
/// Handles references to named palettes, giving a mechanism for PalettePrototypes to be added on serialization.
/// </summary>
[NotContentImplementable]
internal interface IPaletteManagerInternal : IPaletteManager
{
    /// <summary>
    /// Adds the colors in a given palette prototype into the palette manager.
    /// </summary>
    void Add(PalettePrototype palette);
}
