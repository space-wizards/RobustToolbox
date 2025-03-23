using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Gravity2DComponent : Component
{
    /// <summary>
    /// Applies side-view gravity to the map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Gravity;
}
