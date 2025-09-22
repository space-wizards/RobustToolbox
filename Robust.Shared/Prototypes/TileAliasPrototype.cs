using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Prototype that represents an alias from one tile ID to another. These are used when deserializing entities from yaml.
/// </summary>
[Prototype]
public sealed partial class TileAliasPrototype : IPrototype
{
    /// <summary>
    /// The target tile ID to alias to.
    /// </summary>
    [DataField]
    public string Target { get; private set; } = default!;

    /// <summary>
    /// The source tile ID (and the ID of this tile alias).
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
}
