using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    private PredictionRollback _predictionRollback;
    // Two samples cover the current comparison tick and the immediately preceding candidate.
    private PredictionSample _latestPredictionSample;
    private PredictionSample _previousPredictionSample;

    internal void BeginPredictionRollback(EntityUid uid, GameTick predictionTick)
    {
        // Prediction interpolation is the normal tick-to-tick render path for locally simulated movement.
        if (_predictionRollback.Status != PredictionRollbackStatus.Inactive)
            return;

        if (!XformQuery.TryGetComponent(uid, out var xform) || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var endpoint))
            return;

        // Reconciliation only compares endpoints from the same prediction tick.
        var sample = _latestPredictionSample.Entity == uid && _latestPredictionSample.Tick == predictionTick
            ? _latestPredictionSample
            : _previousPredictionSample;
        if (sample.Entity != uid || sample.Tick != predictionTick)
            return;

        // Keep the pose that was actually displayed while prediction is reset and run again.
        _predictionRollback = new PredictionRollback(
            uid,
            sample.Endpoint,
            _renderTransforms.TryGetValue(uid, out var state)
                ? GetRenderedEndpoint(uid, in state, GetRenderTransformInternal(state.Target.Parent))
                : endpoint,
            predictionTick);
    }

    internal void CompletePredictionRollback(GameTick predictionTick)
    {
        // Wait until prediction reaches the exact tick represented by the saved endpoint.

        var explicitSnap = _predictionRollback.Status == PredictionRollbackStatus.HardReset;
        if (_predictionRollback.Status is not (PredictionRollbackStatus.Pending or PredictionRollbackStatus.HardReset)
            || predictionTick < _predictionRollback.Tick)
        {
            return;
        }

        if (predictionTick > _predictionRollback.Tick)
        {
            _predictionRollback = default;
            return;
        }

        var uid = _predictionRollback.Entity;

        if (!XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var predictedEndpoint)
            || !TryGetCommonRenderSpace(
                _predictionRollback.Endpoint.RenderSpace,
                predictedEndpoint.RenderSpace,
                out _))
        {
            _predictionRollback.Status = PredictionRollbackStatus.HardReset;
            return;
        }

        _predictionRollback.RepredictedEndpoint = predictedEndpoint;
        _predictionRollback.Status = ClassifyInterpolation(_predictionRollback.Endpoint, predictedEndpoint) switch
        {
            InterpolationDecision.Snap => PredictionRollbackStatus.HardReset,
            InterpolationDecision.Ignore => PredictionRollbackStatus.Match,
            InterpolationDecision.Interpolate => explicitSnap
                ? PredictionRollbackStatus.HardReset
                : PredictionRollbackStatus.Mismatch,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal void FinishPredictionRollback()
    {
        if (_predictionRollback.Status == PredictionRollbackStatus.Inactive)
            return;

        var rollback = _predictionRollback;
        _predictionRollback = default;

        if (rollback.Status != PredictionRollbackStatus.Pending)
            CompleteSnappedRotationRollback(rollback.Entity, rollback.Tick);

        if (rollback.Status == PredictionRollbackStatus.HardReset)
            _renderTransforms.Remove(rollback.Entity);

        if (rollback.Status != PredictionRollbackStatus.Mismatch)
            return;

        // Layer the visible error over the newly predicted base pose.
        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, rollback.Entity);
        RenderTransformEndpoint basePose;

        if (Unsafe.IsNullRef(ref state))
        {
            if (!XformQuery.TryGetComponent(rollback.Entity, out var xform)
                || xform.Deleted
                || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var endpoint))
            {
                return;
            }

            basePose = endpoint;
            ref var newState = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderTransforms, rollback.Entity, out _);
            StartPredictionInterpolation(
                ref newState,
                endpoint,
                endpoint,
                rollback.Anchor,
                basePose.RenderSpace,
                _snapRenderRotationEntities.Contains(rollback.Entity));
            newState.Alpha = 1f;
            newState.InterpolationStartAlpha = 1f;
            state = ref newState;
        }
        else
        {
            basePose = GetBaseRenderEndpoint(in state);
        }

        var anchor = rollback.Anchor;
        if (_timing.CurTick > rollback.Tick)
        {
            // First-time prediction has already advanced beyond the compared tick.
            // This also applies when releasing input produces no new MoveEvent:
            // ChangeTick then stays behind even though the grid keeps moving.
            // Preserve that movement and only smooth the error between matching endpoints;
            // pinning to the previous frame would cancel forward motion on every correction.
            var previous = ReparentEndpoint(rollback.Endpoint, basePose);
            var corrected = ReparentEndpoint(rollback.RepredictedEndpoint, basePose);
            anchor = basePose with
            {
                LocalPosition = basePose.LocalPosition + state.CorrectionTranslation + previous.LocalPosition - corrected.LocalPosition,
                LocalRotation = basePose.LocalRotation + state.CorrectionRotation +
                                Angle.ShortestDistance(corrected.LocalRotation, previous.LocalRotation)
            };
        }

        if (ExceedsMaxInterpolationDistance(anchor, basePose))
        {
            _renderTransforms.Remove(rollback.Entity);
            return;
        }

        StartCorrection(ref state, anchor, basePose);
    }

    private void CompleteSnappedRotationRollback(EntityUid uid, GameTick predictionTick)
    {
        if (!_snapRenderRotations.TryGetValue(uid, out var snapped)
            || snapped.ChangeTick > predictionTick)
        {
            return;
        }

        _snapRenderRotations.Remove(uid);

        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        if (Unsafe.IsNullRef(ref state)
            || !XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted)
        {
            return;
        }

        SnapRenderRotation(ref state);
    }

    internal void RecordPredictionSample(EntityUid uid, GameTick predictionTick, uint inputSequence)
    {
        if (!XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var endpoint))
        {
            _latestPredictionSample = _previousPredictionSample = default;
            return;
        }

        // The input sequence records which queued inputs were included in this endpoint.
        if (_latestPredictionSample.Entity != uid || _latestPredictionSample.Tick != predictionTick)
            _previousPredictionSample = _latestPredictionSample;

        _latestPredictionSample = new PredictionSample(uid, endpoint, predictionTick, inputSequence);
    }

    internal bool TryGetPredictionSample(
        EntityUid uid,
        bool previous,
        out GameTick predictionTick,
        out uint inputSequence)
    {
        var sample = previous ? _previousPredictionSample : _latestPredictionSample;
        var found = sample.Entity == uid;
        predictionTick = found ? sample.Tick : default;
        inputSequence = found ? sample.InputSequence : default;
        return found;
    }

    private void RefreshPredictionSample(EntityUid uid)
    {
        if (_latestPredictionSample.Entity != uid)
            return;

        // Transform helpers can change the endpoint after the normal end-of-tick sample.
        RecordPredictionSample(uid, _timing.CurTick, _latestPredictionSample.InputSequence);
    }

    private void HandlePredictedTransformMove(
        EntityUid uid,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransformEndpoint rendered,
        EntityUid coordinateSpace,
        bool hasExisting,
        ref RenderTransformState existing,
        bool snapRotation)
    {
        if (_timing.IsFirstTimePredicted)
        {
            // First-time prediction renders over the current tick. Rollback keeps the last rendered pose below.
            if (hasExisting
                && existing.Type == RenderInterpolationType.PredictionInterpolation
                && existing.PredictionHandoff == PredictionHandoffStatus.Inactive
                && existing.ChangeTick == _timing.CurTick
                && existing.Alpha <= existing.InterpolationStartAlpha
                && !existing.PendingPredictionRollback)
            {
                existing.CorrectionTranslation = ReparentCorrection(in existing, target.Parent);
                existing.Target = target;
                existing.CoordinateSpace = coordinateSpace;
                return;
            }

            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderTransforms, uid, out _);
            StartPredictionInterpolation(
                ref state,
                source,
                target,
                rendered,
                coordinateSpace,
                snapRotation);
            return;
        }

        if (hasExisting)
            RebasePredictionInterpolation(ref existing, source, target, rendered, coordinateSpace);
        // Without render state, leave the temporary rollback position uninterpolated.
    }

    private void StartPredictionInterpolation(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransformEndpoint rendered,
        EntityUid coordinateSpace,
        bool snapRotation)
    {
        // Correction follows the destination parent and may continue across later ticks.
        var correction = ReparentCorrection(in state, target.Parent);
        state = new RenderTransformState(source, target, rendered, coordinateSpace, _timing.CurTick, snapRotation)
        {
            Type = RenderInterpolationType.PredictionInterpolation,
            CorrectionTranslation = correction,
            CorrectionRotation = state.CorrectionRotation,
        };
    }

    private void RebasePredictionInterpolation(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransformEndpoint rendered,
        EntityUid coordinateSpace)
    {
        // A new prediction tick starts a new segment; replaying the same tick preserves progress.
        if (_timing.InPrediction && _timing.CurTick > state.ChangeTick)
        {
            state.InterpolationStartAlpha = 0f;
            state.Alpha = 0f;
            state.LastFramePhase = -1f;
            state.ChangeTick = _timing.CurTick;
        }
        else if (!state.PendingPredictionRollback)
        {
            state.InterpolationStartAlpha = state.Alpha;
        }

        state.PendingPredictionRollback = true;
        state.CorrectionTranslation = ReparentCorrection(in state, target.Parent);
        state.Source = source;
        state.Target = target;
        state.CoordinateSpace = coordinateSpace;
        state.LastRendered = rendered;
        state.Type = RenderInterpolationType.PredictionInterpolation;
    }

    private void RebasePredictionHandoff(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransformEndpoint rendered,
        EntityUid coordinateSpace)
    {
        // This is a new server segment, not another replay of the previous prediction tick.
        // Retaining the old alpha would leave every subsequent update at its endpoint.
        var handoffTick = state.PredictionHandoffTick;
        var targetTick = _timing.LastProcessedTick;
        StartNetworkInterpolation(
            ref state,
            source,
            target,
            rendered,
            coordinateSpace,
            targetTick > GameTick.Zero ? targetTick - 1 : targetTick,
            targetTick,
            state.SnapRotation);
        state.PredictionHandoffTick = handoffTick;
        state.PredictionHandoff = PredictionHandoffStatus.RebasePending;
    }

    internal void EndPrediction(EntityUid uid)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        // Stuff like pulling cancels use predictive events, which are run every tick until acknowledged. They shouldn't mess with
        // a handoff that already owns the rendered pose.
        if (Unsafe.IsNullRef(ref state)
            || state.PredictionHandoff != PredictionHandoffStatus.Inactive
            || state.Type != RenderInterpolationType.PredictionInterpolation)
        {
            return;
        }

        if (!XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var target)
            || !TryGetCommonRenderSpace(state.LastRendered.RenderSpace, target.RenderSpace, out var coordinateSpace))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        var source = ReparentEndpoint(state.LastRendered, target, lastFrame: true);
        if (ClassifyInterpolation(source, target) == InterpolationDecision.Snap)
        {
            _renderTransforms.Remove(uid);
            return;
        }

        var sourceTick = _timing.LastProcessedTick;
        StartNetworkInterpolation(
            ref state,
            source,
            target,
            state.LastRendered,
            coordinateSpace,
            sourceTick,
            sourceTick + 1,
            state.SnapRotation);
        state.PredictionHandoffTick = _timing.CurTick;
        state.PredictionHandoff = PredictionHandoffStatus.WaitingForServerState;
    }

    private readonly record struct PredictionSample(
        EntityUid Entity,
        RenderTransformEndpoint Endpoint,
        GameTick Tick,
        uint InputSequence);

    private struct PredictionRollback(
        EntityUid entity,
        RenderTransformEndpoint endpoint,
        RenderTransformEndpoint anchor,
        GameTick tick)
    {
        public readonly EntityUid Entity = entity;
        public readonly RenderTransformEndpoint Endpoint = endpoint;
        public readonly RenderTransformEndpoint Anchor = anchor;
        public readonly GameTick Tick = tick;
        public RenderTransformEndpoint RepredictedEndpoint = default;
        public PredictionRollbackStatus Status = PredictionRollbackStatus.Pending;
    }

    /// <summary>
    /// Tracks whether rollback prediction changed the saved transform.
    /// </summary>
    private enum PredictionRollbackStatus : byte
    {
        Inactive,
        Pending,
        Match,
        Mismatch,
        HardReset,
    }

    // This one's somewhat important so client predicting movement going back to server isn't immediately jerky.
    /// <summary>
    /// Tracks the transition from local prediction back to server movement.
    /// </summary>
    private enum PredictionHandoffStatus : byte
    {
        Inactive,
        WaitingForServerState,
        RebasePending,
    }
}

/// <summary>
/// Allows content to redirect transform reconciliation to the entity currently controlled by the local player.
/// </summary>
[ByRefEvent]
public record struct GetPredictionReconciliationTargetEvent(EntityUid Target)
{
    /// <summary>
    /// Entity whose predicted transform should be sampled.
    /// </summary>
    public EntityUid Target = Target;
}
