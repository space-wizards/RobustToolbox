using System.Numerics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    public override void SetLocalPosition(EntityUid uid, Vector2 value, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        ActivateLerp(uid, xform);
        base.SetLocalPosition(uid, value, xform);

        xform.NextPosition = xform.LocalPosition;
    }

    public override void SetLocalRotation(EntityUid uid, Angle value, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        ActivateLerp(uid, xform);
        base.SetLocalRotation(uid, value, xform);

        xform.NextRotation = xform.LocalRotation;
    }

    public override void SetLocalPositionRotation(EntityUid uid, Vector2 pos, Angle rot, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform))
            return;

        ActivateLerp(uid, xform);
        base.SetLocalPositionRotation(uid, pos, rot, xform);

        // Setting the pos itself may get mutated due to subscribers or grid traversal.
        xform.NextPosition = xform.LocalPosition;
        xform.NextRotation = xform.LocalRotation;
    }
}
