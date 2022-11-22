using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    private void InitializeGrid()
    {
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
        SubscribeLocalEvent<MapGridComponent, MoveEvent>(OnGridMove);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

        // TODO: Grid bounds update.
    }

    private void OnGridInit(GridInitializeEvent ev)
    {
        var xform = Transform(ev.EntityUid);
        if (!TryGetCurrentBroadphase(xform, out var broadphase))
            return;

        DebugTools.Assert(!broadphase.GridTree.Contains(ev.EntityUid));
        DebugTools.Assert(!broadphase.GridMoveBuffer.ContainsKey(ev.EntityUid));
        var aabb = new Box2Rotated(Comp<MapGridComponent>(ev.EntityUid).LocalAABB, xform.WorldRotation)
            .CalcBoundingBox().Translated(xform.WorldPosition);

        broadphase.GridTree.Add(ev.EntityUid, aabb);
        broadphase.GridMoveBuffer.Add(ev.EntityUid, aabb);
    }

    private void OnGridMove(EntityUid uid, MapGridComponent component, ref MoveEvent args)
    {
        // TODO: Need to handle moving across maps.
        if (!TryGetCurrentBroadphase(Transform(uid), out var broadphase))
            return;

        DebugTools.Assert(broadphase.GridTree.Contains(uid));
        var oldWorldPos = args.OldPosition.ToMapPos(EntityManager);
        var worldPos = args.NewPosition.ToMapPos(EntityManager);

        var oldAABB = new Box2Rotated(component.LocalAABB, args.OldRotation).CalcBoundingBox().Translated(oldWorldPos);
        var aabb = new Box2Rotated(component.LocalAABB, args.NewRotation).CalcBoundingBox().Translated(worldPos);
        broadphase.GridMoveBuffer[uid] = aabb;
        broadphase.DynamicTree.MoveProxy(component.MapProxy, in aabb, aabb.Center - oldAABB.Center);
    }

    private void OnGridRemoval(GridRemovalEvent ev)
    {
        if (!TryGetCurrentBroadphase(Transform(ev.EntityUid), out var broadphase))
            return;

        DebugTools.Assert(broadphase.GridTree.Contains(ev.EntityUid));
        DebugTools.Assert(broadphase.GridMoveBuffer.ContainsKey(ev.EntityUid));
        broadphase.GridTree.Remove(ev.EntityUid);
        broadphase.GridMoveBuffer.Remove(ev.EntityUid);
    }
}
