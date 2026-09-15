using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Robust.Client.GameObjects;

public sealed partial class EyeSystem : SharedEyeSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private TransformSystem _renderTransforms = default!;
    [Dependency] private ClientZLevelSystem _clientZLevels = default!;
    [Dependency] private ZLevelSystem _zLevels = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Run after the render transform cache is updated.
        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
        UpdatesAfter.Add(typeof(ClientZLevelSystem));
    }

    [SubscribeLocalEvent]
    private void OnEyeAutoState(Entity<EyeComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        UpdateEye((entity.Owner, entity.Comp));
    }

    [SubscribeLocalEvent]
    private void OnEyeAttached(Entity<EyeComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        UpdateEye((entity.Owner, entity.Comp));
        _eyeManager.CurrentEye = entity.Comp.Eye;
        var ev = new EyeAttachedEvent(entity.Owner, entity.Comp);
        RaiseLocalEvent(entity.Owner, ref ev, true);
    }

    [SubscribeLocalEvent]
    private void OnEyeDetached(Entity<EyeComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        _eyeManager.ClearCurrentEye();
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<EyeComponent> entity, ref ComponentInit args)
    {
        UpdateEye((entity.Owner, entity.Comp));
    }

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        var query = AllEntityQuery<EyeComponent>();

        while (query.MoveNext(out var uid, out var eyeComponent))
        {
            var target = eyeComponent.Target ?? uid;
            if (!TryComp(target, out TransformComponent? xform))
            {
                xform = Transform(uid);
                eyeComponent.Target = null;
                target = uid;
            }

            var pose = _renderTransforms.GetRenderWorldTransform(target, xform);
            var mapId = _renderTransforms.GetMapId((pose.CoordinateSpace, null));
            eyeComponent.Eye.Position = new MapCoordinates(pose.Position, mapId);
            eyeComponent.Eye.RenderedAbsoluteZ = _zLevels.TryGetMapDepth(pose.CoordinateSpace, out _)
                ? _clientZLevels.GetRenderAbsoluteZ(target, xform)
                : null;
        }
    }
}

/// <summary>
/// Raised on an entity when it is attached to one with an <see cref="EyeComponent"/>
/// </summary>
[ByRefEvent]
public readonly record struct EyeAttachedEvent(EntityUid Entity, EyeComponent Component);
