using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

/// <summary>
/// Maintains the client-only positions used for rendering transform interpolation.
/// </summary>
[UsedImplicitly]
public sealed partial class TransformSystem : SharedTransformSystem
{
    /*
     * Okay so because our game runs at TPS, we need to be able to smoothly render entities A -> B
     * We also need to handle mispredicts (correction for translation OR rotation)
     *
     * RenderTransformState handles what the render position of a particular entity was so we can avoid mutating transformcomponent
     * itself and can just let it run in the simulation just fine.
     *
     * We also use it to store any corrections required so they can be adjusted over _correctionHalfLife time
     * (it's multiplicative so... half every time).
     *
     * Most of the complexity comes from handling:
     * - Dropped state buffer so we lerp from A -> C
     * - Handling mispredicted states with correction.
     */

    // Oh yeah the only reason the unsafe ref checks are everywhere is so we can access the direct dictionary value
    // by ref and avoid having to do multiple lookups to mutate it because they're STRUCTS.

    private const int MaxTransformDepth = 128;

    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IClientGameTiming _timing = default!;

    [ViewVariables]
    private readonly Dictionary<EntityUid, RenderTransformState> _renderTransforms = new();

    private readonly Dictionary<EntityUid, SnappedRenderRotation> _snapRenderRotations = new();
    private readonly HashSet<EntityUid> _snapRenderRotationEntities = new();
    private readonly HashSet<EntityUid> _snapRenderTransformAfterParentChange = new();
    // Stores the map before an explicit map snap request.
    private readonly Dictionary<EntityUid, EntityUid> _snapRenderTransformAfterMapChange = new();
    private readonly Dictionary<EntityUid, Angle> _renderRotationOverrides = new();
    private readonly HashSet<EntityUid> _remove = new();
    private float _maxInterpolationDistanceSquared;
    private float _minInterpolationDistanceSquared;
    private float _correctionHalfLife;
    private float _minCorrectionTranslationSquared;
    private float _minCorrectionRotation;

    private PredictionReconciliationTracker _predictionReconciliation;
    private PredictionReconciliationDebugData _lastPredictionReconciliation;

    public override void Initialize()
    {
        base.Initialize();
        OnGlobalMoveEvent += OnTransformMoved;

        Subs.CVar(_configuration, CVars.NetInterpMaxDistance, SetMaxInterpolationDistance, true);
        Subs.CVar(_configuration, CVars.NetInterpMinDistance, SetMinInterpolationDistance, true);
        Subs.CVar(_configuration, CVars.NetInterpCorrectionHalfLife, SetCorrectionHalfLife, true);
        Subs.CVar(_configuration, CVars.NetInterpCorrectionMinTranslation, SetMinCorrectionTranslation, true);
        Subs.CVar(_configuration, CVars.NetInterpCorrectionMinRotation, SetMinCorrectionRotation, true);
    }

    public override void Shutdown()
    {
        OnGlobalMoveEvent -= OnTransformMoved;
        _renderTransforms.Clear();
        _snapRenderRotations.Clear();
        _snapRenderRotationEntities.Clear();
        _snapRenderTransformAfterParentChange.Clear();
        _snapRenderTransformAfterMapChange.Clear();
        _renderRotationOverrides.Clear();
        _predictionReconciliation.Clear();
        _lastPredictionReconciliation = default;
        base.Shutdown();
    }

    /// <summary>
    /// Clears all cached render state.
    /// </summary>
    public void ResetRenderTransforms()
    {
        _renderTransforms.Clear();
        _snapRenderRotations.Clear();
        _snapRenderRotationEntities.Clear();
        _snapRenderTransformAfterParentChange.Clear();
        _snapRenderTransformAfterMapChange.Clear();
        _renderRotationOverrides.Clear();
        _remove.Clear();
        _predictionReconciliation.Clear();
        _lastPredictionReconciliation = default;
        var ev = new RenderTransformsResetEvent();
        RaiseLocalEvent(ref ev);
    }

    [SubscribeLocalEvent]
    private void OnTransformShutdown(EntityUid uid, TransformComponent component, ComponentShutdown args)
    {
        _renderTransforms.Remove(uid);
        _snapRenderRotations.Remove(uid);
        _snapRenderRotationEntities.Remove(uid);
        _snapRenderTransformAfterParentChange.Remove(uid);
        _snapRenderTransformAfterMapChange.Remove(uid);
        _renderRotationOverrides.Remove(uid);

        _predictionReconciliation.RemoveEntity(uid);
        if (_lastPredictionReconciliation.Entity == uid)
            _lastPredictionReconciliation = default;
    }


    private void SetMaxInterpolationDistance(float value)
    {
        value = Math.Max(0f, value);
        _maxInterpolationDistanceSquared = value * value;
    }

    private void SetMinInterpolationDistance(float value)
    {
        value = Math.Max(0f, value);
        _minInterpolationDistanceSquared = value * value;
    }

    private void SetCorrectionHalfLife(float value)
        => _correctionHalfLife = Math.Max(0f, value);

    private void SetMinCorrectionTranslation(float value)
    {
        value = Math.Max(0f, value);
        _minCorrectionTranslationSquared = value * value;
    }

    private void SetMinCorrectionRotation(float value)
        => _minCorrectionRotation = Math.Max(0f, value);

    private void OnTransformMoved(ref MoveEvent args)
    {
        var uid = args.Sender;
        var xform = args.Component;

        if (_snapRenderTransformAfterMapChange.Count != 0 &&
            _snapRenderTransformAfterMapChange.TryGetValue(uid, out var sourceMap) &&
            sourceMap != (xform.MapUid ?? EntityUid.Invalid))
        {
            _snapRenderTransformAfterMapChange.Remove(uid);
            SnapRenderTransform(uid);
            return;
        }

        if (_snapRenderTransformAfterParentChange.Remove(uid))
        {
            SnapRenderTransform(uid);
            return;
        }

        if (xform.Deleted || !TryCreateEndpoint(uid, args.NewPosition, args.NewRotation, out var target))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        UpdateSnappedRenderRotation(uid, target);

        if (!TryCreateEndpoint(uid, args.OldPosition, args.OldRotation, out var oldEndpoint))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        HandleTransformChange(uid, xform, oldEndpoint, target);
    }

