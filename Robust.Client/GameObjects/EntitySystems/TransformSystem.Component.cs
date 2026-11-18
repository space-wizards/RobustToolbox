using System.Numerics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    public override void SetLocalPosition(EntityUid uid, Vector2 value, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        xform.NextPosition = value;
        ActivateLerp(uid, xform);
        base.SetLocalPosition(uid, value, xform);
    }

    public override void SetLocalRotation(EntityUid uid, Angle value, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        xform.NextRotation = value;
        ActivateLerp(uid, xform);
        base.SetLocalRotation(uid, value, xform);
    }

    public override void SetLocalPositionRotation(EntityUid uid, Vector2 pos, Angle rot, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        xform.NextPosition = pos;
        xform.NextRotation = rot;
        ActivateLerp(uid, xform);
        base.SetLocalPositionRotation(uid, pos, rot, xform);
    }
}
