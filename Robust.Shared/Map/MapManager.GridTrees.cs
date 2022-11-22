using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.Map;

internal partial class MapManager
{
    // TODO: Move IMapManager stuff to the system
    private Dictionary<MapId, B2DynamicTree<MapGridComponent>> _gridTrees = new();

    private Dictionary<MapId, HashSet<MapGridComponent>> _movedGrids = new();

    /// <summary>
    /// Gets the grids that have moved this tick until broadphase has run.
    /// </summary>
    /// <param name="mapId"></param>
    /// <returns></returns>
    public HashSet<MapGridComponent> GetMovedGrids(MapId mapId)
    {
        // Temporary until this is moved to SharedPhysicsMapComponent
        if (!_movedGrids.TryGetValue(mapId, out var moved))
        {
            Logger.ErrorS("map", $"Unable to get moved grids for {mapId}");
            moved = new HashSet<MapGridComponent>();
        }

        return moved;
    }

    public void ClearMovedGrids(MapId mapId)
    {
        if (!_movedGrids.TryGetValue(mapId, out var moved))
        {
            Logger.ErrorS("map", $"Unable to clear moved grids for {mapId}");
            return;
        }

        moved.Clear();
    }

    private void StartupGridTrees()
    {
        // Needs to be done on mapmanager startup because the eventbus will clear on shutdown
        // (and mapmanager initialize doesn't run upon connecting to a server every time).
        EntityManager.EventBus.SubscribeEvent<GridInitializeEvent>(EventSource.Local, this, OnGridInit);
        EntityManager.EventBus.SubscribeEvent<GridRemovalEvent>(EventSource.Local, this, OnGridRemove);
        EntityManager.EventBus.SubscribeLocalEvent<MapGridComponent, MoveEvent>(OnGridMove);
        EntityManager.EventBus.SubscribeLocalEvent<MapGridComponent, EntParentChangedMessage>(OnGridParentChange);
    }

    private void ShutdownGridTrees()
    {
        EntityManager.EventBus.UnsubscribeEvent<GridInitializeEvent>(EventSource.Local, this);
        EntityManager.EventBus.UnsubscribeEvent<GridRemovalEvent>(EventSource.Local, this);
        EntityManager.EventBus.UnsubscribeLocalEvent<MapGridComponent, MoveEvent>();
        EntityManager.EventBus.UnsubscribeLocalEvent<MapGridComponent, EntParentChangedMessage>();

        DebugTools.Assert(_gridTrees.Count == 0);
        DebugTools.Assert(_movedGrids.Count == 0);
    }

    private void OnMapCreatedGridTree(MapEventArgs e)
    {
        if (e.Map == MapId.Nullspace) return;

        _gridTrees.Add(e.Map, new B2DynamicTree<MapGridComponent>());
        _movedGrids.Add(e.Map, new HashSet<MapGridComponent>());
    }

    private void OnMapDestroyedGridTree(MapEventArgs e)
    {
        if (e.Map == MapId.Nullspace) return;

        _gridTrees.Remove(e.Map);
        _movedGrids.Remove(e.Map);
    }

    private Box2 GetWorldAABB(MapGridComponent grid, TransformComponent? xform = null)
    {
        xform ??= EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);

        var (worldPos, worldRot) = xform.GetWorldPositionRotation();

        return new Box2Rotated(grid.LocalAABB, worldRot).CalcBoundingBox().Translated(worldPos);
    }

    private void OnGridInit(GridInitializeEvent args)
    {
        if (EntityManager.HasComponent<MapComponent>(args.EntityUid))
            return;

        var xform = EntityManager.GetComponent<TransformComponent>(args.EntityUid);
        var mapId = xform.MapID;

        if (mapId == MapId.Nullspace) return;

        var grid = EntityManager.GetComponent<MapGridComponent>(args.EntityUid);
        AddGrid(grid, mapId);
    }

    private void AddGrid(MapGridComponent grid, MapId mapId)
    {
        DebugTools.Assert(!EntityManager.HasComponent<MapComponent>(grid.Owner));
        var aabb = GetWorldAABB(grid);
        var proxy = _gridTrees[mapId].CreateProxy(in aabb, grid);
        DebugTools.Assert(grid.MapProxy == DynamicTree.Proxy.Free);
        grid.MapProxy = proxy;

        _movedGrids[mapId].Add(grid);
    }

    private void OnGridRemove(GridRemovalEvent args)
    {
        if (EntityManager.HasComponent<MapComponent>(args.EntityUid))
            return;

        var xform = EntityManager.GetComponent<TransformComponent>(args.EntityUid);

        // Can't check for free proxy because DetachParentToNull gets called first woo!
        if (xform.MapID == MapId.Nullspace) return;

        var grid = EntityManager.GetComponent<MapGridComponent>(args.EntityUid);
        RemoveGrid(grid, xform.MapID);
    }

    private void RemoveGrid(MapGridComponent grid, MapId mapId)
    {
        _gridTrees[mapId].DestroyProxy(grid.MapProxy);
        _movedGrids[mapId].Remove(grid);
        grid.MapProxy = DynamicTree.Proxy.Free;
    }

    private void OnGridMove(EntityUid uid, MapGridComponent component, ref MoveEvent args)
    {
        // Just maploader / test things
        if (component.MapProxy == DynamicTree.Proxy.Free) return;

        var xform = args.Component;
        var aabb = GetWorldAABB(component, xform);
        _gridTrees[xform.MapID].MoveProxy(component.MapProxy, in aabb, Vector2.Zero);
        _movedGrids[xform.MapID].Add(component);
    }

    private void OnGridParentChange(EntityUid uid, MapGridComponent component, ref EntParentChangedMessage args)
    {
        if (EntityManager.HasComponent<MapComponent>(uid))
            return;

        var lifestage = EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage;

        // oh boy
        // Want gridinit to handle this hence specialcase those situations.
        // oh boy oh boy, its even worse now.
        // transform now raises parent change events on startup, because container code is a POS.
        if (lifestage < EntityLifeStage.Initialized || args.Transform.LifeStage == ComponentLifeStage.Starting)
            return;

        // Make sure we cleanup old map for moved grid stuff.
        var mapId = args.Transform.MapID;

        // y'all need jesus
        if (args.OldMapId == mapId) return;

        if (component.MapProxy != DynamicTree.Proxy.Free && _movedGrids.TryGetValue(args.OldMapId, out var oldMovedGrids))
        {
            oldMovedGrids.Remove(component);
            RemoveGrid(component, args.OldMapId);
        }

        DebugTools.Assert(component.MapProxy == DynamicTree.Proxy.Free);
        if (_movedGrids.TryGetValue(mapId, out var newMovedGrids))
        {
            newMovedGrids.Add(component);
            AddGrid(component, mapId);
        }
    }

    public void OnGridBoundsChange(EntityUid uid, MapGridComponent grid)
    {
        // Just MapLoader things.
        if (grid.MapProxy == DynamicTree.Proxy.Free) return;

        var xform = EntityManager.GetComponent<TransformComponent>(uid);
        var aabb = GetWorldAABB(grid, xform);
        _gridTrees[xform.MapID].MoveProxy(grid.MapProxy, in aabb, Vector2.Zero);
        _movedGrids[xform.MapID].Add(grid);
    }
}
