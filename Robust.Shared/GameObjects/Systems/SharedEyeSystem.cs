using System.Numerics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract class SharedEyeSystem : EntitySystem
{
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

    public void SetTarget(EntityUid uid, EntityUid? value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Target.Equals(value))
            return;

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
