using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Robust.Client.GameObjects;

public sealed partial class EyeSystem : SharedEyeSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EyeComponent, LocalPlayerDetachedEvent>(OnEyeDetached);
        SubscribeLocalEvent<EyeComponent, LocalPlayerAttachedEvent>(OnEyeAttached);
        SubscribeLocalEvent<EyeComponent, AfterAutoHandleStateEvent>(OnEyeAutoState);

        // Make sure this runs after the entity's final position has been updated.
        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
    }

    private void OnEyeAutoState(EntityUid uid, EyeComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateEye((uid, component));
    }

    private void OnEyeAttached(EntityUid uid, EyeComponent component, LocalPlayerAttachedEvent args)
    {
        UpdateEye((uid, component));
        _eyeManager.CurrentEye = component.Eye;
        var ev = new EyeAttachedEvent(uid, component);
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnEyeDetached(EntityUid uid, EyeComponent component, LocalPlayerDetachedEvent args)
    {
        _eyeManager.ClearCurrentEye();
    }

    private void OnInit(EntityUid uid, EyeComponent component, ComponentInit args)
    {
        UpdateEye((uid, component));
    }

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        var query = AllEntityQuery<EyeComponent>();

        while (query.MoveNext(out var uid, out var eyeComponent))
        {
            if (eyeComponent.Eye == null)
                continue;

            var target = eyeComponent.Target ?? uid;
            if (!TryComp(target, out TransformComponent? xform))
            {
                xform = Transform(uid);
                eyeComponent.Target = null;
                target = uid;
            }

            eyeComponent.Eye.Position = _transform.GetRenderMapCoordinates((target, xform));
        }
    }
}

/// <summary>
/// Raised on an entity when it is attached to one with an <see cref="EyeComponent"/>
/// </summary>
[ByRefEvent]
public readonly record struct EyeAttachedEvent(EntityUid Entity, EyeComponent Component);
