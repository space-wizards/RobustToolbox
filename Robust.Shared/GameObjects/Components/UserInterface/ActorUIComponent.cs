using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Tracks UIs open for a particular entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActorUIComponent : Component
{
    /// <summary>
    /// Currently open BUIs for this entity, also contains corresponding data such as EntityUid, etc.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<PlayerBoundUserInterface> OpenBUIS = new();
}
