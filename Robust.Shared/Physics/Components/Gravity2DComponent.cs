using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Components;

[RegisterComponent, NetworkedComponent]
public sealed class Gravity2DComponent : Component
{
    /// <summary>
    /// Applies side-view gravity to the map.
    /// </summary>
    [DataField("gravity")]
    public Vector2 Gravity;
}
