using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActorComponent : Component
{
    [DataField]
    public Dictionary<EntityUid, List<Enum>> OpenInterfaces = new();

    [ViewVariables]
    public ICommonSession PlayerSession { get; internal set; } = default!;

    [Serializable, NetSerializable]
    internal sealed class ActorComponentState : IComponentState
    {
        public Dictionary<NetEntity, List<Enum>> OpenInterfaces = new();
    }
}
