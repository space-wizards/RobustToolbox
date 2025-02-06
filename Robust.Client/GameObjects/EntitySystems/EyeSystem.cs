using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Robust.Client.GameObjects;

public sealed class EyeSystem : SharedEyeSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EyeComponent, LocalPlayerDetachedEvent>(OnEyeDetached);
        SubscribeLocalEvent<EyeComponent, LocalPlayerAttachedEvent>(OnEyeAttached);
        SubscribeLocalEvent<EyeComponent, AfterAutoHandleStateEvent>(OnEyeAutoState);

        // Make sure this runs *after* entities have been moved by interpolation and movement.
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

            if (!TryComp<TransformComponent>(eyeComponent.Target, out var xform))
            {
                xform = Transform(uid);
                eyeComponent.Target = null;
            }

            eyeComponent.Eye.Position = TransformSystem.GetMapCoordinates(xform);
        }
    }
}

/// <summary>
/// Raised on an entity when it is attached to one with an <see cref="EyeComponent"/>
/// </summary>
[ByRefEvent]
public readonly record struct EyeAttachedEvent(EntityUid Entity, EyeComponent Component);
