using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    // Snapped rotation can update immediately while position keeps its normal interpolation.
    private void UpdateSnappedRenderRotation(EntityUid uid, in RenderTransformEndpoint target)
    {
        if (!_snapRenderRotations.TryGetValue(uid, out var snapped)
            || _timing.ApplyingState
            || !_timing.IsFirstTimePredicted && _timing.CurTick < snapped.ChangeTick)
        {
            return;
        }

        // Only first-time prediction may replace the displayed snapped angle.
        _snapRenderRotations[uid] = snapped with { Parent = target.Parent, LocalRotation = target.LocalRotation };
    }

    private RenderTransformEndpoint ApplyRenderRotationOverride(
        EntityUid uid,
        in RenderTransformEndpoint pose,
        RenderTransform parent)
    {
        if (_renderRotationOverrides.TryGetValue(uid, out var rotation))
            return pose with { LocalRotation = rotation - parent.Rotation };

        if (!_snapRenderRotations.TryGetValue(uid, out var snapped))
            return pose;

        // Snap input-facing locally so it still follows its parent's rendered rotation.
        if (snapped.Parent == pose.Parent)
            return pose with { LocalRotation = snapped.LocalRotation };

        var snappedParentRotation = snapped.Parent.IsValid() ? GetRenderWorldRotation(snapped.Parent) : Angle.Zero;
        return pose with { LocalRotation = snappedParentRotation + snapped.LocalRotation - parent.Rotation };
    }

    internal void SetWorldPositionRotationPreservingRenderTransform(
        EntityUid uid,
        Vector2 worldPosition,
        Angle worldRotation,
        TransformComponent? xform = null)
    {
        // Some predicted input changes simulation rotation without changing what this frame displays.
        var hadState = _renderTransforms.TryGetValue(uid, out var state);
        var hasSnapRotation = _snapRenderRotations.TryGetValue(uid, out var snapRotation);

        SetWorldPositionRotation(uid, worldPosition, worldRotation, xform);
        RefreshPredictionSample(uid);

        if (hadState)
            _renderTransforms[uid] = state;
        else
            _renderTransforms.Remove(uid);

        if (hasSnapRotation)
            _snapRenderRotations[uid] = snapRotation;
        else
            _snapRenderRotations.Remove(uid);
    }

    /// <summary>
    /// Snaps rendered rotation to its simulation target without interrupting position interpolation.
    /// </summary>
    public void SnapRenderRotation(EntityUid uid)
    {
        if (_timing.ApplyingState)
            return;

        _snapRenderRotationEntities.Add(uid);

        if (_timing.IsFirstTimePredicted || !_snapRenderRotations.ContainsKey(uid))
        {
            var xform = XformQuery.GetComponent(uid);
            _snapRenderRotations[uid] = new SnappedRenderRotation(xform.ParentUid, xform.LocalRotation, _timing.CurTick);
        }

        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        if (Unsafe.IsNullRef(ref state))
            return;

        SnapRenderRotation(ref state);
    }

    /// <summary>
    /// Overrides an entity's rendered world rotation without changing its simulation transform.
    /// </summary>
    public void SetRenderRotationOverride(EntityUid uid, Angle rotation)
    {
        _renderRotationOverrides[uid] = rotation;
    }

    /// <summary>
    /// Clears an entity's rendered world rotation override.
    /// </summary>
    public void ClearRenderRotationOverride(EntityUid uid)
    {
        _renderRotationOverrides.Remove(uid);
    }

    private void SnapRenderRotation(ref RenderTransformState state)
    {
        state.SnapRotation = true;
        state.CorrectionRotation = Angle.Zero;
        state.LastRendered = ReparentEndpoint(state.LastRendered, state.Target, lastFrame: true)
            with
            {
                LocalRotation = state.Target.LocalRotation
            };
    }

    private readonly record struct SnappedRenderRotation(EntityUid Parent, Angle LocalRotation, GameTick ChangeTick);
}