    /// <summary>
    /// Selects the render path for a transform change.
    /// </summary>
    private void HandleTransformChange(
        EntityUid uid,
        TransformComponent xform,
        in RenderTransformEndpoint oldEndpoint,
        in RenderTransformEndpoint target)
    {
        ref var existing = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        var hasExisting = !Unsafe.IsNullRef(ref existing);
        var snapRotation = _snapRenderRotationEntities.Contains(uid);

        if (hasExisting)
            existing.SnapRotation = snapRotation;

        var source = oldEndpoint;
        RenderTransform rendered;

        if (hasExisting)
        {
            // Use the last pose that reached the screen.
            // If we cross parents or render spaces, change the relative
            // coordinates to be in the old parent's space.
            rendered = existing.LastRendered;

            var parentOrRenderSpaceChanged = oldEndpoint.Parent != target.Parent
                                             || oldEndpoint.RenderSpace != target.RenderSpace;
            var sameTickPredictionRollback = existing.Type == RenderInterpolationType.PredictionInterpolation
                                             && existing.ChangeTick == _timing.CurTick
                                             && (existing.PendingPredictionRollback || _timing.ApplyingState);
            var sameNetworkState = existing.Type == RenderInterpolationType.NetworkInterpolation
                                         && existing.NetworkTargetTick == _timing.LastProcessedTick
                                         && _timing.ApplyingState;
            // Multiple changes in one simulation tick retain the original source: A -> B -> C is A -> C.
            // Most notable with substepping or content systems touching it.
            if ((existing.ChangeTick == _timing.CurTick || sameNetworkState)
                && existing.Alpha <= existing.InterpolationStartAlpha
                && !existing.PendingPredictionRollback)
            {
                source = existing.Source;
            }
            else if ((parentOrRenderSpaceChanged || sameTickPredictionRollback)
                     && !TryBindRenderTransformToParent(rendered, oldEndpoint.Parent, out source))
            {
                source = oldEndpoint;
            }
        }
        else
        {
            rendered = ResolveLastRenderedEndpoint(oldEndpoint, 0);
        }

        if (!TryGetCommonRenderSpace(source.RenderSpace, target.RenderSpace, out var coordinateSpace))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        // Corrections never make a rejected transform segment eligible for interpolation.
        var decision = ClassifyInterpolation(source, target);
        if (decision == InterpolationDecision.Snap)
        {
            _renderTransforms.Remove(uid);
            return;
        }

        if (hasExisting && TryHandlePredictionHandoff(
                ref existing,
                source,
                target,
                rendered,
                coordinateSpace,
                decision))
        {
            return;
        }

        if (decision == InterpolationDecision.Ignore)
        {
            HandleIgnoredTransformMove(uid, target, rendered, coordinateSpace, hasExisting, ref existing, snapRotation);
            return;
        }

        if (!_timing.ApplyingState)
        {
            HandlePredictedTransformMove(
                uid,
                source,
                target,
                rendered,
                coordinateSpace,
                hasExisting,
                ref existing,
                snapRotation);
            return;
        }

        HandleAppliedTransformMove(
            uid,
            xform,
            source,
            target,
            rendered,
            coordinateSpace,
            hasExisting,
            ref existing,
            snapRotation);

    }

    private void HandleIgnoredTransformMove(
        EntityUid uid,
        in RenderTransformEndpoint target,
        in RenderTransform rendered,
        EntityUid coordinateSpace,
        bool hasExisting,
        ref RenderTransformState existing,
        bool snapRotation)
    {
        if (hasExisting && HasCorrection(in existing))
        {
            StartPredictionInterpolation(
                ref existing,
                target,
                target,
                rendered,
                coordinateSpace,
                true,
                snapRotation);
            existing.Alpha = 1f;
            existing.InterpolationStartAlpha = 1f;
            return;
        }

        _renderTransforms.Remove(uid);
    }

    private InterpolationDecision ClassifyInterpolation(
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target)
    {
        var sourcePose = ResolveEndpoint(source, 0);
        var targetPose = ResolveEndpoint(target, 0);
        var distance = Vector2.DistanceSquared(sourcePose.Position, targetPose.Position);

        if (distance >= _maxInterpolationDistanceSquared)
            return InterpolationDecision.Snap;

        if (distance <= _minInterpolationDistanceSquared
            && sourcePose.Rotation.EqualsApprox(targetPose.Rotation))
        {
            return InterpolationDecision.Ignore;
        }

        return InterpolationDecision.Interpolate;
    }

    private bool ExceedsMaxInterpolationDistance(in RenderTransform source, in RenderTransform target)
        => Vector2.DistanceSquared(source.Position, target.Position) >= _maxInterpolationDistanceSquared;

    private static bool EndpointsEqualApprox(in RenderTransformEndpoint a, in RenderTransformEndpoint b)
    {
        return a.Parent == b.Parent
               && a.RenderSpace == b.RenderSpace
               && a.LocalPosition.EqualsApprox(b.LocalPosition)
               && a.LocalRotation.EqualsApprox(b.LocalRotation);
    }

    private void StartCorrection(
        ref RenderTransformState state,
        in RenderTransform anchor,
        in RenderTransform basePose,
        bool correctTranslation = true)
    {
        // The base interpolation keeps advancing while this render-space error decays.
        state.CorrectionTranslation = correctTranslation
            ? anchor.Position - basePose.Position
            : Vector2.Zero;
        state.CorrectionRotation = state.SnapRotation
            ? Angle.Zero
            : Angle.ShortestDistance(basePose.Rotation, anchor.Rotation);
        state.LastRendered = anchor;
    }

