using System.Numerics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;

namespace Robust.Client.Graphics;

public sealed class ParticlesOverlay : Overlay
{
    [Dependency] private readonly ParticlesManager _particlesManager = default!;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    protected internal override void Draw(in OverlayDrawArgs args)
    {
        if(_particlesManager is null)
            return;
        foreach(var entity in _particlesManager.GetEntitiesWithParticles)
            if(_particlesManager.TryGetParticleSystem(entity, out var system))
                system.Draw(args.WorldHandle, Matrix3x2.Identity);
    }
}
