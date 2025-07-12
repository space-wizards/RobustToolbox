using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Components;


/// <summary>
/// Prevents collision on specific Fixture layers for an attached entity.
/// These layers will effectively be considered "soft" for collision purposes.
/// Start and End collide events will be raised but an entity's movement won't be stopped.
/// Does nothing if the attached entity uses a soft fixture.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FixturesSemiSoftComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Mask;
}