    private static bool HasCorrection(in RenderTransformState state)
    {
        return state.CorrectionTranslation != Vector2.Zero
               || state.CorrectionRotation != Angle.Zero;
    }

    private static void ClearCorrection(ref RenderTransformState state)
    {
        state.CorrectionTranslation = Vector2.Zero;
        state.CorrectionRotation = Angle.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _remove.Clear();
        var correctionDecay = -1f;

        foreach (var (uid, _) in _renderTransforms)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
            if (Unsafe.IsNullRef(ref state))
                continue;

            if (!XformQuery.TryGetComponent(uid, out var xform) || xform.Deleted)
            {
                _remove.Add(uid);
                continue;
            }

            var correctionStarted = false;
            if (state.PredictionHandoff == PredictionHandoffStatus.RebasePending)
            {
                // State application and prediction have finished, so the network base is stable for this frame.
                state.Alpha = GetInterpolationAlpha(ref state);
                var basePose = GetBaseRenderTransform(in state, 0);
                if (ExceedsMaxInterpolationDistance(state.LastRendered, basePose))
                {
                    _remove.Add(uid);
                    continue;
                }

                StartCorrection(ref state, state.LastRendered, basePose);
                state.PredictionHandoff = PredictionHandoffStatus.WaitingForServerState;
                correctionStarted = true;
            }

            if (!correctionStarted && HasCorrection(in state))
            {
                state.PendingPredictionRollback = false;

                if (correctionDecay < 0f)
                {
                    correctionDecay = frameTime <= 0f
                        ? 1f
                        : _correctionHalfLife <= 0f
                            ? 0f
                            : MathF.Pow(0.5f, frameTime / _correctionHalfLife);
                }

                state.CorrectionTranslation *= correctionDecay;
                state.CorrectionRotation *= correctionDecay;
            }

            if (!correctionStarted)
                state.Alpha = GetInterpolationAlpha(ref state);

        }

        // Resolve poses after interpolation has advanced so moving parents are cached for children.
        foreach (var (uid, _) in _renderTransforms)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
            if (Unsafe.IsNullRef(ref state))
                continue;

            if (_remove.Contains(uid))
                continue;

            var xform = XformQuery.GetComponent(uid);
            var hasCorrection = HasCorrection(in state);
            if (state.PredictionHandoff != PredictionHandoffStatus.Inactive
                && _timing.LastRealTick >= state.PredictionHandoffTick)
            {
                state.PredictionHandoffTick = GameTick.Zero;
                state.PredictionHandoff = PredictionHandoffStatus.Inactive;
            }

            var keepPrediction = state.Type == RenderInterpolationType.PredictionInterpolation
                                 && xform.LastModifiedTick > _timing.LastRealTick;
            if (!hasCorrection
                && state.Alpha >= 1f
                && keepPrediction
                && TryCreateEndpoint(uid, xform.Coordinates, xform.LocalRotation, out var endpoint))
            {
                state.Source = endpoint;
                state.Target = endpoint;
                state.InterpolationStartAlpha = 1f;
            }

            state.LastRendered = GetRenderTransformInternal(uid, 0);

            if (state.Type == RenderInterpolationType.PredictionInterpolation)
                state.PendingPredictionRollback = false;

            if (hasCorrection
                && state.CorrectionTranslation.LengthSquared() < _minCorrectionTranslationSquared
                && Math.Abs(state.CorrectionRotation.Theta) < _minCorrectionRotation)
            {
                ClearCorrection(ref state);
                hasCorrection = false;
            }

