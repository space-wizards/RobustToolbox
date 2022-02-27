using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedTransformSystem
{
    #region World Matrix

    [Pure]
    public Matrix3 GetWorldMatrix(EntityUid uid)
    {
        return Transform(uid).WorldMatrix;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetWorldMatrix(TransformComponent component)
    {
        return component.WorldMatrix;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetWorldMatrix(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetWorldMatrix(xformQuery.GetComponent(uid));
    }

    #endregion

    #region World Rotation

    [Pure]
    public Angle GetWorldRotation(EntityUid uid)
    {
        return Transform(uid).WorldRotation;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle GetWorldRotation(TransformComponent component)
    {
        return component.WorldRotation;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Angle GetWorldRotation(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetWorldRotation(xformQuery.GetComponent(uid));
    }

    #endregion

    #region Inverse World Matrix

    [Pure]
    public Matrix3 GetInvWorldMatrix(EntityUid uid)
    {
        return Comp<TransformComponent>(uid).InvWorldMatrix;
    }

    // Temporary until it's moved here
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetInvWorldMatrix(TransformComponent component)
    {
        return component.InvWorldMatrix;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix3 GetInvWorldMatrix(EntityUid uid, EntityQuery<TransformComponent> xformQuery)
    {
        return GetInvWorldMatrix(xformQuery.GetComponent(uid));
    }

    #endregion
}
