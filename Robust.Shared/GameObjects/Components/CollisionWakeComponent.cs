using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
///     An optimisation component for stuff that should be set as collidable when it's awake and non-collidable when asleep.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CollisionWakeSystem))]
public sealed partial class CollisionWakeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