            if (!hasCorrection
                && state.Alpha >= 1f
                && !keepPrediction
                && state.PredictionHandoff == PredictionHandoffStatus.Inactive)
            {
                _remove.Add(uid);
            }
        }

        foreach (var uid in _remove)
        {
            _renderTransforms.Remove(uid);
        }
    }

    private float GetInterpolationAlpha(ref RenderTransformState state)
    {
        if (_timing.TickPeriod <= TimeSpan.Zero)
            return 1f;

        if (state.Type == RenderInterpolationType.NetworkInterpolation
            && state.NetworkTargetTick > state.ChangeTick)
        {
            return GetNetworkInterpolationAlpha(ref state);
        }

        // Prediction runs ahead of ChangeTick; clamp old segments when the phase wraps.
        var phase = _timing.TickPhase;

        if (state.LastFramePhase >= 0f && phase < state.LastFramePhase)
            return 1f;

        state.LastFramePhase = phase;
        return Math.Max(state.Alpha, phase);
    }

    /// <summary>
    /// Gets the transform used for drawing.
    /// </summary>
    [Pure]
    public RenderTransform GetRenderWorldTransform(Entity<TransformComponent?> ent)
    {
        var pose = GetCanonicalRenderWorldTransform(ent);
        if (!pose.CoordinateSpace.IsValid())
            return pose;

        var ev = new RenderTransformResolvedEvent(ent.Owner, ent.Comp, pose);
        RaiseLocalEvent(ref ev);
        return ev.Pose;
    }

    /// <summary>
    /// Gets the transform interpolation pose before render presentation systems adjust it.
    /// </summary>
    [Pure]
    public RenderTransform GetCanonicalRenderWorldTransform(Entity<TransformComponent?> ent)
    {
        var uid = ent.Owner;
        var xform = ent.Comp;
        if (!XformQuery.Resolve(uid, ref xform, false))
            return default;

        return GetRenderTransformInternal((uid, xform), 0);
    }

    /// <summary>
    /// Gets the transform interpolation pose before render presentation systems adjust it.
    /// </summary>
    [Pure]
    public RenderTransform GetCanonicalRenderWorldTransform(EntityUid uid, TransformComponent? xform = null)
        => GetCanonicalRenderWorldTransform((uid, xform));

    /// <summary>
    /// Gets the transform used for drawing.
    /// </summary>
    [Pure]
    public RenderTransform GetRenderWorldTransform(EntityUid uid, TransformComponent? xform = null)
        => GetRenderWorldTransform((uid, xform));

    /// <summary>
    /// Gets an entity's rendered world position and rotation.
    /// </summary>
    [Pure]
    public (Vector2 WorldPosition, Angle WorldRotation) GetRenderWorldPositionRotation(
        Entity<TransformComponent?> ent)
    {
        var pose = GetRenderWorldTransform(ent);
        return (pose.Position, pose.Rotation);
    }

    /// <summary>
    /// Gets an entity's rendered world position and rotation.
    /// </summary>
    [Pure]
    public (Vector2 WorldPosition, Angle WorldRotation) GetRenderWorldPositionRotation(
        EntityUid uid,
        TransformComponent? xform = null)
        => GetRenderWorldPositionRotation((uid, xform));

    /// <summary>
    /// Gets an entity's rendered world position.
    /// </summary>
    [Pure]
    public Vector2 GetRenderWorldPosition(Entity<TransformComponent?> ent)
        => GetRenderWorldTransform(ent).Position;

    /// <summary>
    /// Gets an entity's rendered world position.
    /// </summary>
    [Pure]
    public Vector2 GetRenderWorldPosition(EntityUid uid, TransformComponent? xform = null)
        => GetRenderWorldPosition((uid, xform));

    /// <summary>
    /// Gets an entity's rendered world rotation.
    /// </summary>
    [Pure]
    public Angle GetRenderWorldRotation(Entity<TransformComponent?> ent)
        => GetRenderWorldTransform(ent).Rotation;

    /// <summary>
    /// Gets an entity's rendered world rotation.
    /// </summary>
    [Pure]
    public Angle GetRenderWorldRotation(EntityUid uid, TransformComponent? xform = null)
        => GetRenderWorldRotation((uid, xform));

    /// <summary>
    /// Gets an entity's rendered world matrix.
    /// </summary>
    [Pure]
    public Matrix3x2 GetRenderWorldMatrix(Entity<TransformComponent?> ent)
    {
        var pose = GetRenderWorldTransform(ent);
        return Matrix3Helpers.CreateTransform(pose.Position, pose.Rotation);
    }

    /// <summary>
    /// Gets an entity's rendered world matrix.
    /// </summary>
    [Pure]
    public Matrix3x2 GetRenderWorldMatrix(EntityUid uid, TransformComponent? xform = null)
        => GetRenderWorldMatrix((uid, xform));

    /// <summary>
    /// Gets a rendered world matrix and inverse from one render transform sample.
    /// </summary>
    [Pure]
    public static (Matrix3x2 WorldMatrix, Matrix3x2 InvWorldMatrix) GetRenderWorldMatrixWithInv(in RenderTransform pose)
        => (
            Matrix3Helpers.CreateTransform(pose.Position, pose.Rotation),
            Matrix3Helpers.CreateInverseTransform(pose.Position, pose.Rotation));

    /// <summary>
    /// Gets an entity's rendered world matrix and inverse from one render transform sample.
    /// </summary>
    [Pure]
    public (Matrix3x2 WorldMatrix, Matrix3x2 InvWorldMatrix) GetRenderWorldMatrixWithInv(Entity<TransformComponent?> ent)
    {
        var pose = GetRenderWorldTransform(ent);
        return GetRenderWorldMatrixWithInv(in pose);
    }

    /// <summary>
    /// Gets an entity's rendered world matrix and inverse from one render transform sample.
    /// </summary>
    [Pure]
    public (Matrix3x2 WorldMatrix, Matrix3x2 InvWorldMatrix) GetRenderWorldMatrixWithInv(EntityUid uid, TransformComponent? xform = null)
        => GetRenderWorldMatrixWithInv((uid, xform));

    /// <summary>
    /// Gets the inverse of an entity's rendered world matrix.
    /// </summary>
    [Pure]
    public Matrix3x2 GetInvRenderWorldMatrix(Entity<TransformComponent?> ent)
    {
        var pose = GetRenderWorldTransform(ent);
        return Matrix3Helpers.CreateInverseTransform(pose.Position, pose.Rotation);
    }

    /// <summary>
    /// Gets the inverse of an entity's rendered world matrix.
    /// </summary>
    [Pure]
    public Matrix3x2 GetInvRenderWorldMatrix(EntityUid uid, TransformComponent? xform = null)
        => GetInvRenderWorldMatrix((uid, xform));

    /// <summary>
    /// Gets an entity's rendered coordinates.
    /// </summary>
    [Pure]
    public MapCoordinates GetRenderMapCoordinates(Entity<TransformComponent?> ent)
    {
        var uid = ent.Owner;
        var xform = ent.Comp;
        if (!XformQuery.Resolve(uid, ref xform, false))
            return MapCoordinates.Nullspace;

        var pose = GetRenderWorldTransform((uid, xform));
        return GetRenderMapCoordinates(pose);
    }

    /// <summary>
    /// Converts an existing render transform sample to map coordinates.
    /// </summary>
    [Pure]
    public MapCoordinates GetRenderMapCoordinates(in RenderTransform pose)
        => TryGetRenderSpaceMapId(pose.CoordinateSpace, out var mapId)
            ? new MapCoordinates(pose.Position, mapId)
            : MapCoordinates.Nullspace;

    /// <summary>
    /// Gets an entity's rendered coordinates.
    /// </summary>
    [Pure]
    public MapCoordinates GetRenderMapCoordinates(EntityUid uid, TransformComponent? xform = null)
        => GetRenderMapCoordinates((uid, xform));

    /// <summary>
    /// Gets the render layer samples used to draw an entity.
    /// </summary>
    [Pure]
    public int GetRenderLayerSamples(
        Entity<TransformComponent?> ent,
        Span<RenderLayerSample> samples,
        IReadOnlySet<EntityUid>? visibleMaps = null)
    {
        if (samples.Length == 0)
            return 0;

        var uid = ent.Owner;
        var xform = ent.Comp;
        if (!XformQuery.Resolve(uid, ref xform, false))
            return 0;

        var ev = new RenderLayerSamplesEvent(uid, xform, samples.Length, visibleMaps);
        RaiseLocalEvent(ref ev);
        if (ev.Handled)
        {
            if (ev.Count > 0)
                samples[0] = ev.First;
            if (ev.Count > 1)
                samples[1] = ev.Second;
            return ev.Count;
        }

        var pose = GetRenderWorldTransform((uid, xform));
        if (!pose.CoordinateSpace.IsValid() ||
            visibleMaps != null && !visibleMaps.Contains(pose.CoordinateSpace))
        {
            return 0;
        }

        samples[0] = new RenderLayerSample(pose.CoordinateSpace, pose.Position, pose.Rotation, 1f, 0);
        return 1;
    }

    /// <summary>
    /// Gets the render layer samples used to draw an entity.
    /// </summary>
    [Pure]
    public int GetRenderLayerSamples(
        EntityUid uid,
        Span<RenderLayerSample> samples,
        TransformComponent? xform = null,
        IReadOnlySet<EntityUid>? visibleMaps = null)
        => GetRenderLayerSamples((uid, xform), samples, visibleMaps);

    /// <summary>
    /// Gets an entity's render sample for a specific layer map.
    /// </summary>
    [Pure]
    public bool TryGetRenderLayerSample(
        Entity<TransformComponent?> ent,
        EntityUid layerMap,
        out RenderLayerSample sample,
        IReadOnlySet<EntityUid>? visibleMaps = null)
    {
        Span<RenderLayerSample> samples = stackalloc RenderLayerSample[2];
        var count = GetRenderLayerSamples(ent, samples, visibleMaps);
        for (var i = 0; i < count; i++)
        {
            if (samples[i].Map != layerMap)
                continue;

            sample = samples[i];
            return true;
        }

        sample = default;
        return false;
    }

    /// <summary>
    /// Gets an entity's render sample for a specific layer map.
    /// </summary>
    [Pure]
    public bool TryGetRenderLayerSample(
        EntityUid uid,
        EntityUid layerMap,
        out RenderLayerSample sample,
        TransformComponent? xform = null,
        IReadOnlySet<EntityUid>? visibleMaps = null)
        => TryGetRenderLayerSample((uid, xform), layerMap, out sample, visibleMaps);

    /// <summary>
    /// Gets the rendered world transform for a specific layer map.
    /// </summary>
    [Pure]
    public bool TryGetRenderWorldTransformForLayer(
        Entity<TransformComponent?> ent,
        EntityUid layerMap,
        out RenderTransform pose,
        IReadOnlySet<EntityUid>? visibleMaps = null)
    {
        if (TryGetRenderLayerSample(ent, layerMap, out var sample, visibleMaps))
        {
            pose = GetRenderWorldTransform(ent) with
            {
                Position = sample.Position,
                Rotation = sample.Rotation,
                CoordinateSpace = sample.Map,
                ReferenceDepth = sample.Depth,
            };
            return true;
        }

        pose = default;
        return false;
    }

    /// <summary>
    /// Gets the rendered world transform for a specific layer map.
    /// </summary>
    [Pure]
    public bool TryGetRenderWorldTransformForLayer(
        EntityUid uid,
        EntityUid layerMap,
        out RenderTransform pose,
        TransformComponent? xform = null,
        IReadOnlySet<EntityUid>? visibleMaps = null)
        => TryGetRenderWorldTransformForLayer((uid, xform), layerMap, out pose, visibleMaps);

    private RenderTransform GetRenderTransformInternal(Entity<TransformComponent?> ent, int depth)
    {
        var uid = ent.Owner;
        var xform = ent.Comp;
        if (depth >= MaxTransformDepth || !XformQuery.Resolve(uid, ref xform, false))
            return default;

        if (_renderTransforms.TryGetValue(uid, out var state))
        {
            var snapRotation = state.SnapRotation || _snapRenderRotations.ContainsKey(uid);
            var pose = GetBaseRenderTransform(in state, depth + 1);

            if (!HasCorrection(in state))
                return ApplyRenderRotationOverride(uid, pose);

            pose = CreateRenderTransform(
                pose.Position + state.CorrectionTranslation,
                snapRotation
                    ? pose.Rotation
                    : pose.Rotation + state.CorrectionRotation,
                pose.CoordinateSpace,
                pose.SourceRenderSpace,
                pose.TargetRenderSpace,
                pose.RenderSpaceAlpha);

            return ApplyRenderRotationOverride(uid, pose);
        }

        var renderSpace = xform.MapUid ?? EntityUid.Invalid;
        if (!xform.ParentUid.IsValid())
        {
            var pose = CreateRenderTransform(xform.LocalPosition, xform.LocalRotation, renderSpace, renderSpace, renderSpace, 1f);
            return ApplyRenderRotationOverride(uid, pose);
        }

        var parent = GetRenderTransformInternal(xform.ParentUid, depth + 1);
        var childPose = CreateRenderTransform(
            parent.Position + parent.Rotation.RotateVec(xform.LocalPosition),
            parent.Rotation + xform.LocalRotation,
            parent.CoordinateSpace,
            parent.SourceRenderSpace,
            parent.TargetRenderSpace,
            parent.RenderSpaceAlpha);
        return ApplyRenderRotationOverride(uid, childPose);
    }

    private RenderTransform GetBaseRenderTransform(in RenderTransformState state, int depth)
    {
        var source = ResolveEndpoint(state.Source, depth);
        var targetPose = ResolveEndpoint(state.Target, depth);
        var alpha = GetSegmentAlpha(state);
        var pose = CreateRenderTransform(
            Vector2.Lerp(source.Position, targetPose.Position, alpha),
            state.SnapRotation
                ? targetPose.Rotation
                : Angle.Lerp(source.Rotation, targetPose.Rotation, alpha),
            state.CoordinateSpace,
            state.Source.RenderSpace,
            state.Target.RenderSpace,
            alpha);

        return pose;
    }

    private RenderTransform ResolveEndpoint(in RenderTransformEndpoint endpoint, int depth)
    {
        // Endpoints store local coordinates. Resolve through rendered parents so child interpolation follows
        // the same visual parent pose that will be drawn this frame.
        if (!endpoint.Parent.IsValid() || depth >= MaxTransformDepth)
        {
            return CreateRenderTransform(
                endpoint.LocalPosition,
                endpoint.LocalRotation,
                endpoint.RenderSpace,
                endpoint.RenderSpace,
                endpoint.RenderSpace,
                1f);
        }

        var parent = GetRenderTransformInternal(endpoint.Parent, depth + 1);
        return CreateRenderTransform(
            parent.Position + parent.Rotation.RotateVec(endpoint.LocalPosition),
            parent.Rotation + endpoint.LocalRotation,
            parent.CoordinateSpace,
            endpoint.RenderSpace,
            endpoint.RenderSpace,
            1f);
    }

    private static float GetSegmentAlpha(RenderTransformState state)
    {
        var remaining = 1f - state.InterpolationStartAlpha;
        if (remaining <= float.Epsilon)
            return 1f;

        return Math.Clamp((state.Alpha - state.InterpolationStartAlpha) / remaining, 0f, 1f);
    }

    private RenderTransform ResolveLastRenderedEndpoint(in RenderTransformEndpoint endpoint, int depth)
    {
        // Used when re-parenting across parents or render spaces. Sampling last-rendered parents avoids a
        // one-frame snap back to simulation coordinates during parent or render-space transitions.
        // Yes this tilted me for years.
        if (!endpoint.Parent.IsValid() || depth >= MaxTransformDepth)
        {
            return CreateRenderTransform(
                endpoint.LocalPosition,
                endpoint.LocalRotation,
                endpoint.RenderSpace,
                endpoint.RenderSpace,
                endpoint.RenderSpace,
                1f);
        }

        var parent = GetLastRenderedTransformInternal(endpoint.Parent, depth + 1);
        return CreateRenderTransform(
            parent.Position + parent.Rotation.RotateVec(endpoint.LocalPosition),
            parent.Rotation + endpoint.LocalRotation,
            parent.CoordinateSpace,
            endpoint.RenderSpace,
            endpoint.RenderSpace,
            1f);
    }

    private RenderTransform GetLastRenderedTransformInternal(EntityUid uid, int depth, TransformComponent? xform = null)
    {
        if (depth >= MaxTransformDepth || !XformQuery.Resolve(uid, ref xform, false))
            return default;

        if (_renderTransforms.TryGetValue(uid, out var state))
            return state.LastRendered;

        var renderSpace = xform.MapUid ?? EntityUid.Invalid;
        if (!xform.ParentUid.IsValid())
        {
            return CreateRenderTransform(xform.LocalPosition, xform.LocalRotation, renderSpace, renderSpace, renderSpace, 1f);
        }

        var parent = GetLastRenderedTransformInternal(xform.ParentUid, depth + 1);
        return CreateRenderTransform(
            parent.Position + parent.Rotation.RotateVec(xform.LocalPosition),
            parent.Rotation + xform.LocalRotation,
            parent.CoordinateSpace,
            parent.SourceRenderSpace,
            parent.TargetRenderSpace,
            parent.RenderSpaceAlpha);
    }

    private bool TryCreateEndpoint(
        EntityUid uid,
        in EntityCoordinates coordinates,
        Angle rotation,
        out RenderTransformEndpoint endpoint)
    {
        if (!coordinates.EntityId.IsValid()
            || !XformQuery.TryGetComponent(coordinates.EntityId, out var parent)
            || parent.MapUid is not { } renderSpace)
        {
            endpoint = default;
            return false;
        }

        endpoint = new RenderTransformEndpoint(coordinates.EntityId, coordinates.Position, rotation, renderSpace);
        return true;
    }

    private bool TryBindRenderTransformToParent(in RenderTransform pose, EntityUid parentUid, out RenderTransformEndpoint endpoint)
    {
        // Convert the current rendered world pose into the new parent's local space so interpolation can
        // continue after a parent change.
        if (!parentUid.IsValid()
            || !XformQuery.TryGetComponent(parentUid, out var parentXform)
            || parentXform.MapUid is not { } renderSpace)
        {
            endpoint = default;
            return false;
        }

        var parent = GetLastRenderedTransformInternal(parentUid, 0, parentXform);
        var localPosition = (-parent.Rotation).RotateVec(pose.Position - parent.Position);
        var localRotation = pose.Rotation - parent.Rotation;
        endpoint = new RenderTransformEndpoint(parentUid, localPosition, localRotation, renderSpace);
        return true;
    }

    private RenderTransform CreateRenderTransform(
        Vector2 canonicalPosition,
        Angle rotation,
        EntityUid coordinateSpace,
        EntityUid sourceRenderSpace,
        EntityUid targetRenderSpace,
        float renderSpaceAlpha)
    {
        return new RenderTransform(
            canonicalPosition,
            rotation,
            coordinateSpace,
            sourceRenderSpace,
            targetRenderSpace,
            renderSpaceAlpha);
    }

    /// <summary>
    /// Gets the stable coordinate space used to render between two render spaces.
    /// </summary>
    public bool TryGetCommonRenderSpace(EntityUid first, EntityUid second, out EntityUid commonSpace)
    {
        if (!first.IsValid() || !second.IsValid())
        {
            commonSpace = EntityUid.Invalid;
            return false;
        }

        if (first == second)
        {
            commonSpace = first;
            return true;
        }

        var args = new RenderSpaceCompatibilityEvent(first, second);
        RaiseLocalEvent(ref args);
        commonSpace = args.CommonSpace;
        return commonSpace.IsValid();
    }

    /// <summary>
    /// Checks whether two render spaces share a common coordinate space.
    /// </summary>
    [Pure]
    public bool AreRenderSpacesCompatible(EntityUid first, EntityUid second)
        => TryGetCommonRenderSpace(first, second, out _);

    private bool TryGetRenderSpaceMapId(EntityUid renderSpace, out MapId mapId)
    {
        if (renderSpace.IsValid() && XformQuery.TryGetComponent(renderSpace, out var xform))
        {
            mapId = xform.MapID;
            return mapId != MapId.Nullspace;
        }

        mapId = MapId.Nullspace;
        return false;
    }

    /// <summary>
    /// Discards active render interpolation for an entity and optionally its descendants.
    /// </summary>
    public void SnapRenderTransform(EntityUid uid, bool recursive = false)
    {
        _renderTransforms.Remove(uid);
        _snapRenderRotations.Remove(uid);
        _snapRenderTransformAfterParentChange.Remove(uid);
        _snapRenderTransformAfterMapChange.Remove(uid);
        _renderRotationOverrides.Remove(uid);
        _predictionReconciliation.MarkRollbackHardReset(uid);
        RefreshPredictionSample(uid);
        var ev = new RenderTransformSnappedEvent();
        RaiseLocalEvent(uid, ref ev);
        if (!recursive || !XformQuery.TryGetComponent(uid, out var xform))
            return;

        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            SnapRenderTransform(child, true);
        }
    }

    /// <summary>
    /// Discards render interpolation and suppresses the move event that follows a parent-change event.
    /// </summary>
    /// <remarks>
    /// Parent-change events are raised before the corresponding move event creates render interpolation.
    /// </remarks>
    public void SnapRenderTransformAfterParentChange(EntityUid uid, bool recursive = false)
    {
        SnapRenderTransform(uid, recursive);
        _snapRenderTransformAfterParentChange.Add(uid);
    }

    /// <summary>
    /// Snaps the entity when it next changes maps.
    /// </summary>
    public void SnapRenderTransformAfterMapChange(EntityUid uid, bool recursive = false)
    {
        if (!XformQuery.TryGetComponent(uid, out var xform))
            return;

        _snapRenderTransformAfterMapChange[uid] = xform.MapUid ?? EntityUid.Invalid;
        if (!recursive)
            return;

        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            SnapRenderTransformAfterMapChange(child, true);
        }
    }

    /// <summary>
    /// Enlarges a render query enough to include sparse poses whose simulation target lies outside the viewport.
    /// </summary>
    [Pure]
    public Box2 GetRenderCullingBounds(MapId mapId, in Box2Rotated bounds)
        => GetRenderCullingBounds(mapId, bounds.CalcBoundingBox());

    /// <summary>
    /// Enlarges a render query to include possible render interpolation offsets.
    /// </summary>
    [Pure]
    public Box2 GetRenderCullingBounds(MapId mapId, in Box2 bounds)
    {
        return bounds.Enlarged(MathF.Sqrt(_maxInterpolationDistanceSquared));
    }

    /// <summary>
    /// Enumerates active render interpolation state for debugging.
    /// </summary>
    internal IEnumerable<RenderTransformDebugData> GetRenderTransformDebugData()
    {
        foreach (var uid in _renderTransforms.Keys)
        {
            if (TryGetRenderTransformDebugData(uid, out var data))
                yield return data;
        }
    }

    /// <summary>
    /// Gets active render interpolation state for an entity.
    /// </summary>
    [Pure]
    public bool TryGetRenderTransformDebugData(EntityUid uid, out RenderTransformDebugData data)
    {
        if (!_renderTransforms.TryGetValue(uid, out var state)
            || !XformQuery.TryGetComponent(uid, out var xform))
        {
            data = default;
            return false;
        }

        var simulation = GetWorldPositionRotation(xform);
        var source = ResolveEndpoint(state.Source, 0);
        var target = ResolveEndpoint(state.Target, 0);
        var rendered = GetRenderTransformInternal((uid, xform), 0);
        var renderSpace = xform.MapUid ?? EntityUid.Invalid;
        data = new RenderTransformDebugData(
            uid,
            CreateRenderTransform(
                simulation.WorldPosition,
                simulation.WorldRotation,
                renderSpace,
                renderSpace,
                renderSpace,
                1f),
            rendered,
            source,
            target,
            state.Target.Parent,
            state.CoordinateSpace,
            state.Source.RenderSpace,
            state.Target.RenderSpace,
            state.Type,
            state.Alpha,
            state.CorrectionTranslation,
            state.CorrectionRotation,
            state.ChangeTick,
            state.NetworkTargetTick,
            state.InterpolationStartAlpha,
            state.LastFramePhase,
            state.PendingPredictionRollback,
            state.PredictionHandoff.ToString(),
            state.PredictionHandoffTick);
        return true;
    }

    private struct RenderTransformState
    {
        public RenderTransformEndpoint Source;
        public RenderTransformEndpoint Target;
        // The pose that actually reached the screen last frame.
        public RenderTransform LastRendered;
        // The stable space used while interpolating across parents or maps.
        public EntityUid CoordinateSpace;
        public GameTick ChangeTick;
        public RenderInterpolationType Type;
        public float Alpha;
        // Allows A -> B -> C in one tick to keep its already-rendered progress.
        public float InterpolationStartAlpha;
        public float LastFramePhase;
        public GameTick LastFrameProcessedTick;
        public GameTick NetworkTargetTick;
        public Vector2 CorrectionTranslation;
        public Angle CorrectionRotation;
        public bool PendingPredictionRollback;
        public bool SnapRotation;
        public GameTick PredictionHandoffTick;
        public PredictionHandoffStatus PredictionHandoff;
    }

    private enum InterpolationDecision : byte
    {
        Ignore,
        Interpolate,
        Snap,
    }
}

