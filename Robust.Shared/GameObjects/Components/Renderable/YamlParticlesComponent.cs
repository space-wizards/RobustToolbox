using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class YamlParticlesComponent : Component {
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField] public string ParticleType = "example";
}
