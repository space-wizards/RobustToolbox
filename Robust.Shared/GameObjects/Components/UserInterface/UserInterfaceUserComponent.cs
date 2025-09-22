using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Stores data about this entity and what BUIs they have open.
/// </summary>
/// <remarks>
/// This component is implicitly networked via <see cref="UserInterfaceComponent"/>.
/// I.e., the other component is authoritative about what UIs are open
/// </remarks>
[RegisterComponent]
public sealed partial class UserInterfaceUserComponent : Component
{
    [DataField]
    public Dictionary<EntityUid, List<Enum>> OpenInterfaces = new();
}
