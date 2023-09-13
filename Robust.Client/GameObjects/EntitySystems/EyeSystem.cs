using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects;

public sealed class EyeSystem : SharedEyeSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<EyeComponent, PlayerDetachedEvent>(OnEyeDetached);
        SubscribeLocalEvent<EyeComponent, PlayerAttachedEvent>(OnEyeAttached);
        SubscribeLocalEvent<EyeComponent, AfterAutoHandleStateEvent>(OnEyeAutoState);

        // Make sure this runs *after* entities have been moved by interpolation and movement.
        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesAfter.Add(typeof(PhysicsSystem));
    }

    private void OnEyeAutoState(EntityUid uid, EyeComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateEye(component);
    }

    private void OnEyeAttached(EntityUid uid, EyeComponent component, PlayerAttachedEvent args)
    {
        // TODO: This probably shouldn't be nullable bruv.
        if (component._eye != null)
        {
            _eyeManager.CurrentEye = component._eye;
        }

        var ev = new EyeAttachedEvent(uid, component);
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnEyeDetached(EntityUid uid, EyeComponent component, PlayerDetachedEvent args)
    {
        _eyeManager.ClearCurrentEye();
    }

    private void OnInit(EntityUid uid, EyeComponent component, ComponentInit args)
    {
        component._eye = new Eye
        {
            Position = Transform(uid).MapPosition,
            Zoom = component.Zoom,
            DrawFov = component.DrawFov,
            Rotation = component.Rotation,
        };
    }

    /// <inheritdoc />
    public override void FrameUpdate(float frameTime)
    {
        var query = AllEntityQuery<EyeComponent>();

        while (query.MoveNext(out var uid, out var eyeComponent))
        {
            if (eyeComponent._eye == null)
                continue;

            if (!TryComp<TransformComponent>(eyeComponent.Target, out var xform))
            {
                xform = Transform(uid);
                eyeComponent.Target = null;
            }

            eyeComponent._eye.Position = xform.MapPosition;
        }
    }
}

/// <summary>
/// Raised on an entity when it is attached to one with an <see cref="EyeComponent"/>
/// </summary>
[ByRefEvent]
public readonly record struct EyeAttachedEvent(EntityUid Entity, EyeComponent Component);
