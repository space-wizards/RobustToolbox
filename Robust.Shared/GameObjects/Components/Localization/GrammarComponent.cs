using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Overrides grammar attributes specified in prototypes or localization files.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GrammarSystem))]
public sealed partial class GrammarComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> Attributes = new();
}
