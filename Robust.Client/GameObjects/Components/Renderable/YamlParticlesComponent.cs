using Robust.Client.Graphics;

namespace Robust.Shared.GameObjects;

[RegisterComponent]
public sealed partial class YamlParticlesComponent : SharedYamlParticlesComponent {
    public ParticleSystem? particlesSystem;
}
