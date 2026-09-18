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
        var targetPose = ResolveEndpoint(target, 0);
        _snapRenderRotations[uid] = snapped with { Rotation = targetPose.Rotation };
    }

    private RenderTransform ApplyRenderRotationOverride(EntityUid uid, in RenderTransform pose)
    {
        if (_renderRotationOverrides.TryGetValue(uid, out var rotation))
            return pose with { Rotation = rotation };

        return _snapRenderRotations.TryGetValue(uid, out var snapped)
            ? pose with { Rotation = snapped.Rotation }
            : pose;
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
            _snapRenderRotations[uid] = new SnappedRenderRotation(GetWorldRotation(uid), _timing.CurTick);

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
        var target = ResolveEndpoint(state.Target, 0);
        state.SnapRotation = true;
        state.CorrectionRotation = Angle.Zero;
        state.LastRendered = state.LastRendered with { Rotation = target.Rotation };
    }

    private readonly record struct SnappedRenderRotation(Angle Rotation, GameTick ChangeTick);
}
