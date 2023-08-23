using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class Gravity2DComponent : Component
{
    /// <summary>
    /// Applies side-view gravity to the map.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("gravity")]
    public Vector2 Gravity;
}
