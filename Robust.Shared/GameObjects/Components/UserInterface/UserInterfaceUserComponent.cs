using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Stores data about this entity and what BUIs they have open.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class UserInterfaceUserComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public Dictionary<EntityUid, List<Enum>> OpenInterfaces = new();
}

[Serializable, NetSerializable]
internal sealed class UserInterfaceUserComponentState : IComponentState
{
    public Dictionary<NetEntity, List<Enum>> OpenInterfaces = new();
}
