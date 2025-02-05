using Robust.Client.Graphics;

namespace Robust.Shared.GameObjects;

[RegisterComponent]
public sealed partial class ParticlesComponent : SharedParticlesComponent {
    public ParticleSystem? particlesSystem;
}
