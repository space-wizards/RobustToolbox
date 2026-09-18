using System;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    // Network interpolation presents authoritative movement for entities that are not locally predicted.
    private void HandleAppliedTransformMove(
        EntityUid uid,
        TransformComponent xform,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace,
        bool hasExisting,
        ref RenderTransformState existing,
        bool snapRotation)
    {
        // A transform that was dirty in a future prediction tick is being reset. Compare it after prediction.
        if (xform.LastModifiedTick > _timing.LastRealTick
            || hasExisting && existing.Type == RenderInterpolationType.PredictionInterpolation)
        {
            if (hasExisting)
                RebasePredictionInterpolation(ref existing, source, target, rendered, coordinateSpace);
            else
                _renderTransforms.Remove(uid);

            return;
        }

        // Consecutive state application uses the previous authoritative tick as its source.
        var targetTick = _timing.LastProcessedTick;
        var sourceTick = targetTick > GameTick.Zero
            ? targetTick - 1
            : targetTick;

        if (hasExisting
            && existing.Type == RenderInterpolationType.NetworkInterpolation
            && existing.NetworkTargetTick == targetTick
            && EndpointsEqualApprox(existing.Target, target))
        {
            return;
        }

        ref var networkState = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderTransforms, uid, out _);
        StartNetworkInterpolation(
            ref networkState,
            source,
            target,
            rendered,
            coordinateSpace,
            sourceTick,
            targetTick,
            snapRotation);
    }

    private void StartNetworkInterpolation(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace,
        GameTick sourceTick,
        GameTick targetTick,
        bool snapRotation)
    {
        // Source and target ticks define duration, including gaps caused by dropped states.
        state.Source = source;
        state.Target = target;
        state.LastRendered = rendered;
        state.CoordinateSpace = coordinateSpace;
        state.ChangeTick = sourceTick;
        state.Type = RenderInterpolationType.NetworkInterpolation;
        state.Alpha = 0f;
        state.InterpolationStartAlpha = 0f;
        state.LastFramePhase = -1f;
        state.NetworkTargetTick = targetTick;
        state.PendingPredictionRollback = false;
        state.SnapRotation = snapRotation;
        state.PredictionHandoffTick = GameTick.Zero;
        state.PredictionHandoff = PredictionHandoffStatus.Inactive;
        ClearCorrection(ref state);
    }

    private float GetNetworkInterpolationAlpha(ref RenderTransformState state)
    {
        var phase = _timing.TickPhase;
        var processedTick = _timing.LastProcessedTick;

        if (state.LastFrameProcessedTick == processedTick
            && state.LastFramePhase >= 0f
            && phase < state.LastFramePhase)
        {
            return 1f;
        }

        state.LastFramePhase = phase;
        state.LastFrameProcessedTick = processedTick;

        // Advance by authoritative tick span instead of compressing every segment into one tick.
        var span = state.NetworkTargetTick.Value - state.ChangeTick.Value;
        var wholeTicks = processedTick > state.ChangeTick
            ? processedTick.Value - state.ChangeTick.Value - 1
            : 0;
        var alpha = (wholeTicks + phase) / span;
        return Math.Max(state.Alpha, Math.Clamp(alpha, 0f, 1f));
    }

    internal void StartNetworkInterpolationLookahead(
        EntityUid uid,
        EntityCoordinates targetCoordinates,
        Angle targetRotation,
        GameTick sourceTick,
        GameTick targetTick)
    {
        // The game-state layer supplies a resolved future transform when an intermediate state is missing.
        if (targetTick <= sourceTick
            || !XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var source)
            || !TryCreateEndpoint(targetCoordinates, targetRotation, out var target))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        if (!TryGetCommonRenderSpace(source.RenderSpace, target.RenderSpace, out var coordinateSpace)
            || ClassifyInterpolation(source, target) != InterpolationDecision.Interpolate)
        {
            _renderTransforms.Remove(uid);
            return;
        }

        ref var existing = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        var hasExisting = !Unsafe.IsNullRef(ref existing);

        if (hasExisting
            && existing.Type == RenderInterpolationType.NetworkInterpolation
            && existing.ChangeTick == sourceTick
            && existing.NetworkTargetTick == targetTick
            && EndpointsEqualApprox(existing.Target, target))
        {
            return;
        }

        var rendered = hasExisting
            ? existing.LastRendered
            : ResolveLastRenderedEndpoint(source, 0);

        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderTransforms, uid, out _);
        StartNetworkInterpolation(
            ref state,
            source,
            target,
            rendered,
            coordinateSpace,
            sourceTick,
            targetTick,
            _snapRenderRotationEntities.Contains(uid));
    }

    internal bool TryGetNetworkInterpolationTicks(EntityUid uid, out GameTick sourceTick, out GameTick targetTick)
    {
        if (_renderTransforms.TryGetValue(uid, out var state)
            && state.Type == RenderInterpolationType.NetworkInterpolation)
        {
            sourceTick = state.ChangeTick;
            targetTick = state.NetworkTargetTick;
            return true;
        }

        sourceTick = default;
        targetTick = default;
        return false;
    }
}
