using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Robust.Client.GameObjects;

[UsedImplicitly]
public sealed partial class ClientOccluderSystem : OccluderSystem
{
    private const float SharedOccluderEdgeTolerance = 0.001f;
    private const float SharedOccluderNeighbourQueryPadding = SharedOccluderEdgeTolerance * 4f;

    private readonly HashSet<EntityUid> _dirtyEntities = new();
    private readonly HashSet<(EntityUid TreeUid, Box2 Bounds)> _dirtyBounds = new();
    private readonly Vector4[] _edgeBuffer = new Vector4[PhysicsConstants.MaxPolygonVertices];
    private readonly Vector4[] _otherEdgeBuffer = new Vector4[PhysicsConstants.MaxPolygonVertices];

    [Dependency] private EntityQuery<OccluderComponent> _occluderQuery;
    [Dependency] private EntityQuery<OccluderTreeComponent> _treeQuery;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OccluderComponent, ComponentShutdown>(OnShutdown);
    }

    public override void SetPolygon(EntityUid uid, Vector2[]? polygon, OccluderComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        base.SetPolygon(uid, polygon, comp);
        QueueSharedEdgeUpdate(uid, comp);
    }

    public override void SetEnabled(EntityUid uid, bool enabled, OccluderComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!Resolve(uid, ref comp, false) || enabled == comp.Enabled)
            return;

        base.SetEnabled(uid, enabled, comp, meta);
        QueueSharedEdgeUpdate(uid, comp);
    }

    protected override void OnCompStartup(EntityUid uid, OccluderComponent comp, ComponentStartup args)
    {
        base.OnCompStartup(uid, comp, args);
        QueueSharedEdgeUpdate(uid, comp);
    }

    protected override void OnCompRemoved(EntityUid uid, OccluderComponent comp, ComponentRemove args)
    {
        if (!Terminating(uid))
            QueueSharedEdgeUpdate(uid, comp);

        base.OnCompRemoved(uid, comp, args);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var (treeUid, bounds) in _dirtyBounds)
        {
            DirtyOccludersInTree(treeUid, bounds);
        }

        _dirtyBounds.Clear();

        if (_dirtyEntities.Count == 0)
            return;

        try
        {
            foreach (var uid in _dirtyEntities)
            {
                if (_occluderQuery.TryGetComponent(uid, out var occluder)
                    && _xformQuery.TryGetComponent(uid, out var xform))
                {
                    UpdateCachedSharedEdges(uid, occluder, xform);
                }
            }
        }
        finally
        {
            _dirtyEntities.Clear();
        }
    }

    protected override void OnComponentMove(EntityUid uid, OccluderComponent comp, ref MoveEvent args)
    {
        QueueSharedEdgeUpdate(uid, comp, args.Component);
    }

    private void OnShutdown(EntityUid uid, OccluderComponent comp, ComponentShutdown args)
    {
        if (!Terminating(uid))
            QueueSharedEdgeUpdate(uid, comp);
    }

    protected override void OnOccluderAfterAutoHandleState(EntityUid uid, OccluderComponent comp, ref AfterAutoHandleStateEvent args)
    {
        QueueSharedEdgeUpdate(uid, comp);
    }

    private void QueueSharedEdgeUpdate(EntityUid uid, OccluderComponent occluder, TransformComponent? xform = null)
    {
        occluder.OccludingEdges = 0;
        _dirtyEntities.Add(uid);

        if (occluder.LastTreeBounds is { } lastBounds)
            _dirtyBounds.Add((lastBounds.TreeUid, lastBounds.Bounds.Enlarged(SharedOccluderNeighbourQueryPadding)));

        if (!Resolve(uid, ref xform, false)
            || !TryGetTreeTransform(occluder, xform, out var treeUid, out _, out var treeBounds))
            return;

        _dirtyBounds.Add((treeUid, treeBounds.Enlarged(SharedOccluderNeighbourQueryPadding)));
    }

    private void DirtyOccludersInTree(
        EntityUid treeUid,
        Box2 treeBounds)
    {
        // We need to handle shared edges as there's some cases where we don't want them to show, e.g. between walls.
        if (!_treeQuery.TryGetComponent(treeUid, out var treeComp))
            return;

        treeComp.Tree.QueryAabb((in ComponentTreeEntry<OccluderComponent> entry) =>
        {
            var occluder = entry.Component;
            if (!occluder.Enabled)
                return true;

            occluder.OccludingEdges = 0;
            _dirtyEntities.Add(entry.Uid);
            return true;
        }, treeBounds);
    }

    private void UpdateCachedSharedEdges(
        EntityUid uid,
        OccluderComponent occluder,
        TransformComponent xform)
    {
        occluder.OccludingEdges = 0;
        occluder.LastTreeBounds = null;

        if (!TryGetTreeTransform(occluder, xform, out var treeUid, out var treeTransform, out var treeBounds))
            return;

        occluder.LastTreeBounds = (treeUid, treeBounds);

        var polygon = occluder.Polygon;
        var edgeCount = BuildOccluderEdges(polygon, treeTransform, _edgeBuffer);
        if (edgeCount == 0)
            return;

        if (!_treeQuery.TryGetComponent(treeUid, out var treeComp))
            return;

        var queryBounds = treeBounds.Enlarged(SharedOccluderNeighbourQueryPadding);
        var state = (Uid: uid, TreeUid: treeUid, Edges: _edgeBuffer, EdgeCount: edgeCount, Occluder: occluder, System: this);
        treeComp.Tree.QueryAabb(
            ref state,
            static (ref (
                    EntityUid Uid,
                    EntityUid TreeUid,
                    Vector4[] Edges,
                    int EdgeCount,
                    OccluderComponent Occluder,
                    ClientOccluderSystem System) state,
                in ComponentTreeEntry<OccluderComponent> entry) =>
            {
                if (entry.Uid == state.Uid)
                    return true;

                var other = entry.Component;
                if (!other.Enabled || other.Polygon.Length < 3)
                    return true;

                var otherTransform = state.System.GetTreeTransform(entry.Transform, state.TreeUid);
                var otherEdges = state.System._otherEdgeBuffer;
                var otherEdgeCount = BuildOccluderEdges(other.Polygon, otherTransform, otherEdges);
                if (otherEdgeCount == 0)
                    return true;

                state.Occluder.OccludingEdges |= CalculateSharedEdgeMask(
                    state.Edges.AsSpan(0, state.EdgeCount),
                    otherEdges.AsSpan(0, otherEdgeCount));
                return true;
            },
            queryBounds);
    }

    private bool TryGetTreeTransform(
        OccluderComponent occluder,
        TransformComponent xform,
        out EntityUid treeUid,
        out Matrix3x2 treeTransform,
        out Box2 treeBounds)
    {
        treeUid = default;
        treeTransform = default;
        treeBounds = default;

        var polygon = occluder.Polygon;
        if (!occluder.Enabled || polygon.Length < 3 || xform.MapUid == null)
            return false;

        treeUid = xform.GridUid ?? xform.MapUid.Value;
        treeTransform = GetTreeTransform(xform, treeUid);
        treeBounds = treeTransform.TransformBox(occluder.LocalBounds);
        return true;
    }

    private Matrix3x2 GetTreeTransform(TransformComponent xform, EntityUid treeUid)
    {
        var (position, rotation) = XformSystem.GetRelativePositionRotation(xform, treeUid);
        return Matrix3Helpers.CreateTransform(position, rotation);
    }

    private static byte CalculateSharedEdgeMask(ReadOnlySpan<Vector4> edges, ReadOnlySpan<Vector4> otherEdges)
    {
        Span<OccluderEdgeKey> edgeKeys = stackalloc OccluderEdgeKey[PhysicsConstants.MaxPolygonVertices];
        Span<OccluderEdgeKey> otherEdgeKeys = stackalloc OccluderEdgeKey[PhysicsConstants.MaxPolygonVertices];

        for (var i = 0; i < edges.Length; i++)
        {
            edgeKeys[i] = OccluderEdgeKey.From(edges[i]);
        }

        for (var i = 0; i < otherEdges.Length; i++)
        {
            otherEdgeKeys[i] = OccluderEdgeKey.From(otherEdges[i]);
        }

        byte mask = 0;
        for (var i = 0; i < edges.Length; i++)
        {
            for (var j = 0; j < otherEdges.Length; j++)
            {
                if (!EdgeKeysMatch(edgeKeys[i], otherEdgeKeys[j]))
                    continue;

                mask = (byte) (mask | 1 << i);
                break;
            }
        }

        return mask;
    }

    private static int BuildOccluderEdges(ReadOnlySpan<Vector2> polygon, Matrix3x2 worldTransform, Span<Vector4> edges)
    {
        if (polygon.Length < 3)
            return 0;

        var clockwise = SignedArea(polygon) < 0f;
        var first = default(Vector2);
        var previous = default(Vector2);

        for (var i = 0; i < polygon.Length; i++)
        {
            var sourceIndex = clockwise ? i : polygon.Length - 1 - i;
            var current = Vector2.Transform(polygon[sourceIndex], worldTransform);

            if (i == 0)
            {
                first = current;
            }
            else
            {
                edges[i - 1] = EdgeToVector4(previous, current);
            }

            previous = current;
        }

        edges[polygon.Length - 1] = EdgeToVector4(previous, first);
        return polygon.Length;
    }

    private static float SignedArea(ReadOnlySpan<Vector2> vertices)
    {
        var area = 0f;
        for (var i = 0; i < vertices.Length; i++)
        {
            var j = (i + 1) % vertices.Length;
            area += vertices[i].X * vertices[j].Y;
            area -= vertices[i].Y * vertices[j].X;
        }

        return area * 0.5f;
    }

    private static Vector4 EdgeToVector4(Vector2 a, Vector2 b)
    {
        return new Vector4(a.X, a.Y, b.X, b.Y);
    }

    private static bool EdgeKeysMatch(OccluderEdgeKey a, OccluderEdgeKey b)
    {
        return Math.Abs(a.AX - b.AX) <= 1
               && Math.Abs(a.AY - b.AY) <= 1
               && Math.Abs(a.BX - b.BX) <= 1
               && Math.Abs(a.BY - b.BY) <= 1;
    }

    private readonly record struct OccluderEdgeKey(long AX, long AY, long BX, long BY)
    {
        public static OccluderEdgeKey From(Vector4 edge)
        {
            return From(new Vector2(edge.X, edge.Y), new Vector2(edge.Z, edge.W));
        }

        private static OccluderEdgeKey From(Vector2 a, Vector2 b)
        {
            var ax = Quantize(a.X);
            var ay = Quantize(a.Y);
            var bx = Quantize(b.X);
            var by = Quantize(b.Y);

            if (ax > bx || ax == bx && ay > by)
                return new OccluderEdgeKey(bx, by, ax, ay);

            return new OccluderEdgeKey(ax, ay, bx, by);
        }

        private static long Quantize(float value)
        {
            return (long) MathF.Round(value / SharedOccluderEdgeTolerance);
        }
    }
}
