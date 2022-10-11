using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    public override void SetLocalPosition(TransformComponent xform, Vector2 value)
    {
        xform._prevPosition = xform._localPosition;
        xform._nextPosition = value;
        xform.LerpParent = xform.ParentUid;
        base.SetLocalPosition(xform, value);
        ActivateLerp(xform);
    }

    public override void SetLocalPositionNoLerp(TransformComponent xform, Vector2 value)
    {
        xform._nextPosition = null;
        xform.LerpParent = EntityUid.Invalid;
        base.SetLocalPositionNoLerp(xform, value);
    }

    public override void SetLocalRotation(TransformComponent xform, Angle angle)
    {
        xform._prevRotation = xform._localRotation;
        xform._nextRotation = angle;
        xform.LerpParent = xform.ParentUid;
        base.SetLocalRotation(xform, angle);
        ActivateLerp(xform);
    }

    public override void SetLocalPositionRotation(TransformComponent xform, Vector2 pos, Angle rot)
    {
        xform._prevPosition = xform._localPosition;
        xform._nextPosition = pos;
        xform._prevRotation = xform._localRotation;
        xform._nextRotation = rot;
        xform.LerpParent = xform.ParentUid;
        base.SetLocalPositionRotation(xform, pos, rot);
        ActivateLerp(xform);
    }
}
