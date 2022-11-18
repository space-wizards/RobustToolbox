using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Prototypes;

/// <summary>
/// Prototype that represents an alias from one tile ID to another.
/// Tile alias prototypes, unlike tile prototypes, are implemented here, as they're really just fed to TileDefinitionManager.
/// </summary>
[Prototype("tileAlias", -1)]
public sealed class TileAliasPrototype : IPrototype
{
    /// <summary>
    /// The target tile ID to alias to.
    /// </summary>
    [DataField("target")]
    public string Target { get; private set; } = default!;

    /// <summary>
    /// The source tile ID (and the ID of this tile alias).
    /// </summary>
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; private set; } = default!;
}
