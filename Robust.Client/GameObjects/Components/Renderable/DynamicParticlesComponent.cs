using Robust.Client.Graphics;

namespace Robust.Shared.GameObjects;

[RegisterComponent]
public sealed partial class DynamicParticlesComponent : SharedDynamicParticlesComponent {
    public ParticleSystem? particlesSystem;
}
