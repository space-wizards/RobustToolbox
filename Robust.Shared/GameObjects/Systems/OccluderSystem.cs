using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects.Systems
{
    public abstract class OccluderSystem : EntitySystem
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        private readonly Dictionary<MapId, DynamicTree<OccluderComponent>> _mapTrees =
            new Dictionary<MapId, DynamicTree<OccluderComponent>>();

        private readonly List<OccluderComponent> _occluderQueue = new List<OccluderComponent>();

        internal DynamicTree<OccluderComponent> GetOccluderTreeForMap(MapId map)
        {
            return _mapTrees[map];
        }

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.MapCreated += MapManagerOnMapCreated;
            _mapManager.MapDestroyed += MapManagerOnMapDestroyed;

            SubscribeLocalEvent<EntityInitializedMessage>(EntInitialized);
            SubscribeLocalEvent<EntMapIdChangedMessage>(EntMapIdChanged);
            SubscribeLocalEvent<MoveEvent>(EntMoved);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<OccluderBoundingBoxChangedMessage>(OccluderBoundingBoxChanged);
            SubscribeLocalEvent<OccluderTreeRemoveOccluderMessage>(RemoveOccluder);
        }

        public override void FrameUpdate(float frameTime)
        {
            UpdateTrees();
        }

        public override void Update(float frameTime)
        {
            UpdateTrees();
        }

        public void UpdateTrees()
        {
            foreach (var queuedUpdateOccluder in _occluderQueue)
            {
                var transform = queuedUpdateOccluder.Owner.Transform;
                var map = transform.MapID;
                if (map == MapId.Nullspace)
                {
                    continue;
                }

                var updateMapTree = _mapTrees[map];

                updateMapTree.AddOrUpdate(queuedUpdateOccluder);
                queuedUpdateOccluder.TreeUpdateQueued = false;
            }


            _occluderQueue.Clear();
        }

        // If the Transform is removed BEFORE the Occluder,
        // then the MapIdChanged code will handle and remove it (because MapId gets set to nullspace).
        // Otherwise these will still have their past MapId and that's all we need..
        private void RemoveOccluder(OccluderTreeRemoveOccluderMessage ev)
        {
            _mapTrees[ev.Map].Remove(ev.Occluder);
        }

        private void OccluderBoundingBoxChanged(OccluderBoundingBoxChangedMessage ev)
        {
            QueueUpdateOccluder(ev.Occluder);
        }

        private void EntParentChanged(EntParentChangedMessage ev)
        {
            UpdateEntity(ev.Entity);
        }

        private void EntMoved(MoveEvent ev)
        {
            UpdateEntity(ev.Sender);
        }

        private void EntMapIdChanged(EntMapIdChangedMessage ev)
        {
            if (ev.Entity.TryGetComponent(out OccluderComponent? occluder))
            {
                // Nullspace is a valid map ID for stuff to have but we also aren't gonna bother indexing it.
                // So that's why there's a GetValueOrDefault.
                var oldTree = _mapTrees.GetValueOrDefault(ev.OldMapId);
                var newTree = _mapTrees.GetValueOrDefault(ev.Entity.Transform.MapID);

                oldTree?.Remove(occluder);
                newTree?.AddOrUpdate(occluder);
            }
        }

        private void EntInitialized(EntityInitializedMessage ev)
        {
            UpdateEntity(ev.Entity);
        }

        private void UpdateEntity(IEntity entity)
        {
            if (entity.TryGetComponent(out OccluderComponent? occluder))
            {
                QueueUpdateOccluder(occluder);
            }

            foreach (var child in entity.Transform.Children)
            {
                UpdateEntity(child.Owner);
            }
        }

        private void MapManagerOnMapDestroyed(object? sender, MapEventArgs e)
        {
            _mapTrees.Remove(e.Map);
        }

        private void MapManagerOnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace)
            {
                return;
            }

            _mapTrees.Add(e.Map, new DynamicTree<OccluderComponent>(ExtractAabbFunc));
        }

        private static Box2 ExtractAabbFunc(in OccluderComponent o)
        {
            var worldPos = o.Owner.Transform.WorldPosition;
            return o.BoundingBox.Translated(worldPos);
        }

        private void QueueUpdateOccluder(OccluderComponent occluder)
        {
            if (!occluder.TreeUpdateQueued)
            {
                occluder.TreeUpdateQueued = true;

                _occluderQueue.Add(occluder);
            }
        }

        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId originMapId, in Ray ray, float maxLength,
            Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            var mapTree = _mapTrees[originMapId];
            var list = new List<RayCastResults>();

            mapTree.QueryRay(ref list,
                (ref List<RayCastResults> state, in OccluderComponent value, in Vector2 point, float distFromOrigin) =>
                {
                    if (distFromOrigin > maxLength)
                    {
                        return true;
                    }

                    if (!value.Enabled)
                    {
                        return true;
                    }

                    if (predicate != null && predicate.Invoke(value.Owner))
                    {
                        return true;
                    }

                    var result = new RayCastResults(distFromOrigin, point, value.Owner);
                    state.Add(result);
                    return !returnOnFirstHit;
                }, ray);

            return list;
        }
    }

    internal readonly struct OccluderTreeRemoveOccluderMessage
    {
        public readonly OccluderComponent Occluder;
        public readonly MapId Map;

        public OccluderTreeRemoveOccluderMessage(OccluderComponent occluder, MapId map)
        {
            Occluder = occluder;
            Map = map;
        }
    }
}
