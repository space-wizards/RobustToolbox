using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using static Robust.Shared.GameObjects.OccluderComponent;

namespace Robust.Client.GameObjects;

// NOTE: this class handles both snap grid updates of occluders, as well as occluder tree updates (via its parent).
// This seems like it's doing somewhat double work because it already has an update queue for occluders but...
// See the thing is the snap grid stuff was coded earlier
// and technically it only cares about changes in the entity's SNAP GRID position.
// Whereas the tree stuff is precise.
// Also I just realized this and I cba to refactor this again.
[UsedImplicitly]
internal sealed class ClientOccluderSystem : OccluderSystem
{
    private readonly HashSet<EntityUid> _dirtyEntities = new();
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OccluderComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<OccluderComponent, ComponentShutdown>(OnShutdown);
    }

    public override void SetEnabled(EntityUid uid, bool enabled, OccluderComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!Resolve(uid, ref comp, false) || enabled == comp.Enabled)
            return;

        base.SetEnabled(uid, enabled, comp, meta);

        var xform = Transform(uid);
        QueueTreeUpdate(uid, comp, xform);
        QueueOccludedDirectionUpdate(uid, comp, xform);
    }

    private void OnShutdown(EntityUid uid, OccluderComponent comp, ComponentShutdown args)
    {
        if (!Terminating(uid))
            QueueOccludedDirectionUpdate(uid, comp);
    }

    protected override void OnCompStartup(EntityUid uid, OccluderComponent comp, ComponentStartup args)
    {
        base.OnCompStartup(uid, comp, args);
        AnchorStateChanged(uid, comp, Transform(uid));
    }

    public void AnchorStateChanged(EntityUid uid, OccluderComponent comp, TransformComponent xform)
    {
        QueueOccludedDirectionUpdate(uid, comp, xform);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_dirtyEntities.Count == 0)
            return;

        var query = GetEntityQuery<OccluderComponent>();
        var xforms = GetEntityQuery<TransformComponent>();
        var grids = GetEntityQuery<MapGridComponent>();

        try
        {
            foreach (var entity in _dirtyEntities)
            {
                if (query.TryGetComponent(entity, out var occluder))
                    UpdateOccluder(entity, occluder, query, xforms, grids);
            }
        }
        finally
        {
            _dirtyEntities.Clear();
        }
    }

    private void OnAnchorChanged(EntityUid uid, OccluderComponent comp, ref AnchorStateChangedEvent args)
    {
        AnchorStateChanged(uid, comp, args.Transform);
    }

    private void QueueOccludedDirectionUpdate(EntityUid sender, OccluderComponent occluder, TransformComponent? xform = null)
    {
        if (!Resolve(sender, ref xform))
            return;

        occluder.Occluding = OccluderDir.None;
        var query = GetEntityQuery<OccluderComponent>();
        Vector2i pos;
        EntityUid gridId;
        MapGridComponent? grid;

        if (occluder.Enabled && xform.Anchored && TryComp(xform.GridUid, out grid))
        {
            gridId = xform.GridUid.Value;
            pos = _mapSystem.TileIndicesFor(gridId, grid, xform.Coordinates);
            _dirtyEntities.Add(sender);
        }
        else if (occluder.LastPosition != null)
        {
            (gridId, pos) = occluder.LastPosition.Value;
            occluder.LastPosition = null;
            if (!TryComp(gridId, out grid))
                return;
        }
        else
        {
            return;
        }

        DirtyNeighbours(_mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos + new Vector2i(0, 1)), query);
        DirtyNeighbours(_mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos + new Vector2i(0, -1)), query);
        DirtyNeighbours(_mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos + new Vector2i(1, 0)), query);
        DirtyNeighbours(_mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, pos + new Vector2i(-1, 0)), query);
    }

    private void DirtyNeighbours(AnchoredEntitiesEnumerator enumerator, EntityQuery<OccluderComponent> occluderQuery)
    {
        while (enumerator.MoveNext(out var entity))
        {
            if (occluderQuery.TryGetComponent(entity.Value, out var occluder))
            {
                _dirtyEntities.Add(entity.Value);
                occluder.Occluding = OccluderDir.None;
            }
        }
    }

    private void UpdateOccluder(EntityUid uid,
        OccluderComponent occluder,
        EntityQuery<OccluderComponent> occluders,
        EntityQuery<TransformComponent> xforms,
        EntityQuery<MapGridComponent> grids)
    {
        // Content may want to override the default behavior for occlusion.
        // Apparently OD needs this?
        {
            var ev = new OccluderDirectionsEvent(uid, occluder);
            RaiseLocalEvent(uid, ref ev, true);

            if (ev.Handled)
                return;
        }

        if (!occluder.Enabled)
        {
            DebugTools.Assert(occluder.Occluding == OccluderDir.None);
            DebugTools.Assert(occluder.LastPosition == null);
            return;
        }

        var xform = xforms.GetComponent(uid);
        if (!xform.Anchored || !grids.TryGetComponent(xform.GridUid, out var grid))
        {
            DebugTools.Assert(occluder.Occluding == OccluderDir.None);
            DebugTools.Assert(occluder.LastPosition == null);
            return;
        }

        var tile = _mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

        // TODO: Sub to parent changes instead or something.
        // DebugTools.Assert(occluder.LastPosition == null
            // || occluder.LastPosition.Value.Grid == xform.GridUid && occluder.LastPosition.Value.Tile == tile);
        occluder.LastPosition = (xform.GridUid.Value, tile);

        // dir starts at the relative effective south direction;
        var dir = xform.LocalRotation.GetCardinalDir();
        CheckDir(dir, OccluderDir.South, tile, occluder, xform.GridUid.Value, grid, occluders, xforms);

        dir = dir.GetClockwise90Degrees();
        CheckDir(dir, OccluderDir.West, tile, occluder, xform.GridUid.Value, grid, occluders, xforms);

        dir = dir.GetClockwise90Degrees();
        CheckDir(dir, OccluderDir.North, tile, occluder, xform.GridUid.Value, grid, occluders, xforms);

        dir = dir.GetClockwise90Degrees();
        CheckDir(dir, OccluderDir.East, tile, occluder, xform.GridUid.Value, grid, occluders, xforms);
    }

    private void CheckDir(
        Direction dir,
        OccluderDir occDir,
        Vector2i tile,
        OccluderComponent occluder,
        EntityUid gridUid,
        MapGridComponent grid,
        EntityQuery<OccluderComponent> query,
        EntityQuery<TransformComponent> xforms)
    {
        if ((occluder.Occluding & occDir) != 0)
            return;

        foreach (var neighbor in _mapSystem.GetAnchoredEntities(gridUid, grid, tile.Offset(dir)))
        {
            if (!query.TryGetComponent(neighbor, out var otherOccluder) || !otherOccluder.Enabled)
                continue;

            occluder.Occluding |= occDir;

            // while we are here, also set the occluder flag for the other entity;
            var otherXform = xforms.GetComponent(neighbor);
            DebugTools.Assert(otherXform.Anchored);
            var rot = -otherXform.LocalRotation;
            var otherOcDir = FromDirection(rot.RotateDir(dir.GetOpposite()));
            otherOccluder.Occluding |= otherOcDir;
        }
    }

    public static OccluderDir FromDirection(Direction dir)
    {
        return dir switch
        {
            Direction.South => OccluderDir.South,
            Direction.North => OccluderDir.North,
            Direction.East => OccluderDir.East,
            Direction.West => OccluderDir.West,
            _ => throw new ArgumentException($"Invalid dir: {dir}.")
        };
    }

    /// <summary>
    /// Raised by occluders when trying to get occlusion directions.
    /// </summary>
    [ByRefEvent]
    public struct OccluderDirectionsEvent
    {
        public bool Handled = false;
        public readonly EntityUid Sender = default!;
        public readonly OccluderComponent Occluder = default!;

        public OccluderDirectionsEvent(EntityUid sender, OccluderComponent occluder)
        {
            Sender = sender;
            Occluder = occluder;
        }
    }
}
