using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    public override void SetLocalPosition(TransformComponent xform, Vector2 value)
    {
        xform.PrevPosition = xform._localPosition;
        xform.NextPosition = value;
        xform.LerpParent = xform.ParentUid;
        base.SetLocalPosition(xform, value);
        ActivateLerp(xform);
    }

    public override void SetLocalPositionNoLerp(TransformComponent xform, Vector2 value)
    {
        xform.NextPosition = null;
        xform.LerpParent = EntityUid.Invalid;
        base.SetLocalPositionNoLerp(xform, value);
    }

    public override void SetLocalRotation(TransformComponent xform, Angle angle)
    {
        xform.PrevRotation = xform._localRotation;
        xform.NextRotation = angle;
        xform.LerpParent = xform.ParentUid;
        base.SetLocalRotation(xform, angle);
        ActivateLerp(xform);
    }

    public override void SetLocalPositionRotation(TransformComponent xform, Vector2 pos, Angle rot)
    {
        xform.PrevPosition = xform._localPosition;
        xform.NextPosition = pos;
        xform.PrevRotation = xform._localRotation;
        xform.NextRotation = rot;
        xform.LerpParent = xform.ParentUid;
        base.SetLocalPositionRotation(xform, pos, rot);
        ActivateLerp(xform);
    }
}
