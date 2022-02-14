using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedTransformSystem
{
    #region World Matrix

    [Pure]
    public Matrix3 GetWorldMatrix(EntityUid uid)
    {
        return Comp<TransformComponent>(uid).WorldMatrix;
    }

    [Pure]
    public Matrix3 GetWorldMatrix(TransformComponent component)
    {
        return component.WorldMatrix;
    }

    [Pure]
    public Matrix3 GetWorldMatrix(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetWorldMatrix(xformQuery.GetComponent(uid));
    }

    #endregion

    #region Inverse World Matrix

    [Pure]
    public Matrix3 GetInvWorldMatrix(EntityUid uid)
    {
        return Comp<TransformComponent>(uid).InvWorldMatrix;
    }

    [Pure]
    public Matrix3 GetInvWorldMatrix(TransformComponent component)
    {
        return component.InvWorldMatrix;
    }

    [Pure]
    public Matrix3 GetInvWorldMatrix(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetInvWorldMatrix(xformQuery.GetComponent(uid));
    }

    #endregion
}