/// <summary>
/// A rendered transform and its coordinate-space ownership.
/// </summary>
/// <param name="Position">Rendered world position.</param>
/// <param name="Rotation">Rendered world rotation.</param>
/// <param name="CoordinateSpace">Map space used to resolve the canonical position.</param>
/// <param name="SourceRenderSpace">Map where the current render segment started.</param>
/// <param name="TargetRenderSpace">Map where the current render segment ends.</param>
/// <param name="RenderSpaceAlpha">Blend progress between source and target render spaces.</param>
/// <param name="CanonicalPosition">Rendered world position before presentation projection.</param>
/// <param name="AbsoluteZ">Rendered absolute z position.</param>
/// <param name="ReferenceDepth">Map depth used to project <see cref="Position"/>.</param>
public readonly record struct RenderTransform(
    Vector2 Position,
    Angle Rotation,
    EntityUid CoordinateSpace,
    EntityUid SourceRenderSpace,
    EntityUid TargetRenderSpace,
    float RenderSpaceAlpha,
    Vector2 CanonicalPosition,
    float AbsoluteZ,
    int ReferenceDepth)
{
    public RenderTransform(Vector2 position, Angle rotation, EntityUid renderSpace)
        : this(position, rotation, renderSpace, renderSpace, renderSpace, 1f)
    {
    }

    public RenderTransform(
        Vector2 position,
        Angle rotation,
        EntityUid coordinateSpace,
        EntityUid sourceRenderSpace,
        EntityUid targetRenderSpace,
        float renderSpaceAlpha)
        : this(
            position,
            rotation,
            coordinateSpace,
            sourceRenderSpace,
            targetRenderSpace,
            renderSpaceAlpha,
            position,
            0f,
            0)
    {
    }
}

