using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace Robust.Client.GameObjects;

public sealed partial class ClientZLevelSystem : EntitySystem
{
    [Dependency] private IClientGameTiming _timing = default!;
    [Dependency] private MapSystem _maps = default!;
    [Dependency] private TransformSystem _transforms = default!;
    [Dependency] private ZLevelSystem _zLevels = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery;
    [Dependency] private EntityQuery<ZLevelPositionComponent> _positionQuery;

    private readonly Dictionary<EntityUid, ZLevelVisuals> _visuals = new();
    private readonly Dictionary<EntityUid, HeightInterpolation> _heights = new();
    private readonly Dictionary<EntityUid, EntityUid> _entityMaps = new();
    private readonly Dictionary<EntityUid, EntityUid> _continuousSourceMaps = new();
    private readonly HashSet<EntityUid> _remove = new();
    private readonly HashSet<EntityUid> _snap = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(TransformSystem));
        UpdatesBefore.Add(typeof(EyeSystem));
    }

    public override void Shutdown()
    {
        _visuals.Clear();
        _heights.Clear();
        _entityMaps.Clear();
        _continuousSourceMaps.Clear();
        _remove.Clear();
        _snap.Clear();
        base.Shutdown();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        _remove.Clear();
        _snap.Clear();
        foreach (var uid in _heights.Keys)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_heights, uid);
            if (Unsafe.IsNullRef(ref state))
                continue;

            if (!EnsureInitialized(uid, ref state))
            {
                _remove.Add(uid);
                continue;
            }

            if (state.PendingMapValidation)
            {
                if (!state.ContinuousMapMove)
                {
                    _snap.Add(uid);
                    _remove.Add(uid);
                    continue;
                }

                state.PendingMapValidation = false;
            }

            UpdateAlpha(ref state);
            var keepPrediction = state.Type == RenderInterpolationType.PredictionInterpolation &&
                                 state.CreatedTick > _timing.LastRealTick;
            if (state.Alpha >= 1f && !keepPrediction)
                _remove.Add(uid);
        }

        foreach (var uid in _snap)
        {
            _transforms.SnapRenderTransform(uid);
        }

        foreach (var uid in _remove)
        {
            _heights.Remove(uid);
        }

        // Map and height events for this frame have been done. Do not reuse a crossing's
        // source map for an unrelated height change on a later frame to avoid nightmare mispredicts.
        _continuousSourceMaps.Clear();
    }

    [SubscribeLocalEvent]
    private void OnRenderSpaceCompatibility(ref RenderSpaceCompatibilityEvent args)
    {
        if (_zLevels.TryGetMapDepthOffset(args.First, args.Second, out _))
            args.CommonSpace = args.Second;
    }

    [SubscribeLocalEvent]
    private void OnRenderTransformsReset(ref RenderTransformsResetEvent args)
    {
        _heights.Clear();
        _continuousSourceMaps.Clear();
    }

    [SubscribeLocalEvent]
    private void OnRenderTransformResolved(ref RenderTransformResolvedEvent args)
    {
        args.Pose = GetProjectedRenderTransform(args.Entity, args.Transform, args.Pose);
    }

    [SubscribeLocalEvent]
    private void OnRenderLayerSamples(ref RenderLayerSamplesEvent args)
    {
        args.Handled = true;
        Span<RenderLayerSample> samples = stackalloc RenderLayerSample[2];
        var count = GetRenderLayerSamples((args.Entity, args.Transform), samples, args.VisibleMaps);
        for (var i = 0; i < count; i++)
        {
            args.AddSample(samples[i]);
        }
    }

    [SubscribeLocalEvent]
    private void OnRenderTransformSnapped(Entity<ZLevelPositionComponent> entity, ref RenderTransformSnappedEvent args)
    {
        ClearHeightInterpolation(entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnPositionChanged(Entity<ZLevelPositionComponent> entity, ref ZLevelPositionChangedEvent args)
    {
        var hasExisting = _heights.TryGetValue(entity.Owner, out var existing);
        // Rewinding simulation must not replace the segment already being displayed.
        if (hasExisting && existing.Type == RenderInterpolationType.PredictionInterpolation &&
            _timing.CurTick < existing.CreatedTick)
            return;

        if (args.ContinuousMapMove && _heights.TryGetValue(entity.Owner, out var pending))
        {
            pending.ContinuousMapMove = true;
            if (_continuousSourceMaps.TryGetValue(entity.Owner, out var markerSourceMap))
            {
                pending.SourceMap = markerSourceMap;
                if (!pending.Initialized)
                    pending.SourceExplicit = false;
            }
            _heights[entity.Owner] = pending;
            _continuousSourceMaps.Remove(entity.Owner);
        }
        else if (args.ContinuousMapMove &&
                 !_continuousSourceMaps.ContainsKey(entity.Owner) &&
                 _xformQuery.TryComp(entity.Owner, out var movedXform))
        {
            var movedPose = _transforms.GetCanonicalRenderWorldTransform((entity.Owner, movedXform));
            if (movedPose.SourceRenderSpace.IsValid())
                _continuousSourceMaps[entity.Owner] = movedPose.SourceRenderSpace;
        }

        if (args.OldHeight.Equals(args.NewHeight) || !_xformQuery.TryComp(entity.Owner, out var xform))
            return;

        hasExisting = _heights.TryGetValue(entity.Owner, out existing);
        var matchesTarget = hasExisting &&
                            existing.TargetLocalHeight.Equals(args.NewHeight) &&
                            xform.MapUid == existing.TargetMap;
        if (matchesTarget &&
            (existing.Type == RenderInterpolationType.NetworkInterpolation ||
             !_timing.IsFirstTimePredicted && existing.CreatedTick == _timing.CurTick))
        {
            _continuousSourceMaps.Remove(entity.Owner);
            _entityMaps[entity.Owner] = existing.TargetMap;
            return;
        }

        var preserveSource = hasExisting &&
                             existing.CreatedTick == _timing.CurTick &&
                             existing.Alpha <= ZLevelProjection.BoundaryEpsilon;
        if (preserveSource)
            EnsureInitialized(entity.Owner, ref existing);
        var sourceZ = preserveSource
            ? existing.Source
            : hasExisting
                ? GetRenderAbsoluteZ(entity.Owner, xform)
                : 0f;
        var targetMap = xform.MapUid ?? EntityUid.Invalid;
        var pose = _transforms.GetCanonicalRenderWorldTransform((entity.Owner, xform));
        var sourceMap = preserveSource
            ? existing.SourceMap
            : _continuousSourceMaps.TryGetValue(entity.Owner, out var continuousSource)
                ? continuousSource
                : pose.SourceRenderSpace.IsValid()
                    ? pose.SourceRenderSpace
                    : _entityMaps.TryGetValue(entity.Owner, out var previousMap)
                        ? previousMap
                        : targetMap;
        _continuousSourceMaps.Remove(entity.Owner);

        _heights[entity.Owner] = new HeightInterpolation
        {
            Source = sourceZ,
            SourceExplicit = hasExisting,
            SourceLocalHeight = preserveSource ? existing.SourceLocalHeight : args.OldHeight,
            TargetLocalHeight = args.NewHeight,
            LastPhase = -1f,
            CreatedTick = _timing.CurTick,
            Type = preserveSource ? existing.Type : _timing.ApplyingState ? RenderInterpolationType.NetworkInterpolation : RenderInterpolationType.PredictionInterpolation,
            ContinuousMapMove = args.ContinuousMapMove,
            SourceMap = sourceMap,
            TargetMap = targetMap,
        };
        _entityMaps[entity.Owner] = targetMap;
    }

    [SubscribeLocalEvent]
    private void OnPositionInit(Entity<ZLevelPositionComponent> entity, ref ComponentInit args)
    {
        if (_xformQuery.TryComp(entity.Owner, out var xform))
            _entityMaps[entity.Owner] = xform.MapUid ?? EntityUid.Invalid;
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<ZLevelPositionComponent> entity, ref MoveEvent args)
    {
        var targetMap = args.Component.MapUid ?? EntityUid.Invalid;
        if (args.ParentChanged)
        {
            var sourceMap = _entityMaps.TryGetValue(entity.Owner, out var previousMap)
                ? previousMap
                : _xformQuery.TryComp(args.OldPosition.EntityId, out var oldParent)
                    ? oldParent.MapUid ?? EntityUid.Invalid
                    : EntityUid.Invalid;
            if (sourceMap.IsValid() && sourceMap != targetMap)
                _continuousSourceMaps[entity.Owner] = sourceMap;
        }
        _entityMaps[entity.Owner] = targetMap;
    }

    private void ClearHeightInterpolation(EntityUid uid)
    {
        _heights.Remove(uid);
        _continuousSourceMaps.Remove(uid);
        if (_xformQuery.TryComp(uid, out var xform))
            _entityMaps[uid] = xform.MapUid ?? EntityUid.Invalid;
    }

    internal void StartNetworkInterpolationLookahead(EntityUid uid, EntityCoordinates targetCoordinates, float targetLocalHeight, GameTick sourceTick, GameTick targetTick)
    {
        if (!_positionQuery.TryComp(uid, out var zPosition) ||
            !_xformQuery.TryComp(uid, out var xform) ||
            !_xformQuery.TryComp(targetCoordinates.EntityId, out var parent) ||
            parent.MapUid is not { } targetMap)
            return;

        var targetDepth = _zLevels.TryGetMapDepth(targetMap, out var depth) ? depth.Value : 0;
        _heights[uid] = new HeightInterpolation
        {
            Source = GetRenderAbsoluteZ(uid, xform),
            Target = ZLevelProjection.GetAbsoluteZ(targetDepth, targetLocalHeight),
            LastPhase = -1f,
            SourceTick = sourceTick,
            TargetTick = targetTick,
            CreatedTick = _timing.CurTick,
            Type = RenderInterpolationType.NetworkInterpolation,
            SourceMap = xform.MapUid ?? EntityUid.Invalid,
            TargetMap = targetMap,
            ContinuousMapMove = true,
            Initialized = true,
            SourceLocalHeight = zPosition.LocalHeight,
            TargetLocalHeight = targetLocalHeight,
        };
    }

    [SubscribeLocalEvent]
    private void OnPositionShutdown(Entity<ZLevelPositionComponent> entity, ref ComponentShutdown args)
    {
        _heights.Remove(entity.Owner);
        _entityMaps.Remove(entity.Owner);
        _continuousSourceMaps.Remove(entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<ZLevelMapNetworkComponent> entity, ref ZLevelNetworkShutdownEvent args)
    {
        _visuals.Remove(entity.Owner);
    }

    public ZLevelVisuals GetVisuals(EntityUid network)
    {
        if (_visuals.TryGetValue(network, out var visuals))
            return visuals;
        var args = new ZLevelVisualsEvent();
        RaiseLocalEvent(network, ref args);
        visuals = new ZLevelVisuals(args.ProjectionOffset, args.StopAtOpaque);
        _visuals[network] = visuals;
        return visuals;
    }

    public float GetRenderAbsoluteZ(EntityUid uid, TransformComponent? xform = null)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrNullRef(_heights, uid);
        if (!Unsafe.IsNullRef(ref state))
        {
            if (!EnsureInitialized(uid, ref state))
                return 0f;
            if (!_timing.InSimulation)
                UpdateAlpha(ref state);
            return MathHelper.Lerp(state.Source, state.Target, state.Alpha);
        }
        if (!_xformQuery.Resolve(uid, ref xform, false))
            return 0f;
        var map = xform.MapUid ?? EntityUid.Invalid;
        var depth = _zLevels.TryGetMapDepth(map, out var value) ? value.Value : 0;
        return _positionQuery.TryComp(uid, out var position)
            ? ZLevelProjection.GetAbsoluteZ(depth, position.LocalHeight)
            : depth;
    }

    private bool EnsureInitialized(EntityUid uid, ref HeightInterpolation state)
    {
        if (state.Initialized)
            return true;
        if (!_xformQuery.HasComp(uid))
            return false;

        var sourceDepth = _zLevels.TryGetMapDepth(state.SourceMap, out var source) ? source.Value : 0;
        var targetDepth = _zLevels.TryGetMapDepth(state.TargetMap, out var target) ? target.Value : 0;
        if (!state.SourceExplicit)
            state.Source = ZLevelProjection.GetAbsoluteZ(sourceDepth, state.SourceLocalHeight);
        state.Target = ZLevelProjection.GetAbsoluteZ(targetDepth, state.TargetLocalHeight);
        state.PendingMapValidation = _timing.ApplyingState && state.SourceMap != state.TargetMap;
        state.Initialized = true;
        return true;
    }

    private void UpdateAlpha(ref HeightInterpolation state)
    {
        var phase = _timing.TickPhase;
        if (state.Type == RenderInterpolationType.PredictionInterpolation)
        {
            // CurTick has advanced past the simulated tick by the time its frames are drawn.
            // A segment gets AT LEAST one tick of interpolation, even while waiting for the server.
            var wholeTicks = _timing.CurTick > state.CreatedTick
                ? _timing.CurTick.Value - state.CreatedTick.Value - 1
                : 0;
            state.Alpha = Math.Clamp(wholeTicks + phase, 0f, 1f);
        }
        else if (state.TargetTick > state.SourceTick)
        {
            var span = state.TargetTick.Value - state.SourceTick.Value;
            var wholeTicks = _timing.LastProcessedTick > state.SourceTick
                ? _timing.LastProcessedTick.Value - state.SourceTick.Value - 1
                : 0;
            state.Alpha = Math.Max(state.Alpha, Math.Clamp((wholeTicks + phase) / span, 0f, 1f));
        }
        else if (state.LastPhase >= 0f && phase < state.LastPhase)
            state.Alpha = _timing.CurTick == state.CreatedTick ? phase : 1f;
        else
            state.Alpha = Math.Max(state.Alpha, phase);
        state.LastPhase = phase;
    }

    private ZLevelRenderTransform GetRenderData(Entity<TransformComponent?> ent)
    {
        var pose = _transforms.GetCanonicalRenderWorldTransform(ent);
        var projected = ProjectRenderTransform(ent, pose);
        if (_heights.TryGetValue(ent.Owner, out var state))
        {
            projected = projected with
            {
                RenderSpaceAlpha = state.Alpha,
                SourceRenderSpace = state.SourceMap,
                TargetRenderSpace = state.TargetMap,
            };
        }
        return projected;
    }

    private ZLevelRenderTransform ProjectRenderTransform(Entity<TransformComponent?> ent, RenderTransform pose)
    {
        var absoluteZ = GetRenderAbsoluteZ(ent.Owner, ent.Comp);
        var coordinateSpace = GetRenderCoordinateSpace(pose.CoordinateSpace, absoluteZ);
        var depth = _zLevels.TryGetMapDepth(coordinateSpace, out var value) ? value.Value : 0;
        var offset = Vector2.Zero;
        if (_zLevels.TryGetMapData(coordinateSpace, out var map, out _))
            offset = GetVisuals(map.Network).ProjectionOffset;
        var projected = new ZLevelRenderTransform(
            ZLevelProjection.Project(pose.Position, absoluteZ, depth, offset),
            pose.Position,
            pose.Rotation,
            coordinateSpace,
            pose.SourceRenderSpace,
            pose.TargetRenderSpace,
            pose.RenderSpaceAlpha,
            absoluteZ,
            depth,
            offset);
        return projected;
    }

    private RenderTransform GetProjectedRenderTransform(EntityUid uid, TransformComponent? xform, RenderTransform pose)
    {
        var projected = ProjectRenderTransform((uid, xform), pose);

        return pose with
        {
            Position = projected.Position,
            CanonicalPosition = projected.CanonicalPosition,
            CoordinateSpace = projected.CoordinateSpace,
            AbsoluteZ = projected.AbsoluteZ,
            ReferenceDepth = projected.ReferenceDepth,
        };
    }

    private EntityUid GetRenderCoordinateSpace(EntityUid simulationSpace, float absoluteZ)
    {
        if (!_zLevels.TryGetMapData(simulationSpace, out _, out var network) ||
            network.SortedZLevels.Count == 0)
        {
            return simulationSpace;
        }

        var depth = Math.Clamp(
            (int) MathF.Floor(absoluteZ + ZLevelProjection.BoundaryEpsilon),
            0,
            network.SortedZLevels.Count - 1);
        return network.SortedZLevels[depth];
    }

    internal int GetRenderLayerSamples(Entity<TransformComponent?> ent, Span<RenderLayerSample> samples, IReadOnlySet<EntityUid>? visibleMaps = null)
    {
        if (samples.Length == 0)
            return 0;
        var pose = GetRenderData(ent);
        if (!_zLevels.TryGetMapData(pose.CoordinateSpace, out var coordinateMap, out var network))
        {
            if (!pose.CoordinateSpace.IsValid() ||
                visibleMaps != null && !visibleMaps.Contains(pose.CoordinateSpace))
                return 0;

            samples[0] = new RenderLayerSample(pose.CoordinateSpace, pose.Position, pose.Rotation, 1f, 0);
            return 1;
        }
        var weights = ZLevelProjection.GetLayerWeights(pose.AbsoluteZ);
        var offset = GetVisuals(coordinateMap.Network).ProjectionOffset;
        var count = 0;
        if (weights.LowerWeight > 0f && weights.LowerDepth >= 0 && weights.LowerDepth < network.SortedZLevels.Count)
        {
            var map = network.SortedZLevels[weights.LowerDepth];
            if (visibleMaps == null || visibleMaps.Contains(map))
                samples[count++] = new RenderLayerSample(map, ZLevelProjection.Reproject(pose.Position, coordinateMap.Depth, weights.LowerDepth, offset), pose.Rotation, weights.LowerWeight, weights.LowerDepth);
        }
        if (weights.UpperWeight > 0f && samples.Length > count && weights.UpperDepth >= 0 && weights.UpperDepth < network.SortedZLevels.Count)
        {
            var map = network.SortedZLevels[weights.UpperDepth];
            if (visibleMaps == null || visibleMaps.Contains(map))
                samples[count++] = new RenderLayerSample(map, ZLevelProjection.Reproject(pose.Position, coordinateMap.Depth, weights.UpperDepth, offset), pose.Rotation, weights.UpperWeight, weights.UpperDepth);
        }
        if (count == 1 && samples[0].Opacity < 1f)
            samples[0] = samples[0] with { Opacity = 1f };
        else if (visibleMaps != null && count == 2)
        {
            var total = samples[0].Opacity + samples[1].Opacity;
            if (total > ZLevelProjection.BoundaryEpsilon)
            {
                samples[0] = samples[0] with { Opacity = samples[0].Opacity / total };
                samples[1] = samples[1] with { Opacity = samples[1].Opacity / total };
            }
        }
        return count;
    }

    internal int GetRenderLayerSamples(EntityUid uid, Span<RenderLayerSample> samples, TransformComponent? xform = null, IReadOnlySet<EntityUid>? visibleMaps = null)
        => GetRenderLayerSamples((uid, xform), samples, visibleMaps);

    internal bool TryGetRenderLayerSample(Entity<TransformComponent?> ent, EntityUid layerMap, out RenderLayerSample sample, IReadOnlySet<EntityUid>? visibleMaps = null)
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

    internal bool TryGetRenderLayerSample(EntityUid uid, EntityUid layerMap, out RenderLayerSample sample, TransformComponent? xform = null, IReadOnlySet<EntityUid>? visibleMaps = null)
        => TryGetRenderLayerSample((uid, xform), layerMap, out sample, visibleMaps);

    public bool TryProjectMapCoordinatesForLayer(MapCoordinates coordinates, EntityUid layerMap, out Vector2 position)
    {
        position = coordinates.Position;
        var sourceMap = _maps.GetMapOrInvalid(coordinates.MapId);
        if (sourceMap == EntityUid.Invalid || layerMap == EntityUid.Invalid)
            return false;
        if (sourceMap == layerMap)
            return true;
        if (!_zLevels.TryGetMapData(sourceMap, out var source, out _) || !_zLevels.TryGetMapData(layerMap, out var target, out _) || source.Network != target.Network)
            return false;
        position = ZLevelProjection.Reproject(position, source.Depth, target.Depth, GetVisuals(source.Network).ProjectionOffset);
        return true;
    }

    public bool TryGetMapRenderBoundsForLayer(EntityUid sourceMap, EntityUid layerMap, in Box2Rotated layerBounds, out MapId sourceMapId, out Box2Rotated sourceBounds)
    {
        sourceMapId = MapId.Nullspace;
        sourceBounds = layerBounds;
        if (!TryComp(sourceMap, out MapComponent? map) || !TryProjectMapCoordinatesForLayer(new MapCoordinates(Vector2.Zero, map.MapId), layerMap, out var origin))
            return false;
        sourceMapId = map.MapId;
        sourceBounds.Box = sourceBounds.Box.Translated(-origin);
        sourceBounds.Origin -= origin;
        return true;
    }

    public bool TryGetZLevelRenderDebugData(EntityUid uid, out ZLevelRenderDebugData data)
    {
        if (!_xformQuery.TryComp(uid, out var xform))
        {
            data = default;
            return false;
        }

        var pose = GetRenderData((uid, xform));
        var mapDepth = _zLevels.TryGetMapDepth(xform.MapUid ?? EntityUid.Invalid, out var depth) ? depth.Value : 0;
        Span<RenderLayerSample> samples = stackalloc RenderLayerSample[2];
        var count = GetRenderLayerSamples((uid, xform), samples);
        var transformDebug = _transforms.TryGetRenderTransformDebugData(uid, out var debug);
        var state = _heights.TryGetValue(uid, out var height) ? height : new HeightInterpolation
        {
            Source = pose.AbsoluteZ,
            Target = pose.AbsoluteZ,
            Alpha = 1f,
            Type = transformDebug ? debug.Type : default,
        };
        var type = transformDebug ? debug.Type : state.Type;
        data = new ZLevelRenderDebugData(
            type,
            state.Alpha,
            CreateDebugTransform(_transforms.GetCanonicalRenderWorldTransform((uid, xform)),
                ZLevelProjection.GetAbsoluteZ(mapDepth, _positionQuery.TryComp(uid, out var simulationPosition) ? simulationPosition.LocalHeight : 0f)),
            CreateDebugTransform(transformDebug ? debug.Source : _transforms.GetCanonicalRenderWorldTransform((uid, xform)), state.Source),
            CreateDebugTransform(transformDebug ? debug.Target : _transforms.GetCanonicalRenderWorldTransform((uid, xform)), state.Target),
            pose,
            transformDebug ? debug.CorrectionTranslation : Vector2.Zero,
            transformDebug ? debug.CorrectionRotation : Angle.Zero,
            0f,
            mapDepth,
            _positionQuery.TryComp(uid, out var position) ? position.LocalHeight : pose.AbsoluteZ - mapDepth,
            count,
            count > 0 ? samples[0] : default,
            count > 1 ? samples[1] : default);
        return true;
    }

    private ZLevelRenderTransform CreateDebugTransform(in RenderTransform pose, float absoluteZ)
    {
        var depth = _zLevels.TryGetMapDepth(pose.CoordinateSpace, out var value) ? value.Value : 0;
        var offset = Vector2.Zero;
        if (_zLevels.TryGetMapData(pose.CoordinateSpace, out var map, out _))
            offset = GetVisuals(map.Network).ProjectionOffset;
        return new ZLevelRenderTransform(ZLevelProjection.Project(pose.Position, absoluteZ, depth, offset), pose.Position, pose.Rotation, pose.CoordinateSpace, pose.SourceRenderSpace, pose.TargetRenderSpace, pose.RenderSpaceAlpha, absoluteZ, depth, offset);
    }

    private struct HeightInterpolation
    {
        public float Source;
        public float Target;
        public float Alpha;
        public float LastPhase;
        public GameTick SourceTick;
        public GameTick TargetTick;
        public GameTick CreatedTick;
        public RenderInterpolationType Type;
        public EntityUid SourceMap;
        public EntityUid TargetMap;
        public bool PendingMapValidation;
        public bool ContinuousMapMove;
        public bool Initialized;
        public bool SourceExplicit;
        public float SourceLocalHeight;
        public float TargetLocalHeight;
    }
}

/// <summary>
/// Raised on a z-level network when the client needs its rendering settings.
/// </summary>
[ByRefEvent]
public struct ZLevelVisualsEvent
{
    public Vector2 ProjectionOffset;
    public bool StopAtOpaque;
}

public readonly record struct ZLevelVisuals(Vector2 ProjectionOffset, bool StopAtOpaque);

public readonly record struct ZLevelRenderTransform(Vector2 Position, Vector2 CanonicalPosition, Angle Rotation, EntityUid CoordinateSpace, EntityUid SourceRenderSpace, EntityUid TargetRenderSpace, float RenderSpaceAlpha, float AbsoluteZ, int ReferenceDepth, Vector2 ProjectionOffset);

public readonly record struct ZLevelRenderDebugData(RenderInterpolationType Type, float Alpha, ZLevelRenderTransform Simulation, ZLevelRenderTransform Source, ZLevelRenderTransform Target, ZLevelRenderTransform Rendered, Vector2 CorrectionTranslation, Angle CorrectionRotation, float CorrectionZ, int MapDepth, float SourceLocalHeight, int LayerCount, RenderLayerSample FirstLayer, RenderLayerSample SecondLayer)
{
    public EntityUid SourceRenderSpace => Source.SourceRenderSpace;
    public EntityUid TargetRenderSpace => Target.TargetRenderSpace;
}
