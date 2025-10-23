using System.Numerics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Graphics;

public sealed class ParticlesOverlay : Overlay
{
    private ParticlesManager _particlesManager = default!;
    private IEntityManager _entManager = default!;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    protected internal override void Draw(in OverlayDrawArgs args)
    {
        _particlesManager ??= IoCManager.Resolve<ParticlesManager>();
        _entManager ??= IoCManager.Resolve<IEntityManager>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var xformSystem = _entManager.System<SharedTransformSystem>();

        foreach (var entity in _particlesManager.GetEntitiesWithParticles)
            if (_particlesManager.TryGetParticleSystem(entity, out var system) && xformQuery.TryGetComponent(entity, out var xform))
            {
                system.Draw(args.WorldHandle, xformSystem.GetWorldPositionRotationInvMatrix(xform).InvWorldMatrix);
            }
    }
}
