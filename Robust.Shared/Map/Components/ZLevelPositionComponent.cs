using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Map.Components;

/// <summary>
/// Gives an entity continuous render-only height inside a z-level network.
/// </summary>
/// <remarks>
///  Rendering uses absoluteZ = mapDepth + localHeight without modifying sprite offsets.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
public sealed partial class ZLevelPositionComponent : Component
{
    /// <summary>
    /// Replicated local physics height.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LocalHeight;

    /// <summary>
    /// Increments when continuous z motion crosses a map boundary.
    /// </summary>
    [AutoNetworkedField]
    public uint ContinuousMapMoveSequence;

}
