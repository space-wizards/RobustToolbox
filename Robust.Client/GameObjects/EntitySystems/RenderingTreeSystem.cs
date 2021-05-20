using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Keeps track of <see cref="DynamicTree{T}"/>s for various rendering-related components.
    /// </summary>
    [UsedImplicitly]
    public sealed class RenderingTreeSystem : EntitySystem
    {
        // Nullspace is not indexed. Keep that in mind.

        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        private readonly Dictionary<MapId, Dictionary<GridId, MapTrees>> _gridTrees = new();

        private readonly List<SpriteComponent> _spriteQueue = new();
        private readonly List<PointLightComponent> _lightQueue = new();

        internal DynamicTree<SpriteComponent> GetSpriteTreeForMap(MapId map, GridId grid)
        {
            return _gridTrees[map][grid].SpriteTree;
        }

        internal DynamicTree<PointLightComponent> GetLightTreeForMap(MapId map, GridId grid)
        {
            return _gridTrees[map][grid].LightTree;
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SpriteSystem));
            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));

            _mapManager.MapCreated += MapManagerOnMapCreated;
            _mapManager.MapDestroyed += MapManagerOnMapDestroyed;
            _mapManager.OnGridCreated += MapManagerOnGridCreated;
            _mapManager.OnGridRemoved += MapManagerOnGridRemoved;

            SubscribeLocalEvent<SpriteComponent, EntMapIdChangedMessage>(SpriteMapChanged);
            SubscribeLocalEvent<SpriteComponent, MoveEvent>(SpriteMoved);
            SubscribeLocalEvent<SpriteComponent, EntParentChangedMessage>(SpriteParentChanged);
            SubscribeLocalEvent<SpriteComponent, ComponentRemove>(RemoveSprite);

            SubscribeLocalEvent<PointLightComponent, EntMapIdChangedMessage>(LightMapChanged);
            SubscribeLocalEvent<PointLightComponent, MoveEvent>(LightMoved);
            SubscribeLocalEvent<PointLightComponent, EntParentChangedMessage>(LightParentChanged);
            SubscribeLocalEvent<PointLightComponent, PointLightRadiusChangedEvent>(PointLightRadiusChanged);
            SubscribeLocalEvent<PointLightComponent, RenderTreeRemoveLightEvent>(RemoveLight);
        }

        // For the RemoveX methods
        // If the Transform is removed BEFORE the Sprite/Light,
        // then the MapIdChanged code will handle and remove it (because MapId gets set to nullspace).
        // Otherwise these will still have their past MapId and that's all we need..

        #region SpriteHandlers
        private void SpriteMapChanged(EntityUid uid, SpriteComponent component, EntMapIdChangedMessage args)
        {
            QueueSpriteUpdate(component);
        }

        private void SpriteMoved(EntityUid uid, SpriteComponent component, MoveEvent args)
        {
            QueueSpriteUpdate(component);
        }

        private void SpriteParentChanged(EntityUid uid, SpriteComponent component, EntParentChangedMessage args)
        {
            QueueSpriteUpdate(component);
        }

        private void RemoveSprite(EntityUid uid, SpriteComponent component, ComponentRemove args)
        {
            ClearSprite(component);
        }

        private void ClearSprite(SpriteComponent component)
        {
            if (_gridTrees.TryGetValue(component.IntersectingMapId, out var gridTrees))
            {
                foreach (var gridId in component.IntersectingGrids)
                {
                    if (!gridTrees.TryGetValue(gridId, out var tree)) continue;
                    tree.SpriteTree.Remove(component);
                }
            }

            component.IntersectingGrids.Clear();
        }

        private void QueueSpriteUpdate(SpriteComponent component)
        {
            if (component.TreeUpdateQueued) return;

            component.TreeUpdateQueued = true;
            _spriteQueue.Add(component);

            foreach (var child in component.Owner.Transform.Children)
            {
                QueueSpriteUpdate(child.Owner);
            }
        }

        private void QueueSpriteUpdate(IEntity entity)
        {
            if (!entity.TryGetComponent(out SpriteComponent? spriteComponent)) return;
            QueueSpriteUpdate(spriteComponent);

            foreach (var child in entity.Transform.Children)
            {
                QueueSpriteUpdate(child.Owner);
            }
        }
        #endregion

        #region LightHandlers
        private void LightMapChanged(EntityUid uid, PointLightComponent component, EntMapIdChangedMessage args)
        {
            QueueLightUpdate(component);
        }

        private void LightMoved(EntityUid uid, PointLightComponent component, MoveEvent args)
        {
            QueueLightUpdate(component);
        }

        private void LightParentChanged(EntityUid uid, PointLightComponent component, EntParentChangedMessage args)
        {
            QueueLightUpdate(component);
        }

        private void PointLightRadiusChanged(EntityUid uid, PointLightComponent component, PointLightRadiusChangedEvent args)
        {
            QueueLightUpdate(component);
        }

        private void RemoveLight(EntityUid uid, PointLightComponent component, RenderTreeRemoveLightEvent args)
        {
            ClearLight(component);
        }

        private void ClearLight(PointLightComponent component)
        {
            if (_gridTrees.TryGetValue(component.IntersectingMapId, out var gridTrees))
            {
                foreach (var gridId in component.IntersectingGrids)
                {
                    if (!gridTrees.TryGetValue(gridId, out var tree)) continue;
                    tree.LightTree.Remove(component);
                }
            }

            component.IntersectingGrids.Clear();
        }

        private void QueueLightUpdate(PointLightComponent component)
        {
            if (component.TreeUpdateQueued) return;

            component.TreeUpdateQueued = true;
            _lightQueue.Add(component);

            foreach (var child in component.Owner.Transform.Children)
            {
                QueueLightUpdate(child.Owner);
            }
        }

        private void QueueLightUpdate(IEntity entity)
        {
            if (!entity.TryGetComponent(out PointLightComponent? lightComponent)) return;
            QueueLightUpdate(lightComponent);

            foreach (var child in entity.Transform.Children)
            {
                QueueLightUpdate(child.Owner);
            }
        }
        #endregion

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.MapCreated -= MapManagerOnMapCreated;
            _mapManager.MapDestroyed -= MapManagerOnMapDestroyed;
            _mapManager.OnGridCreated -= MapManagerOnGridCreated;
            _mapManager.OnGridRemoved -= MapManagerOnGridRemoved;
        }

        private void MapManagerOnMapDestroyed(object? sender, MapEventArgs e)
        {
            foreach (var (_, gridTree) in _gridTrees[e.Map])
            {
                foreach (var comp in gridTree.LightTree)
                {
                    comp.IntersectingGrids.Clear();
                }

                foreach (var comp in gridTree.SpriteTree)
                {
                    comp.IntersectingGrids.Clear();
                }

                // Just in case?
                gridTree.LightTree.Clear();
                gridTree.SpriteTree.Clear();
            }

            _gridTrees.Remove(e.Map);
        }

        private void MapManagerOnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace)
            {
                return;
            }

            _gridTrees.Add(e.Map, new Dictionary<GridId, MapTrees>
            {
                {GridId.Invalid, new MapTrees()}
            });
        }

        private void MapManagerOnGridCreated(MapId mapId, GridId gridId)
        {
            _gridTrees[mapId].Add(gridId, new MapTrees());
        }

        private void MapManagerOnGridRemoved(MapId mapId, GridId gridId)
        {
            var gridTree = _gridTrees[mapId][gridId];

            foreach (var sprite in gridTree.SpriteTree)
            {
                sprite.IntersectingGrids.Remove(gridId);
            }

            foreach (var light in gridTree.LightTree)
            {
                light.IntersectingGrids.Remove(gridId);
            }

            // Clear in case
            gridTree.LightTree.Clear();
            gridTree.SpriteTree.Clear();
            _gridTrees[mapId].Remove(gridId);
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var sprite in _spriteQueue)
            {
                var mapId = sprite.Owner.Transform.MapID;

                // If we're on a new map then clear the old one.
                if (sprite.IntersectingMapId != mapId)
                {
                    ClearSprite(sprite);
                }

                sprite.IntersectingMapId = mapId;

                if (mapId == MapId.Nullspace) continue;

                var mapTree = _gridTrees[mapId];
                var aabb = MapTrees.SpriteAabbFunc(sprite);
                var intersectingGrids = _mapManager.FindGridIdsIntersecting(mapId, aabb, true).ToList();

                // Remove from old
                foreach (var gridId in sprite.IntersectingGrids)
                {
                    if (intersectingGrids.Contains(gridId)) continue;
                    mapTree[gridId].SpriteTree.Remove(sprite);
                }

                // Rebuild in the update below
                sprite.IntersectingGrids.Clear();

                // Update / add to new
                foreach (var gridId in intersectingGrids)
                {
                    var translated = aabb.Translated(gridId == GridId.Invalid
                        ? Vector2.Zero
                        : -_mapManager.GetGrid(gridId).WorldPosition);

                    mapTree[gridId].SpriteTree.AddOrUpdate(sprite, translated);

                    sprite.IntersectingGrids.Add(gridId);
                }

                sprite.TreeUpdateQueued = false;
            }

            foreach (var light in _lightQueue)
            {
                var mapId = light.Owner.Transform.MapID;

                // If we're on a new map then clear the old one.
                if (light.IntersectingMapId != mapId)
                {
                    ClearLight(light);
                }

                light.IntersectingMapId = mapId;

                if (mapId == MapId.Nullspace) continue;

                var mapTree = _gridTrees[mapId];
                var aabb = MapTrees.LightAabbFunc(light);
                var intersectingGrids = _mapManager.FindGridIdsIntersecting(mapId, aabb, true).ToList();

                // Remove from old
                foreach (var gridId in intersectingGrids)
                {
                    if (intersectingGrids.Contains(gridId)) continue;
                    mapTree[gridId].LightTree.Remove(light);
                }

                // Rebuild in the update below
                light.IntersectingGrids.Clear();

                // Update / add to new
                foreach (var gridId in intersectingGrids)
                {
                    var translated = aabb.Translated(gridId == GridId.Invalid
                        ? Vector2.Zero
                        : -_mapManager.GetGrid(gridId).WorldPosition);

                    mapTree[gridId].LightTree.AddOrUpdate(light, translated);
                    light.IntersectingGrids.Add(gridId);
                }

                light.TreeUpdateQueued = false;
            }

            _spriteQueue.Clear();
            _lightQueue.Clear();
        }

        private sealed class MapTrees
        {
            public readonly DynamicTree<SpriteComponent> SpriteTree;
            public readonly DynamicTree<PointLightComponent> LightTree;

            public MapTrees()
            {
                SpriteTree = new DynamicTree<SpriteComponent>(SpriteAabbFunc);
                LightTree = new DynamicTree<PointLightComponent>(LightAabbFunc);
            }

            internal static Box2 SpriteAabbFunc(in SpriteComponent value)
            {
                var worldPos = value.Owner.Transform.WorldPosition;

                return new Box2(worldPos, worldPos);
            }

            internal static Box2 LightAabbFunc(in PointLightComponent value)
            {
                var worldPos = value.Owner.Transform.WorldPosition;

                var boxSize = value.Radius * 2;
                return Box2.CenteredAround(worldPos, (boxSize, boxSize));
            }
        }
    }

    internal class RenderTreeRemoveLightEvent : EntityEventArgs
    {
        public RenderTreeRemoveLightEvent(PointLightComponent light, MapId map)
        {
            Light = light;
            Map = map;
        }

        public PointLightComponent Light { get; }
        public MapId Map { get; }
    }
}
