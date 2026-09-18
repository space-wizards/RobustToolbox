using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

public sealed partial class TransformSystem
{
    internal void BeginPredictionRollback(EntityUid uid, GameTick predictionTick)
    {
        // Prediction interpolation is the normal tick-to-tick render path for locally simulated movement.
        if (_predictionReconciliation.Rollback.Status != PredictionRollbackStatus.Inactive)
            return;

        if (!XformQuery.TryGetComponent(uid, out var xform) || xform.Deleted)
            return;

        // Keep the pose that was actually displayed while prediction is reset and run again.
        _predictionReconciliation.TryBeginRollback(
            uid,
            predictionTick,
            GetRenderTransformInternal((uid, xform), 0));
    }

    internal void CompletePredictionRollback(GameTick predictionTick)
    {
        // Wait until prediction reaches the exact tick represented by the saved endpoint.
        ref var rollback = ref _predictionReconciliation.Rollback;

        var explicitSnap = rollback.Status == PredictionRollbackStatus.HardReset;
        if (rollback.Status is not (PredictionRollbackStatus.Pending or PredictionRollbackStatus.HardReset)
            || predictionTick < rollback.Tick)
        {
            return;
        }

        if (predictionTick > rollback.Tick)
        {
            _predictionReconciliation.ClearRollback();
            return;
        }

        var uid = rollback.Entity;

        if (!XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var predictedEndpoint)
            || !TryGetCommonRenderSpace(
                rollback.Endpoint.RenderSpace,
                predictedEndpoint.RenderSpace,
                out _))
        {
            rollback.Status = PredictionRollbackStatus.HardReset;
            return;
        }

