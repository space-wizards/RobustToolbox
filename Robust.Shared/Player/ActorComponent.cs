using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, List<Enum>> OpenInterfaces = new();

    [ViewVariables]
    public ICommonSession PlayerSession { get; internal set; } = default!;
}