/// <summary>
/// Raised after transform interpolation resolves a pose and before that pose reaches render callers.
/// </summary>
[ByRefEvent]
public struct RenderTransformResolvedEvent
{
    public readonly EntityUid Entity;
    public readonly TransformComponent? Transform;
    public RenderTransform Pose;

    public RenderTransformResolvedEvent(EntityUid entity, TransformComponent? transform, RenderTransform pose)
    {
        Entity = entity;
        Transform = transform;
        Pose = pose;
    }
}

/// <summary>
/// Raised when transform callers need the render samples for an entity.
/// </summary>
[ByRefEvent]
public struct RenderLayerSamplesEvent
{
    public readonly EntityUid Entity;
    public readonly TransformComponent? Transform;
    public readonly int Capacity;
    public readonly IReadOnlySet<EntityUid>? VisibleMaps;
    public bool Handled;
    public int Count;
    public RenderLayerSample First;
    public RenderLayerSample Second;

    public RenderLayerSamplesEvent(
        EntityUid entity,
        TransformComponent? transform,
        int capacity,
        IReadOnlySet<EntityUid>? visibleMaps)
    {
        Entity = entity;
        Transform = transform;
        Capacity = capacity;
        VisibleMaps = visibleMaps;
        Handled = false;
        Count = 0;
        First = default;
        Second = default;
    }

