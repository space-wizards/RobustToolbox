using System.Numerics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    protected override void OnCompStartup(EntityUid uid, TransformComponent xform, ComponentStartup args)
    {
        // When parenting a transform to an entity that doesn't yet exist on the client (ex: outside of PVS),
        // the parenting will be done before the target entity's transform is initialized.
        // The parent transforms will have their GridUids set on initialize, but this value
        // won't be copied to their children. Thus we need to push the GridUid to the children once
        // we have it set up.
        foreach (var child in xform._children)
        {
            SetGridId(child, XformQuery.GetComponent(child), xform._gridUid);
        }

        base.OnCompStartup(uid, xform, args);
    }

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