        switch (ClassifyInterpolation(rollback.Endpoint, predictedEndpoint))
        {
            case InterpolationDecision.Snap:
                rollback.Status = PredictionRollbackStatus.HardReset;
                break;
            case InterpolationDecision.Ignore:
                rollback.Status = PredictionRollbackStatus.Match;
                break;
            case InterpolationDecision.Interpolate:
                rollback.Status = explicitSnap
                    ? PredictionRollbackStatus.HardReset
                    : PredictionRollbackStatus.Mismatch;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal void FinishPredictionRollback()
    {
        if (_predictionReconciliation.Rollback.Status == PredictionRollbackStatus.Inactive)
            return;

        var rollback = _predictionReconciliation.TakeRollback();

        if (rollback.Status != PredictionRollbackStatus.Pending)
            CompleteSnappedRotationRollback(rollback.Entity, rollback.Tick);

        if (rollback.Status is PredictionRollbackStatus.Pending or PredictionRollbackStatus.HardReset)
        {
            if (rollback.Status == PredictionRollbackStatus.HardReset)
                _renderTransforms.Remove(rollback.Entity);

            return;
        }

        if (rollback.Status != PredictionRollbackStatus.Mismatch)
            return;

        // Layer the visible error over the newly predicted base pose.
        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, rollback.Entity);
        RenderTransform basePose;

        if (Unsafe.IsNullRef(ref state))
        {
            if (!XformQuery.TryGetComponent(rollback.Entity, out var xform)
                || xform.Deleted
                || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var endpoint))
            {
                return;
            }

            basePose = ResolveEndpoint(endpoint, 0);
            ref var newState = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderTransforms, rollback.Entity, out _);
            StartPredictionInterpolation(
                ref newState,
                endpoint,
                endpoint,
                rollback.Anchor,
                basePose.CoordinateSpace,
                false,
                _snapRenderRotationEntities.Contains(rollback.Entity));
            newState.Alpha = 1f;
            newState.InterpolationStartAlpha = 1f;
            state = ref newState;
        }
        else
        {
            basePose = GetBaseRenderTransform(in state, 0);
        }

        if (ExceedsMaxInterpolationDistance(rollback.Anchor, basePose))
        {
            _renderTransforms.Remove(rollback.Entity);
            return;
        }

        StartCorrection(ref state, rollback.Anchor, basePose);
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
            _predictionReconciliation.ClearSamples();
            return;
        }

        // The input sequence records which queued inputs were included in this endpoint.
        _predictionReconciliation.Record(uid, endpoint, predictionTick, inputSequence);
    }

    internal bool TryGetPredictionSample(
        EntityUid uid,
        bool previous,
        out GameTick predictionTick,
        out uint inputSequence)
    {
        return _predictionReconciliation.TryGetSample(
            uid,
            previous,
            out predictionTick,
            out inputSequence);
    }

    private void RefreshPredictionSample(EntityUid uid)
    {
        if (!_predictionReconciliation.TryGetLatestSample(uid, out var sample))
            return;

        // Transform helpers can change the endpoint after the regular end-of-tick sample.
        RecordPredictionSample(uid, _timing.CurTick, sample.InputSequence);
    }

    private void HandlePredictedTransformMove(
        EntityUid uid,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
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
                && existing.ChangeTick == _timing.CurTick
                && existing.Alpha <= existing.InterpolationStartAlpha
                && !existing.PendingPredictionRollback)
            {
                existing.Target = target;
                existing.CoordinateSpace = coordinateSpace;
                return;
            }

            var preserveCorrection = hasExisting && HasCorrection(in existing);
            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderTransforms, uid, out _);
            StartPredictionInterpolation(
                ref state,
                source,
                target,
                rendered,
                coordinateSpace,
                preserveCorrection,
                snapRotation);
            return;
        }

        if (hasExisting)
        {
            RebasePredictionInterpolation(ref existing, source, target, rendered, coordinateSpace);
            return;
        }

        // Rollback without render state must not interpolate from a temporary rollback position.
        _renderTransforms.Remove(uid);
    }

    private void StartPredictionInterpolation(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace,
        bool preserveCorrection,
        bool snapRotation)
    {
        // Correction is independent from this base interpolation and may continue across later ticks.
        state.Source = source;
        state.Target = target;
        state.LastRendered = rendered;
        state.CoordinateSpace = coordinateSpace;
        state.ChangeTick = _timing.CurTick;
        state.Type = RenderInterpolationType.PredictionInterpolation;
        state.Alpha = 0f;
        state.InterpolationStartAlpha = 0f;
        state.LastFramePhase = -1f;
        state.PendingPredictionRollback = false;
        state.SnapRotation = snapRotation;

        if (preserveCorrection)
            return;

        ClearCorrection(ref state);
    }

    private void RebasePredictionInterpolation(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace)
    {
        // Rollback updates the saved segment without starting a new visible interpolation.
        RebasePredictionState(ref state, source, target, rendered, coordinateSpace);
        state.Type = RenderInterpolationType.PredictionInterpolation;
    }

    private void RebasePredictionHandoff(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace)
    {
        // Buffered states can still be stale while prediction hands back to network presentation.
        RebasePredictionState(ref state, source, target, rendered, coordinateSpace);
        state.Type = RenderInterpolationType.PredictionInterpolation;
        state.PredictionHandoff = PredictionHandoffStatus.RebasePending;
    }

    private void RebasePredictionState(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace)
    {
        // Prediction rollback may revisit a previous simulation tick so keep it so the correction remains continuous.
        var newPredictionTick = _timing.InPrediction && _timing.CurTick > state.ChangeTick;

        if (newPredictionTick)
        {
            state.InterpolationStartAlpha = 0f;
            state.Alpha = 0f;
            state.LastFramePhase = -1f;
            state.ChangeTick = _timing.CurTick;
            state.PendingPredictionRollback = true;
        }

        if (!state.PendingPredictionRollback)
        {
            state.InterpolationStartAlpha = state.Alpha;
            state.PendingPredictionRollback = true;
        }

        state.Source = source;
        state.Target = target;
        state.CoordinateSpace = coordinateSpace;
        state.LastRendered = rendered;
    }

    private bool TryHandlePredictionHandoff(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace,
        InterpolationDecision decision)
    {
        // Authoritative updates own the segment while a predicted entity returns to server state.
        if (state.PredictionHandoff == PredictionHandoffStatus.Inactive)
            return false;

        if (_timing.ApplyingState)
        {
            RebasePredictionHandoff(ref state, source, target, rendered, coordinateSpace);
            return true;
        }

        if (decision != InterpolationDecision.Ignore)
            return false;

        state.LastRendered = rendered;
        return true;
    }

    internal void EndPrediction(EntityUid uid)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        if (Unsafe.IsNullRef(ref state)
            || state.Type != RenderInterpolationType.PredictionInterpolation
            || !XformQuery.TryGetComponent(uid, out var xform)
            || xform.Deleted
            || !TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var target)
            || !TryBindRenderTransformToParent(state.LastRendered, target.Parent, out var source)
            || !TryGetCommonRenderSpace(source.RenderSpace, target.RenderSpace, out var coordinateSpace))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        if (ClassifyInterpolation(source, target) == InterpolationDecision.Snap)
        {
            _renderTransforms.Remove(uid);
            return;
        }

        // Continue from the displayed pose until an authoritative segment replaces this handoff.
        var sourceTick = _timing.LastProcessedTick;
        StartPredictionHandoff(
            ref state,
            source,
            target,
            coordinateSpace,
            sourceTick);
    }

    private void StartPredictionHandoff(
        ref RenderTransformState state,
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target,
        EntityUid coordinateSpace,
        GameTick sourceTick)
    {
        // Keep the handoff alive until authoritative states reach the first unpredicted tick.
        var startingHandoff = state.PredictionHandoff == PredictionHandoffStatus.Inactive;
        var handoff = startingHandoff
            ? PredictionHandoffStatus.WaitingForAuthoritativeState
            : state.PredictionHandoff;
        var handoffTick = startingHandoff
            ? _timing.CurTick
            : state.PredictionHandoffTick;
        var correctionTranslation = state.CorrectionTranslation;
        var correctionRotation = state.CorrectionRotation;
        StartNetworkInterpolation(
            ref state,
            source,
            target,
            state.LastRendered,
            coordinateSpace,
            sourceTick,
            sourceTick + 1,
            state.SnapRotation);
        state.CorrectionTranslation = correctionTranslation;
        state.CorrectionRotation = correctionRotation;
        state.PredictionHandoffTick = handoffTick;
        state.PredictionHandoff = handoff;
    }

    private struct PredictionSample(
        EntityUid entity,
        RenderTransformEndpoint endpoint,
        GameTick tick,
        uint inputSequence)
    {
        public readonly EntityUid Entity = entity;
        public RenderTransformEndpoint Endpoint = endpoint;
        public readonly GameTick Tick = tick;
        public uint InputSequence = inputSequence;
    }

    private struct PredictionReconciliationTracker
    {
        public PredictionRollback Rollback;
        // Two samples cover the current comparison tick and the immediately preceding candidate.
        private PredictionSample _latestSample;
        private PredictionSample _previousSample;

        public bool TryBeginRollback(EntityUid uid, GameTick tick, in RenderTransform anchor)
        {
            // Reconciliation only compares endpoints from the same prediction tick.
            PredictionSample sample;
            if (_latestSample.Entity == uid && _latestSample.Tick == tick)
                sample = _latestSample;
            else if (_previousSample.Entity == uid && _previousSample.Tick == tick)
                sample = _previousSample;
            else
                return false;

            Rollback = new PredictionRollback(uid, sample.Endpoint, anchor, tick);
            return true;
        }

        public PredictionRollback TakeRollback()
        {
            var rollback = Rollback;
            Rollback = default;
            return rollback;
        }

        public void Clear()
        {
            Rollback = default;
            ClearSamples();
        }

        public void ClearRollback()
        {
            Rollback = default;
        }

        public void ClearSamples()
        {
            _latestSample = default;
            _previousSample = default;
        }

        public void MarkRollbackHardReset(EntityUid uid)
        {
            // Keep a real mismatch snapped when rollback finishes.
            if (Rollback.Entity == uid)
                Rollback.Status = PredictionRollbackStatus.HardReset;
        }

        public void Record(
            EntityUid uid,
            in RenderTransformEndpoint endpoint,
            GameTick tick,
            uint inputSequence)
        {
            if (_latestSample.Entity == uid && _latestSample.Tick == tick)
            {
                _latestSample.Endpoint = endpoint;
                _latestSample.InputSequence = inputSequence;
                return;
            }

            _previousSample = _latestSample;
            _latestSample = new PredictionSample(uid, endpoint, tick, inputSequence);
        }

        public bool TryGetSample(
            EntityUid uid,
            bool previous,
            out GameTick tick,
            out uint inputSequence)
        {
            var sample = previous ? _previousSample : _latestSample;
            if (sample.Entity == uid)
            {
                tick = sample.Tick;
                inputSequence = sample.InputSequence;
                return true;
            }

            tick = default;
            inputSequence = default;
            return false;
        }

        public bool TryGetLatestSample(EntityUid uid, out PredictionSample sample)
        {
            sample = _latestSample;
            return sample.Entity == uid;
        }

        public void RemoveEntity(EntityUid uid)
        {
            if (Rollback.Entity == uid)
                Rollback = default;

            if (_latestSample.Entity == uid || _previousSample.Entity == uid)
                ClearSamples();
        }
    }

    private struct PredictionRollback(
        EntityUid entity,
        RenderTransformEndpoint endpoint,
        RenderTransform anchor,
        GameTick tick)
    {
        public readonly EntityUid Entity = entity;
        public readonly RenderTransformEndpoint Endpoint = endpoint;
        public readonly RenderTransform Anchor = anchor;
        public readonly GameTick Tick = tick;
        public PredictionRollbackStatus Status = PredictionRollbackStatus.Pending;
    }

    private enum PredictionRollbackStatus : byte
    {
        Inactive,
        Pending,
        Match,
        Mismatch,
        HardReset,
    }

    private enum PredictionHandoffStatus : byte
    {
        Inactive,
        WaitingForAuthoritativeState,
        RebasePending,
    }
}

/// <summary>
/// Allows client features to redirect transform reconciliation to the entity currently controlled by the local player.
/// </summary>
[ByRefEvent]
public record struct GetPredictionReconciliationTargetEvent
{
    /// <summary>
    /// Entity whose predicted transform should be sampled and reconciled.
    /// </summary>
    public EntityUid Target;

    /// <summary>
    /// Initializes the query with the attached local entity as the default target.
    /// </summary>
    /// <param name="target">The default reconciliation target.</param>
    public GetPredictionReconciliationTargetEvent(EntityUid target)
    {
        Target = target;
    }
}
