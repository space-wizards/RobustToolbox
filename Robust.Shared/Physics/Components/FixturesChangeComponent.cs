using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Physics.Components;

/// <summary>
/// Adds / removes fixtures on startup / shutdown without modifying the other fixtures on the entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FixturesChangeComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, Fixture> Fixtures = new();
}
