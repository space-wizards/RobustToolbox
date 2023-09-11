using System.Numerics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract class SharedEyeSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    /// <summary>
    /// Refreshes all values for IEye with the component.
    /// </summary>
    public void UpdateEye(EyeComponent component)
    {
        if (component._eye == null)
            return;

        component._eye.Offset = component.Offset;
        component._eye.DrawFov = component.DrawFov;
        component._eye.Rotation = component.Rotation;
        component._eye.Zoom = component.Zoom;
    }

    public void SetOffset(EntityUid uid, Vector2 value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Offset.Equals(value))
            return;

        eyeComponent.Offset = value;
        if (eyeComponent._eye != null)
        {
            eyeComponent._eye.Offset = value;
        }
        Dirty(uid, eyeComponent);
    }

    public void SetDrawFov(EntityUid uid, bool value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.DrawFov.Equals(value))
            return;

        eyeComponent.DrawFov = value;
        if (eyeComponent._eye != null)
        {
            eyeComponent._eye.DrawFov = value;
        }
        Dirty(uid, eyeComponent);
    }

    public void SetRotation(EntityUid uid, Angle rotation, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Rotation.Equals(rotation))
            return;

        eyeComponent.Rotation = rotation;
        if (eyeComponent._eye != null)
        {
            eyeComponent._eye.Rotation = rotation;
        }
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
        if (eyeComponent._eye != null)
        {
            eyeComponent._eye.Zoom = value;
        }
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
