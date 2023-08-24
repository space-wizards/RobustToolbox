using System.Numerics;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

public abstract class SharedEyeSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    public void SetOffset(EntityUid uid, Vector2 value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Offset.Equals(value))
            return;

        eyeComponent.Offset = value;
        Dirty(uid, eyeComponent);
    }

    public void SetDrawFov(EntityUid uid, bool value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.DrawFov.Equals(value))
            return;

        eyeComponent.DrawFov = value;
        Dirty(uid, eyeComponent);
    }

    public void SetZoom(EntityUid uid, Vector2 value, EyeComponent? eyeComponent = null)
    {
        if (!Resolve(uid, ref eyeComponent))
            return;

        if (eyeComponent.Offset.Equals(value))
            return;

        eyeComponent.Offset = value;
        Dirty(uid, eyeComponent);
    }
}
