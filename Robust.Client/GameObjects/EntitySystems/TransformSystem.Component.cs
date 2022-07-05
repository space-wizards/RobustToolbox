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
}
