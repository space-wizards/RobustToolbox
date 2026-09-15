using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Map.Components;

/// <summary>
/// Runtime map group for ordered z-level maps.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(ZLevelSystem))]
public sealed partial class ZLevelMapNetworkComponent : Component
{
    /// <summary>
    /// Runtime z-level maps in lower-to-upper order.
    /// </summary>
    [Access(Other = AccessPermissions.ReadExecute)]
    public IReadOnlyList<EntityUid> SortedZLevels => SortedZLevelsInternal;

    /// <summary>
    /// Mutable runtime z-level map storage.
    /// </summary>
    [AutoNetworkedField, Access(typeof(ZLevelSystem), Other = AccessPermissions.None)]
    internal readonly List<EntityUid> SortedZLevelsInternal = new();

    /// <summary>
    /// Saved maps in lower-to-upper order.
    /// </summary>
    [DataField]
    public List<EntityUid> Maps = new();

    /// <summary>
    /// Saved links between adjacent grids.
    /// </summary>
    [DataField]
    public List<ZLevelGridLink> GridLinks = new();

}

/// <summary>
/// A lower-to-upper grid pair belonging to a saved z-level map network.
/// </summary>
[DataDefinition]
public sealed partial class ZLevelGridLink
{
    [DataField(required: true)]
    public EntityUid Lower;

    [DataField(required: true)]
    public EntityUid Upper;

    public ZLevelGridLink()
    {
    }

    public ZLevelGridLink(EntityUid lower, EntityUid upper)
    {
        Lower = lower;
        Upper = upper;
    }
}

/// <summary>
/// Raised on a z-level network while that network component is shutting down.
/// </summary>
[ByRefEvent]
public readonly record struct ZLevelNetworkShutdownEvent;
