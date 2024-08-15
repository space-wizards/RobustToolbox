using System;
using System.Numerics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects;

public abstract class SharedEyeSystem : EntitySystem
{
    [Dependency] private readonly SharedViewSubscriberSystem _views = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, PlayerAttachedEvent>(OnEyePlayerAttached);
        SubscribeLocalEvent<EyeComponent, PlayerDetachedEvent>(OnEyePlayerDetached);
    }

    private void OnEyePlayerAttached(Entity<EyeComponent> ent, ref PlayerAttachedEvent args)
    {
        var value = ent.Comp.Target;

        if (value != null && TryComp(ent.Owner, out ActorComponent? actorComp))
        {
            _views.AddViewSubscriber(value.Value, actorComp.PlayerSession);
        }
    }

    private void OnEyePlayerDetached(Entity<EyeComponent> ent, ref PlayerDetachedEvent args)
    {
        var value = ent.Comp.Target;

        if (value != null && TryComp(ent.Owner, out ActorComponent? actorComp))
        {
            _views.RemoveViewSubscriber(value.Value, actorComp.PlayerSession);
        }
    }

    /// <summary>
    /// Refreshes all values for IEye with the component.
    /// </summary>
    public void UpdateEye(Entity<EyeComponent?> entity)
    {
        var component = entity.Comp;
        if (!Resolve(entity, ref component))
            return;

        component.Eye.Offset = component.Offset;
        component.Eye.DrawFov = component.DrawFov;
        component.Eye.DrawLight = component.DrawLight;
        component.Eye.Rotation = component.Rotation;
        component.Eye.Zoom = component.Zoom;
    }

    public void SetOffset(EntityUid uid, Vector2 value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Offset.Equals(value))
            return;

        eyeComponent.Offset = value;
        eyeComponent.Eye.Offset = value;
        Dirty(uid, eyeComponent);
    }

    public void SetDrawFov(EntityUid uid, bool value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.DrawFov.Equals(value))
            return;

        eyeComponent.DrawFov = value;
        eyeComponent.Eye.DrawFov = value;
        Dirty(uid, eyeComponent);
    }

    public void SetDrawLight(Entity<EyeComponent?> entity, bool value)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (entity.Comp.DrawLight == value)
            return;

        entity.Comp.DrawLight = value;
        entity.Comp.Eye.DrawLight = value;
        Dirty(entity);
    }

    public void SetRotation(EntityUid uid, Angle rotation, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Rotation.Equals(rotation))
            return;

        eyeComponent.Rotation = rotation;
        eyeComponent.Eye.Rotation = rotation;
    }

    /// <summary>
    /// Sets the eye component as tracking another entity.
    /// Will also add the target to view subscribers so they can leave range and still work with PVS.
    /// </summary>
    public void SetTarget(EntityUid uid, EntityUid? value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Target.Equals(value))
            return;

        // Automatically handle view subs.
        if (TryComp(uid, out ActorComponent? actorComp))
        {
            if (value != null)
            {
                _views.AddViewSubscriber(value.Value, actorComp.PlayerSession);
            }
            else
            {
                // Should never be null here
                _views.RemoveViewSubscriber(eyeComponent.Target!.Value, actorComp.PlayerSession);
            }
        }

        eyeComponent.Target = value;
        Dirty(uid, eyeComponent);
    }

    public void SetZoom(EntityUid uid, Vector2 value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Zoom.Equals(value))
            return;

        eyeComponent.Zoom = value;
        eyeComponent.Eye.Zoom = value;
    }

    public void SetPvsScale(Entity<EyeComponent?> eye, float scale)
    {
        if (!Resolve(eye.Owner, ref eye.Comp, false))
            return;

        // Prevent a admin or some other fuck-up from causing exception spam in PVS system due to divide-by-zero or
        // other such issues
        if (!float.IsFinite(scale))
        {
            Log.Error($"Attempted to set pvs scale to invalid value: {scale}. Eye: {ToPrettyString(eye)}");
            SetPvsScale(eye, 1);
            return;
        }

        eye.Comp.PvsScale = Math.Clamp(scale, 0.1f, 100f);
    }

    public void SetVisibilityMask(EntityUid uid, int value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.VisibilityMask.Equals(value))
            return;

        eyeComponent.VisibilityMask = value;
        Dirty(uid, eyeComponent);
    }
}
