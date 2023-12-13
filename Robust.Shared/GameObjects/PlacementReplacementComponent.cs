using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// If an entity with this component is placed on top of another anchored entity with this component and the same key it will replace it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlacementReplacementComponent : Component
{
    [DataField("key")]
    public string Key = "";
}