    public bool AddSample(in RenderLayerSample sample)
    {
        Handled = true;
        if (Count >= Capacity || Count >= 2)
            return false;

        if (Count == 0)
            First = sample;
        else
            Second = sample;

        Count++;
        return true;
    }
}

public readonly record struct RenderLayerSample(EntityUid Map, Vector2 Position, Angle Rotation, float Opacity, int Depth);

/// <summary>
/// Raised on an entity after its cached render interpolation is snapped.
/// </summary>
[ByRefEvent]
public readonly record struct RenderTransformSnappedEvent;

/// <summary>
/// Local transform endpoint used as an interpolation anchor.
/// </summary>
/// <param name="Parent">Parent entity the local position is relative to.</param>
/// <param name="LocalPosition">Position in parent space.</param>
/// <param name="LocalRotation">Rotation in parent space.</param>
/// <param name="RenderSpace">Map used for render-space compatibility.</param>
internal readonly record struct RenderTransformEndpoint(
    EntityUid Parent,
    Vector2 LocalPosition,
    Angle LocalRotation,
    EntityUid RenderSpace);

/// <summary>
/// The source of an active render interpolation.
/// </summary>
public enum RenderInterpolationType : byte
{
    NetworkInterpolation,
    PredictionInterpolation,
}

/// <summary>
/// Raised when transform interpolation needs a common coordinate space for two render spaces.
/// </summary>
[ByRefEvent]
public struct RenderSpaceCompatibilityEvent
{
    /// <summary>
    /// The source render space.
    /// </summary>
    public readonly EntityUid First;

