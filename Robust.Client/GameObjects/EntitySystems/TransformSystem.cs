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
using Robust.Shared.Map.Enumerators;
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
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IClientGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _maps = default!;

    // TODO: Jaggedarray or something
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

    /// <summary>
    /// Invoked when interpolation crosses render spaces. The default policy only permits the same map.
    /// A client feature such as z-level rendering can mark other map pairs as compatible and not SNAP.
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
        ResetRenderTransforms();
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
        _predictionRollback = default;
        _latestPredictionSample = _previousPredictionSample = default;
    }

    [SubscribeLocalEvent]
    private void OnTransformShutdown(EntityUid uid, TransformComponent component, ComponentShutdown args)
    {
        _renderTransforms.Remove(uid);
        _snapRenderRotations.Remove(uid);
        _snapRenderRotationEntities.Remove(uid);
        _snapRenderTransformAfterParentChange.Remove(uid);
        _renderRotationOverrides.Remove(uid);

        if (_predictionRollback.Entity == uid)
            _predictionRollback = default;

        if (_latestPredictionSample.Entity == uid || _previousPredictionSample.Entity == uid)
            _latestPredictionSample = _previousPredictionSample = default;
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

    [SubscribeLocalEvent]
    private void OnRenderParentChanged(ref EntParentChangedMessage args)
    {
        // The simulation hierarchy is acyclic, but an ancestor's old interpolation endpoint
        // can still refer to this entity. Drop that history before resolving the new hierarchy.
        HashSet<EntityUid>? visited = null;
        var parent = args.Transform.ParentUid;
        while (XformQuery.TryGetComponent(parent, out var xform))
        {
            if (_renderTransforms.ContainsKey(parent) || _snapRenderRotations.ContainsKey(parent))
            {
                visited ??= new HashSet<EntityUid>();
                visited.Clear();
                if (RenderDependsOn(parent, args.Entity, visited))
                    SnapRenderTransform(parent);
            }

            parent = xform.ParentUid;
        }
    }

    private bool RenderDependsOn(EntityUid uid, EntityUid dependency, HashSet<EntityUid> visited)
    {
        if (uid == dependency)
            return true;

        if (!visited.Add(uid) || !XformQuery.TryGetComponent(uid, out var xform))
            return false;

        // Include simulation parents too: removing a completed segment exposes that hierarchy.
        if (xform.ParentUid.IsValid() && RenderDependsOn(xform.ParentUid, dependency, visited))
            return true;

        if (_renderTransforms.TryGetValue(uid, out var state)
            && (RenderDependsOn(state.Source.Parent, dependency, visited)
                || RenderDependsOn(state.Target.Parent, dependency, visited)
                || RenderDependsOn(state.LastRendered.Parent, dependency, visited)))
        {
            return true;
        }

        return _snapRenderRotations.TryGetValue(uid, out var snapped)
               && RenderDependsOn(snapped.Parent, dependency, visited);
    }

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
        var rendered = oldEndpoint;

        if (hasExisting)
        {
            // Keep the last displayed local pose attached to its rendered parent.
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
            else if (parentOrRenderSpaceChanged || sameTickPredictionRollback)
            {
                source = ReparentEndpoint(rendered, oldEndpoint, lastFrame: true);
                source = source with
                {
                    LocalPosition = source.LocalPosition - ReparentCorrection(in existing, source.Parent),
                    LocalRotation = source.LocalRotation - existing.CorrectionRotation
                };
            }
        }

        if (!TryGetCommonRenderSpace(source.RenderSpace, target.RenderSpace, out var coordinateSpace))
        {
            _renderTransforms.Remove(uid);
            return;
        }

        // Reject teleports before carrying over prediction correction.
        var decision = ClassifyInterpolation(source, target);
        if (decision == InterpolationDecision.Snap)
        {
            _renderTransforms.Remove(uid);
            return;
        }

        if (hasExisting && existing.PredictionHandoff != PredictionHandoffStatus.Inactive)
        {
            if (_timing.ApplyingState)
            {
                RebasePredictionHandoff(ref existing, source, target, rendered, coordinateSpace);
                return;
            }

            if (decision == InterpolationDecision.Ignore)
            {
                existing.LastRendered = rendered;
                return;
            }
        }

        if (decision == InterpolationDecision.Ignore)
        {
            if (hasExisting && HasCorrection(in existing))
            {
                StartPredictionInterpolation(ref existing, target, target, rendered, coordinateSpace, snapRotation);
                existing.Alpha = existing.InterpolationStartAlpha = 1f;
            }
            else
            {
                _renderTransforms.Remove(uid);
            }
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

    private InterpolationDecision ClassifyInterpolation(
        in RenderTransformEndpoint source,
        in RenderTransformEndpoint target)
    {
        var relativeSource = ReparentEndpoint(source, target);
        var distance = Vector2.DistanceSquared(relativeSource.LocalPosition, target.LocalPosition);

        if (distance >= _maxInterpolationDistanceSquared)
            return InterpolationDecision.Snap;

        if (distance <= _minInterpolationDistanceSquared
            && relativeSource.LocalRotation.EqualsApprox(target.LocalRotation))
        {
            return InterpolationDecision.Ignore;
        }

        return InterpolationDecision.Interpolate;
    }

    private bool ExceedsMaxInterpolationDistance(in RenderTransformEndpoint source, in RenderTransformEndpoint target)
        => Vector2.DistanceSquared(ReparentEndpoint(source, target).LocalPosition, target.LocalPosition) >= _maxInterpolationDistanceSquared;

    private static bool EndpointsEqualApprox(in RenderTransformEndpoint a, in RenderTransformEndpoint b)
    {
        return a.Parent == b.Parent
               && a.RenderSpace == b.RenderSpace
               && a.LocalPosition.EqualsApprox(b.LocalPosition)
               && a.LocalRotation.EqualsApprox(b.LocalRotation);
    }

    private void StartCorrection(
        ref RenderTransformState state,
        in RenderTransformEndpoint anchor,
        in RenderTransformEndpoint basePose)
    {
        // Both the base interpolation and the decaying error follow the rendered parent.
        var localAnchor = ReparentEndpoint(anchor, basePose);
        state.CorrectionTranslation = localAnchor.LocalPosition - basePose.LocalPosition;
        state.CorrectionRotation = state.SnapRotation
            ? Angle.Zero
            : Angle.ShortestDistance(basePose.LocalRotation, localAnchor.LocalRotation);
        state.LastRendered = localAnchor;
    }

    private static bool HasCorrection(in RenderTransformState state)
    {
        return state.CorrectionTranslation != Vector2.Zero
               || state.CorrectionRotation != Angle.Zero;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _remove.Clear();
        var correctionDecay = frameTime <= 0f ? 1f
            : _correctionHalfLife <= 0f ? 0f
            : MathF.Pow(0.5f, frameTime / _correctionHalfLife);

        foreach (var uid in _renderTransforms.Keys)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);

            if (!XformQuery.TryGetComponent(uid, out var xform) || xform.Deleted)
            {
                _remove.Add(uid);
                continue;
            }

            if (state.PredictionHandoff != PredictionHandoffStatus.RebasePending && HasCorrection(in state))
            {
                state.PendingPredictionRollback = false;

                state.CorrectionTranslation *= correctionDecay;
                state.CorrectionRotation *= correctionDecay;
            }

            state.Alpha = GetInterpolationAlpha(ref state);
        }

        // Resolve poses after all parents' interpolation has advanced. Defer removal until both passes finish.
        foreach (var uid in _renderTransforms.Keys)
        {
            if (_remove.Contains(uid))
                continue;

            ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
            var xform = XformQuery.GetComponent(uid);
            if (state.PredictionHandoff == PredictionHandoffStatus.RebasePending)
            {
                // All parents have advanced before rebasing a handoff into their local space.
                var basePose = GetBaseRenderEndpoint(in state);
                if (ExceedsMaxInterpolationDistance(state.LastRendered, basePose))
                {
                    _remove.Add(uid);
                    continue;
                }

                StartCorrection(ref state, state.LastRendered, basePose);
                state.PredictionHandoff = PredictionHandoffStatus.WaitingForServerState;
            }

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

            var parent = GetRenderTransformInternal(state.Target.Parent);
            state.LastRendered = GetRenderedEndpoint(uid, in state, parent);

            if (state.Type == RenderInterpolationType.PredictionInterpolation)
                state.PendingPredictionRollback = false;

            if (hasCorrection
                && state.CorrectionTranslation.LengthSquared() < _minCorrectionTranslationSquared
                && Math.Abs(state.CorrectionRotation.Theta) < _minCorrectionRotation)
            {
                state.CorrectionTranslation = Vector2.Zero;
                state.CorrectionRotation = Angle.Zero;
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

        // GameLoop increments CurTick after simulation, so ChangeTick + 1 is still
        // the frame interpolating this movement. A later tick must finish it even
        // when a slow frame skips it.
        if (_timing.CurTick > state.ChangeTick + 1)
            return 1f;

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
        => GetRenderTransformInternal(ent.Owner, ent.Comp);

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
        var pose = GetRenderWorldTransform(ent);
        return TryGetRenderSpaceMapId(pose.CoordinateSpace, out var mapId)
            ? new MapCoordinates(pose.Position, mapId)
            : MapCoordinates.Nullspace;
    }

    private RenderTransform GetRenderTransformInternal(EntityUid uid, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform, false))
            return default;

        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_renderTransforms, uid);
        if (!Unsafe.IsNullRef(ref state))
        {
            var renderedParent = GetRenderTransformInternal(state.Target.Parent);
            var local = GetRenderedEndpoint(uid, in state, renderedParent);
            var pose = ResolveEndpoint(local, renderedParent);
            // An entity moving locally inherits its parent's transition between render spaces.
            // Only its own cross-space segment replaces that metadata.
            return state.Source.RenderSpace == state.Target.RenderSpace
                ? pose
                : pose with
                {
                    CoordinateSpace = state.CoordinateSpace,
                    SourceRenderSpace = state.Source.RenderSpace,
                    TargetRenderSpace = state.Target.RenderSpace,
                    RenderSpaceAlpha = GetSegmentAlpha(in state)
                };
        }

        var renderSpace = xform.MapUid ?? EntityUid.Invalid;
        var parent = xform.ParentUid.IsValid()
            ? GetRenderTransformInternal(xform.ParentUid)
            : new RenderTransform(Vector2.Zero, Angle.Zero, renderSpace);
        var endpoint = new RenderTransformEndpoint(xform.ParentUid, xform.LocalPosition, xform.LocalRotation, renderSpace);
        return ResolveEndpoint(ApplyRenderRotationOverride(uid, endpoint, parent), parent);
    }

    private RenderTransformEndpoint GetBaseRenderEndpoint(in RenderTransformState state)
        => GetBaseRenderEndpoint(in state, GetRenderTransformInternal(state.Target.Parent));

    private RenderTransformEndpoint GetBaseRenderEndpoint(in RenderTransformState state, in RenderTransform parent)
    {
        var alpha = GetSegmentAlpha(in state);
        // During a crossing, both endpoints continue following their own moving parents.
        // Convert the source into the destination frame before interpolating local coordinates.
        var source = state.Source.Parent == state.Target.Parent
            ? state.Source
            : ToLocalEndpoint(ResolveEndpoint(state.Source), state.Target, parent);
        return state.Target with
        {
            LocalPosition = Vector2.Lerp(source.LocalPosition, state.Target.LocalPosition, alpha),
            LocalRotation = state.SnapRotation
                ? state.Target.LocalRotation
                : Angle.Lerp(source.LocalRotation, state.Target.LocalRotation, alpha)
        };
    }

    private RenderTransformEndpoint GetRenderedEndpoint(EntityUid uid, in RenderTransformState state, in RenderTransform parent)
    {
        var local = GetBaseRenderEndpoint(in state, parent);
        local = local with
        {
            LocalPosition = local.LocalPosition + state.CorrectionTranslation,
            LocalRotation = local.LocalRotation + (state.SnapRotation ? Angle.Zero : state.CorrectionRotation)
        };
        return ApplyRenderRotationOverride(uid, local, parent);
    }

    private RenderTransform ResolveEndpoint(in RenderTransformEndpoint endpoint)
    {
        // Endpoints store local coordinates. Resolve through rendered parents so child interpolation follows
        // the same visual parent pose that will be drawn this frame.
        if (!endpoint.Parent.IsValid())
            return new RenderTransform(endpoint.LocalPosition, endpoint.LocalRotation, endpoint.RenderSpace);

        var parent = GetRenderTransformInternal(endpoint.Parent, null);
        return ResolveEndpoint(endpoint, parent);
    }

    private static RenderTransform ResolveEndpoint(in RenderTransformEndpoint endpoint, in RenderTransform parent)
    {
        return parent with
        {
            Position = parent.Position + parent.Rotation.RotateVec(endpoint.LocalPosition),
            Rotation = parent.Rotation + endpoint.LocalRotation
        };
    }

    private static float GetSegmentAlpha(in RenderTransformState state)
    {
        var remaining = 1f - state.InterpolationStartAlpha;
        if (remaining <= float.Epsilon)
            return 1f;

        return Math.Clamp((state.Alpha - state.InterpolationStartAlpha) / remaining, 0f, 1f);
    }

    private RenderTransform ResolveLastRenderedEndpoint(in RenderTransformEndpoint endpoint)
    {
        // Reparent using the same parent poses that were displayed with this local snapshot.
        if (!endpoint.Parent.IsValid())
            return new RenderTransform(endpoint.LocalPosition, endpoint.LocalRotation, endpoint.RenderSpace);

        var parent = GetLastRenderedTransformInternal(endpoint.Parent);
        return ResolveEndpoint(endpoint, parent);
    }

    private RenderTransform GetLastRenderedTransformInternal(EntityUid uid, TransformComponent? xform = null)
    {
        if (!XformQuery.Resolve(uid, ref xform, false))
            return default;

        if (_renderTransforms.TryGetValue(uid, out var state))
            return ResolveLastRenderedEndpoint(state.LastRendered);

        var renderSpace = xform.MapUid ?? EntityUid.Invalid;
        if (!xform.ParentUid.IsValid())
            return new RenderTransform(xform.LocalPosition, xform.LocalRotation, renderSpace);

        var parent = GetLastRenderedTransformInternal(xform.ParentUid);
        return ResolveEndpoint(new RenderTransformEndpoint(
            xform.ParentUid, xform.LocalPosition, xform.LocalRotation, renderSpace), parent);
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

    private RenderTransformEndpoint ReparentEndpoint(
        in RenderTransformEndpoint endpoint,
        in RenderTransformEndpoint target,
        bool lastFrame = false)
    {
        if (endpoint.Parent == target.Parent)
            return endpoint with { RenderSpace = target.RenderSpace };

        var pose = lastFrame ? ResolveLastRenderedEndpoint(endpoint) : ResolveEndpoint(endpoint);
        var parent = lastFrame
            ? GetLastRenderedTransformInternal(target.Parent)
            : GetRenderTransformInternal(target.Parent);
        return ToLocalEndpoint(pose, target, parent);
    }

    private static RenderTransformEndpoint ToLocalEndpoint(
        in RenderTransform pose,
        in RenderTransformEndpoint target,
        in RenderTransform parent)
    {
        var position = (-parent.Rotation).RotateVec(pose.Position - parent.Position);
        return target with
        {
            LocalPosition = position,
            LocalRotation = pose.Rotation - parent.Rotation
        };
    }

    private Vector2 ReparentCorrection(in RenderTransformState state, EntityUid parent)
    {
        if (state.Target.Parent == parent || state.CorrectionTranslation == Vector2.Zero)
            return state.CorrectionTranslation;

        var oldRotation = GetLastRenderedTransformInternal(state.Target.Parent).Rotation;
        var newRotation = GetLastRenderedTransformInternal(parent).Rotation;
        return (oldRotation - newRotation).RotateVec(state.CorrectionTranslation);
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
        if (_predictionRollback.Entity == uid)
            _predictionRollback.Status = PredictionRollbackStatus.HardReset;
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
    /// Enlarges a render query enough to include entities whose simulation target lies outside the viewport.
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
        var paddedBounds = _maxInterpolationDistance > 0f
            ? bounds.Enlarged(_maxInterpolationDistance)
            : bounds;
        var queryBounds = paddedBounds;

        // Grid lookup uses simulation poses so need to fudge it a bit to include them.
        foreach (var grid in _maps.GetAllGrids(mapId))
        {
            var renderMatrix = GetRenderWorldMatrix(grid.Owner);
            var simulationMatrix = GetWorldMatrix(grid.Owner);
            if (renderMatrix == simulationMatrix)
                continue;

            Matrix3x2.Invert(renderMatrix, out var inverseRenderMatrix);
            var localBounds = inverseRenderMatrix.TransformBox(paddedBounds);
            if (!localBounds.Intersects(grid.Comp.LocalAABB))
                continue;

            queryBounds = queryBounds.Union(simulationMatrix.TransformBox(localBounds));
        }

        return queryBounds;
    }

    /// <summary>
    /// Gets grid chunks intersecting world bounds at the grid's rendered pose.
    /// </summary>
    /// <param name="grid">The grid to query.</param>
    /// <param name="worldBounds">The visible area in world space.</param>
    /// <param name="renderMatrix">The drawing matrix, including any pixel snapping adjustment.
    /// Defaults to the grid's render world matrix.</param>
    internal ChunkEnumerator GetRenderGridChunks(
        Entity<MapGridComponent> grid,
        in Box2Rotated worldBounds,
        Matrix3x2? renderMatrix = null)
    {
        var matrix = renderMatrix ?? GetRenderWorldMatrix(grid.Owner);
        Matrix3x2.Invert(matrix, out var inverseMatrix);
        var localBounds = inverseMatrix.TransformBox(worldBounds);
        return _maps.GetLocalMapChunks(grid.Owner, grid.Comp, localBounds);
    }

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
        var source = ResolveEndpoint(state.Source);
        var target = ResolveEndpoint(state.Target);
        var rendered = GetRenderTransformInternal(uid, xform);
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

    private struct RenderTransformState(
        RenderTransformEndpoint source,
        RenderTransformEndpoint target,
        RenderTransformEndpoint rendered,
        EntityUid coordinateSpace,
        GameTick changeTick,
        bool snapRotation)
    {
        public RenderTransformEndpoint Source = source;
        public RenderTransformEndpoint Target = target;
        // Snapshots and correction offsets are local, just like interpolation endpoints.
        public RenderTransformEndpoint LastRendered = rendered;
        public EntityUid CoordinateSpace = coordinateSpace;
        public GameTick ChangeTick = changeTick;
        public RenderInterpolationType Type;
        public float Alpha;
        public float InterpolationStartAlpha;
        public float LastFramePhase = -1f;
        public GameTick LastFrameProcessedTick;
        public GameTick NetworkTargetTick;
        // Expressed in Target.Parent's coordinates.
        public Vector2 CorrectionTranslation;
        public Angle CorrectionRotation;
        public bool PendingPredictionRollback;
        public bool SnapRotation = snapRotation;
        public GameTick PredictionHandoffTick;
        public PredictionHandoffStatus PredictionHandoff;
    }

    // How to handle the difference between two transforms.
    private enum InterpolationDecision : byte
    {
        // The change is too small to need smoothing.
        Ignore,
        // Smoothly move from the source to the target.
        Interpolate,
        // The change is too large to smooth; jump to the target.
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
    // Smooth movement between server updates.
    NetworkInterpolation,
    // Smooth locally predicted movement between simulation ticks.
    PredictionInterpolation,
}

// This is really just here for z-levels so lerping can smoothly go across maps.
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
/// Debug data for an active render interpolation.
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
