using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Components;

/// <summary>
/// Does this entity have joint data relayed from elsewhere.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JointRelayTargetComponent : Component
{
    [DataField("relayTarget"), AutoNetworkedField]
    public List<EntityUid> Relayed = new();
}
