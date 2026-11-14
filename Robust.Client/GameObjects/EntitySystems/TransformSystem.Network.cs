using System;
using System.Runtime.InteropServices;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    private void HandleAppliedTransformMove(
        EntityUid uid,
        TransformComponent xform,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransformEndpoint rendered,
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

            return;
        }

        // Consecutive state application uses the prior server tick as its source.
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
        in RenderTransformEndpoint rendered,
        EntityUid coordinateSpace,
        GameTick sourceTick,
        GameTick targetTick,
        bool snapRotation)
    {
        state = new RenderTransformState(source, target, rendered, coordinateSpace, sourceTick, snapRotation)
        {
            Type = RenderInterpolationType.NetworkInterpolation,
            NetworkTargetTick = targetTick,
        };
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
        // ClientGameStateManager can supply a resolved future transform when an intermediate state is missing so ignore it.
        if (!XformQuery.TryGetComponent(uid, out var xform) || xform.Deleted)
        {
            _renderTransforms.Remove(uid);
            return;
        }

        ref var existing = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        var hasExisting = !Unsafe.IsNullRef(ref existing);

        // Lookahead is only for remote movement.
        if (xform.LastModifiedTick > _timing.LastRealTick
            || HasComp<PredictedPhysicsComponent>(uid)
            || hasExisting && (existing.Type == RenderInterpolationType.PredictionInterpolation
                || existing.PredictionHandoff != PredictionHandoffStatus.Inactive
                || HasCorrection(in existing)))
        {
            return;
        }

        if (targetTick <= sourceTick
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var source)
            || !TryCreateEndpoint(targetCoordinates, targetRotation, out var target))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        if (!TryGetCommonRenderSpace(source.RenderSpace, target.RenderSpace, out var coordinateSpace)
            // A future parent may still be our child until the rest of its state is applied.
            // Wait for that hierarchy instead of introducing a render-only parenting cycle.
            || target.Parent != source.Parent && RenderDependsOn(target.Parent, uid, new())
            || ClassifyInterpolation(source, target) != InterpolationDecision.Interpolate)
        {
            _renderTransforms.Remove(uid);
            return;
        }

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
            : source;

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