    /// <summary>
    /// The destination render space.
    /// </summary>
    public readonly EntityUid Second;

    /// <summary>
    /// A common coordinate space, or invalid if these spaces are incompatible.
    /// </summary>
    public EntityUid CommonSpace;

    public RenderSpaceCompatibilityEvent(EntityUid first, EntityUid second)
    {
        First = first;
        Second = second;
        CommonSpace = EntityUid.Invalid;
    }
}

/// <summary>
/// Raised after all cached render interpolation is discarded.
/// </summary>
[ByRefEvent]
public readonly record struct RenderTransformsResetEvent;

/// <summary>
/// Diagnostic data for an active render interpolation.
/// </summary>
/// <param name="Simulation">Current simulation transform.</param>
/// <param name="Rendered">Transform currently reaching the renderer.</param>
/// <param name="Source">Interpolation source endpoint in world form.</param>
/// <param name="Target">Interpolation target endpoint in world form.</param>
/// <param name="CoordinateSpace">Stable space used by the active interpolation.</param>
/// <param name="CorrectionTranslation">Visible position error decaying over the base interpolation.</param>
public readonly record struct RenderTransformDebugData(
    EntityUid Entity,
    RenderTransform Simulation,
    RenderTransform Rendered,
    RenderTransform Source,
    RenderTransform Target,
    EntityUid Parent,
    EntityUid CoordinateSpace,
    EntityUid SourceRenderSpace,
    EntityUid TargetRenderSpace,
    RenderInterpolationType Type,
    float Alpha,
    Vector2 CorrectionTranslation,
    Angle CorrectionRotation,
    GameTick ChangeTick,
    GameTick NetworkTargetTick,
    float InterpolationStartAlpha,
    float LastFramePhase,
    bool PendingPredictionRollback,
    string PredictionHandoff,
    GameTick PredictionHandoffTick);
