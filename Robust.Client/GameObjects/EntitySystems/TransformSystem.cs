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
    private readonly Dictionary<EntityUid, Angle> _renderRotationOverrides = new();
    private readonly HashSet<EntityUid> _remove = new();

    private float _maxInterpolationDistance;
    private float _maxInterpolationDistanceSquared;
    private float _minInterpolationDistanceSquared;
    private float _correctionHalfLife;
    private float _minCorrectionTranslationSquared;
    private float _minCorrectionRotation;

    private PredictionReconciliationTracker _predictionReconciliation;

    /// <summary>
    /// Invoked when interpolation crosses render spaces. The default policy only permits the same map.
    /// A client feature such as z-level rendering can mark other map pairs as compatible.
    /// </summary>
    public event RenderSpaceCompatibilityHandler? RenderSpaceCompatibility;

    public delegate void RenderSpaceCompatibilityHandler(ref RenderSpaceCompatibilityEvent args);

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
        _renderRotationOverrides.Clear();
        _predictionReconciliation.Clear();
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
        _renderRotationOverrides.Clear();
        _remove.Clear();
        _predictionReconciliation.Clear();
    }

    [SubscribeLocalEvent]
    private void OnTransformShutdown(EntityUid uid, TransformComponent component, ComponentShutdown args)
    {
        _renderTransforms.Remove(uid);
        _snapRenderRotations.Remove(uid);
        _snapRenderRotationEntities.Remove(uid);
        _snapRenderTransformAfterParentChange.Remove(uid);
        _renderRotationOverrides.Remove(uid);

        _predictionReconciliation.RemoveEntity(uid);
    }

    private void SetMaxInterpolationDistance(float value)
    {
        _maxInterpolationDistance = Math.Max(0f, value);
        _maxInterpolationDistanceSquared = _maxInterpolationDistance * _maxInterpolationDistance;
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

        if (_snapRenderTransformAfterParentChange.Remove(uid))
        {
            SnapRenderTransform(uid);
            return;
        }

        if (xform.Deleted || !TryCreateEndpoint(args.NewPosition, args.NewRotation, out var target))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        UpdateSnappedRenderRotation(uid, target);

        if (!TryCreateEndpoint(args.OldPosition, args.OldRotation, out var oldEndpoint))
        {
            _renderTransforms.Remove(uid);
            return;
        }

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
            // Essentially if we cross parents, like with grid-traversal or z-levels, we need to change the relative
            // coordinates to be in the old parent's space.
            rendered = existing.LastRendered;

            var parentOrRenderSpaceChanged = oldEndpoint.Parent != target.Parent
                                             || oldEndpoint.RenderSpace != target.RenderSpace;
            var sameTickPredictionRollback = existing.Type == RenderInterpolationType.PredictionInterpolation
                                             && existing.ChangeTick == _timing.CurTick
                                             && (existing.PendingPredictionRollback || _timing.ApplyingState);
            // Multiple changes in one simulation tick retain the original source: A -> B -> C is A -> C.
            // Most notable with substepping or content systems touching it.
            if (existing.ChangeTick == _timing.CurTick
                && existing.Alpha <= existing.InterpolationStartAlpha
                && !existing.PendingPredictionRollback)
            {
                source = existing.Source;
            }
            else if ((parentOrRenderSpaceChanged || sameTickPredictionRollback)
                     && !TryBindRenderTransformToParent(rendered, args.OldPosition.EntityId, out source))
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

        // Corrections never make a rejected transform segment... interpolatable, interpolable?
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

    private void StartCorrection(ref RenderTransformState state, in RenderTransform anchor, in RenderTransform basePose)
    {
        // The base interpolation keeps advancing while this render-space error decays.
        state.CorrectionTranslation = anchor.Position - basePose.Position;
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
                state.PredictionHandoff = PredictionHandoffStatus.WaitingForAuthoritativeState;
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
                && TryCreateEndpoint(xform.Coordinates, xform.LocalRotation, out var endpoint))
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
        var uid = ent.Owner;
        var xform = ent.Comp;
        if (!XformQuery.Resolve(uid, ref xform, false))
            return default;

        return GetRenderTransformInternal((uid, xform), 0);
    }

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
    /// Gets an entity's rendered world position.
    /// </summary>
    [Pure]
    public Vector2 GetRenderWorldPosition(Entity<TransformComponent?> ent)
        => GetRenderWorldTransform(ent).Position;

    /// <summary>
    /// Gets an entity's rendered world rotation.
    /// </summary>
    [Pure]
    public Angle GetRenderWorldRotation(Entity<TransformComponent?> ent)
        => GetRenderWorldTransform(ent).Rotation;

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
    /// Gets the inverse of an entity's rendered world matrix.
    /// </summary>
    [Pure]
    public Matrix3x2 GetInvRenderWorldMatrix(Entity<TransformComponent?> ent)
    {
        var pose = GetRenderWorldTransform(ent);
        return Matrix3Helpers.CreateInverseTransform(pose.Position, pose.Rotation);
    }

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
        return TryGetRenderSpaceMapId(pose.CoordinateSpace, out var mapId)
            ? new MapCoordinates(pose.Position, mapId)
            : MapCoordinates.Nullspace;
    }

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

            pose = new RenderTransform(
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
            var pose = new RenderTransform(xform.LocalPosition, xform.LocalRotation, renderSpace);
            return ApplyRenderRotationOverride(uid, pose);
        }

        var parent = GetRenderTransformInternal(xform.ParentUid, depth + 1);
        var childPose = parent with
        {
            Position = parent.Position + parent.Rotation.RotateVec(xform.LocalPosition),
            Rotation = parent.Rotation + xform.LocalRotation
        };
        return ApplyRenderRotationOverride(uid, childPose);
    }

    private RenderTransform GetBaseRenderTransform(in RenderTransformState state, int depth)
    {
        var source = ResolveEndpoint(state.Source, depth);
        var targetPose = ResolveEndpoint(state.Target, depth);
        var alpha = GetSegmentAlpha(state);
        return new RenderTransform(
            Vector2.Lerp(source.Position, targetPose.Position, alpha),
            state.SnapRotation
                ? targetPose.Rotation
                : Angle.Lerp(source.Rotation, targetPose.Rotation, alpha),
            state.CoordinateSpace,
            state.Source.RenderSpace,
            state.Target.RenderSpace,
            alpha);
    }

    private RenderTransform ResolveEndpoint(in RenderTransformEndpoint endpoint, int depth)
    {
        // Endpoints store local coordinates. Resolve through rendered parents so child interpolation follows
        // the same visual parent pose that will be drawn this frame.
        if (!endpoint.Parent.IsValid() || depth >= MaxTransformDepth)
            return new RenderTransform(endpoint.LocalPosition, endpoint.LocalRotation, endpoint.RenderSpace);

        var parent = GetRenderTransformInternal(endpoint.Parent, depth + 1);
        return new RenderTransform(
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
        // one-frame snap back to simulation coordinates during grid traversal or z-level transitions.
        // Yes this tilted me for years.
        if (!endpoint.Parent.IsValid() || depth >= MaxTransformDepth)
            return new RenderTransform(endpoint.LocalPosition, endpoint.LocalRotation, endpoint.RenderSpace);

        var parent = GetLastRenderedTransformInternal(endpoint.Parent, depth + 1);
        return new RenderTransform(
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
            return new RenderTransform(xform.LocalPosition, xform.LocalRotation, renderSpace);

        var parent = GetLastRenderedTransformInternal(xform.ParentUid, depth + 1);
        return parent with
        {
            Position = parent.Position + parent.Rotation.RotateVec(xform.LocalPosition),
            Rotation = parent.Rotation + xform.LocalRotation
        };
    }

    private bool TryCreateEndpoint(
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

        // This is really just here so we can lerp z-level movement cleanly.
        var args = new RenderSpaceCompatibilityEvent(first, second);
        RenderSpaceCompatibility?.Invoke(ref args);
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
        _renderRotationOverrides.Remove(uid);
        _predictionReconciliation.MarkRollbackHardReset(uid);
        RefreshPredictionSample(uid);

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
        => _maxInterpolationDistance > 0f
            ? bounds.Enlarged(_maxInterpolationDistance)
            : bounds;

    /// <summary>
    /// Enumerates active render interpolation state for debugging.
    /// </summary>
    public IEnumerable<RenderTransformDebugData> GetRenderTransformDebugData()
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
        data = new RenderTransformDebugData(
            uid,
            new RenderTransform(simulation.WorldPosition, simulation.WorldRotation, xform.MapUid ?? EntityUid.Invalid),
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
            state.CorrectionRotation);
        return true;
    }

    private struct RenderTransformState
    {
        public RenderTransformEndpoint Source;
        public RenderTransformEndpoint Target;
        public RenderTransform LastRendered;
        public EntityUid CoordinateSpace;
        public GameTick ChangeTick;
        public RenderInterpolationType Type;
        public float Alpha;
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
/// A rendered transform and its coordinate.
/// </summary>
public readonly record struct RenderTransform(
    Vector2 Position,
    Angle Rotation,
    EntityUid CoordinateSpace,
    EntityUid SourceRenderSpace,
    EntityUid TargetRenderSpace,
    float RenderSpaceAlpha)
{
    public RenderTransform(Vector2 position, Angle rotation, EntityUid renderSpace)
        : this(position, rotation, renderSpace, renderSpace, renderSpace, 1f)
    {
    }
}

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
    PredictionCorrection,
}

/// <summary>
/// Allows a renderer to supply a common coordinate space for two render spaces.
/// </summary>
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
/// Diagnostic data for an active render interpolation.
/// </summary>
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
    Angle CorrectionRotation);
