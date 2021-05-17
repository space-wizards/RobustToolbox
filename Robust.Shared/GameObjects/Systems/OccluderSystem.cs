using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public abstract class OccluderSystem : EntitySystem
    {

        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        private readonly Dictionary<MapId, Dictionary<GridId, DynamicTree<OccluderComponent>>> _gridTrees =
                     new();

        private readonly List<(OccluderComponent Occluder, EntityCoordinates Coordinates)> _occluderAddQueue =
                     new();

        private readonly List<(OccluderComponent Occluder, EntityCoordinates Coordinates)> _occluderRemoveQueue =
            new();

        internal bool TryGetOccluderTreeForGrid(MapId mapId, GridId gridId, [NotNullWhen(true)] out DynamicTree<OccluderComponent>? gridTree)
        {
            gridTree = null;

            if (!_gridTrees.TryGetValue(mapId, out var grids))
                return false;

            if (!grids.TryGetValue(gridId, out gridTree))
                return false;

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.MapCreated += OnMapCreated;
            _mapManager.MapDestroyed += OnMapDestroyed;
            _mapManager.OnGridCreated += OnGridCreated;
            _mapManager.OnGridRemoved += OnGridRemoved;

            SubscribeLocalEvent<MoveEvent>(EntMoved);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<OccluderBoundingBoxChangedMessage>(OccluderBoundingBoxChanged);
            SubscribeLocalEvent<OccluderTreeRemoveOccluderMessage>(RemoveOccluder);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.MapCreated -= OnMapCreated;
            _mapManager.MapDestroyed -= OnMapDestroyed;
            _mapManager.OnGridCreated -= OnGridCreated;
            _mapManager.OnGridRemoved -= OnGridRemoved;
        }

        public override void FrameUpdate(float frameTime)
        {
            UpdateTrees();
        }

        public override void Update(float frameTime)
        {
            UpdateTrees();
        }

        private void UpdateTrees()
        {
            // Only care about stuff parented to a grid I think?
            foreach (var (occluder, coordinates) in _occluderRemoveQueue)
            {
                if (coordinates.TryGetParent(EntityManager, out var parent) &&
                    parent.HasComponent<MapGridComponent>())
                {
                    var gridTree = _gridTrees[parent.Transform.MapID][parent.Transform.GridID];

                    gridTree.Remove(occluder);
                }
            }

            _occluderRemoveQueue.Clear();

            foreach (var (occluder, coordinates) in _occluderAddQueue)
            {
                if (occluder.Deleted) continue;

                if (coordinates.TryGetParent(EntityManager, out var parent) &&
                    parent.HasComponent<MapGridComponent>() || occluder.Owner.Transform.GridID == GridId.Invalid)
                {
                    parent ??= EntityManager.GetEntity(occluder.Owner.Transform.ParentUid);
                    var gridTree = _gridTrees[parent.Transform.MapID][parent.Transform.GridID];

                    gridTree.AddOrUpdate(occluder);
                }
            }

            _occluderAddQueue.Clear();
        }

        // If the Transform is removed BEFORE the Occluder,
        // then the MapIdChanged code will handle and remove it (because MapId gets set to nullspace).
        // Otherwise these will still have their past MapId and that's all we need..
        private void RemoveOccluder(OccluderTreeRemoveOccluderMessage ev)
        {
            _gridTrees[ev.MapId][ev.GridId].Remove(ev.Occluder);
        }

        private void OccluderBoundingBoxChanged(OccluderBoundingBoxChangedMessage ev)
        {
            QueueUpdateOccluder(ev.Occluder, ev.Occluder.Owner.Transform.Coordinates);
        }

        private void EntMoved(MoveEvent ev)
        {
            ev.OldPosition.TryGetParent(EntityManager, out var oldParent);
            ev.NewPosition.TryGetParent(EntityManager, out var newParent);

            if (oldParent?.Uid != newParent?.Uid)
                RemoveEntity(ev.Sender, ev.OldPosition);

            AddOrUpdateEntity(ev.Sender, ev.NewPosition);
        }

        private void EntParentChanged(EntParentChangedMessage message)
        {
            if (!message.Entity.TryGetComponent(out OccluderComponent? occluder))
                return;

            // Really only care if it's a map or grid
            if (message.OldParent != null && message.OldParent.TryGetComponent(out MapGridComponent? oldGrid))
            {
                var map = message.OldParent.Transform.MapID;
                if (_gridTrees[map].TryGetValue(oldGrid.GridIndex, out var tree))
                {
                    tree.Remove(occluder);
                }
            }

            var newParent = EntityManager.GetEntity(message.Entity.Transform.ParentUid);

            newParent.TryGetComponent(out MapGridComponent? newGrid);
            var newGridIndex = newGrid?.GridIndex ?? GridId.Invalid;
            var newMap = newParent.Transform.MapID;

            if (!_gridTrees.TryGetValue(newMap, out var newMapGrids))
            {
                newMapGrids = new Dictionary<GridId, DynamicTree<OccluderComponent>>();
                _gridTrees[newMap] = newMapGrids;
            }

            if (!newMapGrids.TryGetValue(newGridIndex, out var newTree))
            {
                newTree = new DynamicTree<OccluderComponent>(ExtractAabbFunc);
                newMapGrids[newGridIndex] = newTree;
            }

            newTree.AddOrUpdate(occluder);
        }

        private void RemoveEntity(IEntity entity, EntityCoordinates coordinates)
        {
            if (entity.TryGetComponent(out OccluderComponent? occluder))
            {
                QueueRemoveOccluder(occluder, coordinates);
            }
        }

        internal void AddOrUpdateEntity(IEntity entity, EntityCoordinates coordinates)
        {
            if (entity.TryGetComponent(out OccluderComponent? occluder))
            {
                QueueUpdateOccluder(occluder, coordinates);
            }
            // Do we even need the children update? Coz they be slow af and allocate a lot.
            // If you do end up adding children back in then for the love of GOD check if the entity has a mapgridcomponent
        }

        private void OnMapDestroyed(object? sender, MapEventArgs e)
        {
            _gridTrees.Remove(e.Map);
        }

        private void OnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace)
                return;

            _gridTrees[e.Map] = new Dictionary<GridId, DynamicTree<OccluderComponent>>();
        }

        private void OnGridRemoved(MapId mapId, GridId gridId)
        {
            foreach (var (_, gridIds) in _gridTrees)
            {
                if (gridIds.Remove(gridId))
                    break;
            }
        }

        private void OnGridCreated(MapId mapId, GridId gridId)
        {
            if (!_gridTrees.TryGetValue(mapId, out var gridTree))
                return;

            gridTree.Add(gridId, new DynamicTree<OccluderComponent>(ExtractAabbFunc));
        }

        private static Box2 ExtractAabbFunc(in OccluderComponent o)
        {
            return o.BoundingBox.Translated(o.Owner.Transform.LocalPosition);
        }

        private void QueueUpdateOccluder(OccluderComponent occluder, EntityCoordinates coordinates)
        {
            _occluderAddQueue.Add((occluder, coordinates));
        }

        private void QueueRemoveOccluder(OccluderComponent occluder, EntityCoordinates coordinates)
        {
            _occluderRemoveQueue.Add((occluder, coordinates));
        }

        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId originMapId, in Ray ray, float maxLength,
            Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            var list = new List<RayCastResults>();
            var worldBox = new Box2();

            foreach (var gridId in _mapManager.FindGridIdsIntersecting(originMapId, worldBox, true))
            {
                var gridTree = _gridTrees[originMapId][gridId];

                gridTree.QueryRay(ref list,
                    (ref List<RayCastResults> state, in OccluderComponent value, in Vector2 point, float distFromOrigin) =>
                    {
                        if (distFromOrigin > maxLength)
                            return true;

                        if (!value.Enabled)
                            return true;

                        if (predicate != null && predicate.Invoke(value.Owner))
                            return true;

                        var result = new RayCastResults(distFromOrigin, point, value.Owner);
                        state.Add(result);
                        return !returnOnFirstHit;
                    }, ray);
            }

            return list;
        }
    }

    internal readonly struct OccluderTreeRemoveOccluderMessage
    {
        public readonly OccluderComponent Occluder;
        public readonly MapId MapId;
        public readonly GridId GridId;

        public OccluderTreeRemoveOccluderMessage(OccluderComponent occluder, MapId mapId, GridId gridId)
        {
            Occluder = occluder;
            MapId = mapId;
            GridId = gridId;
        }
    }
}
