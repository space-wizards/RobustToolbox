using System.Collections.Generic;
using System.Drawing;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.ColorNaming;

/// <summary>
/// A prototype for storing arbitrary, named references to colours.
/// </summary>
[Prototype]
public sealed partial class PalettePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = null!;

    /// <summary>
    /// A localizable name for the palette.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; } = null!;

    /// <summary>
    /// A dictionary of names to colors.
    /// When being defined in YAML, colors should not be stored as references to other palettes.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, Color> Colors { get; private set; } = null!;
}
