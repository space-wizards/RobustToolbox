using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Tracks UIs open for a particular entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActorUIComponent : Component
{
    /// <summary>
    /// Currently open BUIs for this entity, also contains corresponding data such as EntityUid, etc.
    /// </summary>
    [DataField]
    public List<PlayerBoundUserInterface> OpenBUIS = new();
}

[Serializable, NetSerializable]
public sealed class ActorUIComponentState : ComponentState
{
    public List<PlayerBoundUserInterface> OpenBUIS;

    public ActorUIComponentState(List<PlayerBoundUserInterface> openBuis)
    {
        OpenBUIS = openBuis;
    }
}
