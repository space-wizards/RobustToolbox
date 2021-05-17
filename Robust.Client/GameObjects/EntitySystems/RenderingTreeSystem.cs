using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

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

            SubscribeLocalEvent<EntMapIdChangedMessage>(EntMapIdChanged);
            SubscribeLocalEvent<MoveEvent>(EntMoved);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<PointLightRadiusChangedMessage>(PointLightRadiusChanged);
            SubscribeLocalEvent<RenderTreeRemoveSpriteMessage>(RemoveSprite);
            SubscribeLocalEvent<RenderTreeRemoveLightMessage>(RemoveLight);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.MapCreated -= MapManagerOnMapCreated;
            _mapManager.MapDestroyed -= MapManagerOnMapDestroyed;
            _mapManager.OnGridCreated -= MapManagerOnGridCreated;
            _mapManager.OnGridRemoved -= MapManagerOnGridRemoved;
        }

        // For these next 2 methods (the Remove* ones):
        // If the Transform is removed BEFORE the Sprite/Light,
        // then the MapIdChanged code will handle and remove it (because MapId gets set to nullspace).
        // Otherwise these will still have their past MapId and that's all we need..
        private void RemoveLight(RenderTreeRemoveLightMessage ev)
        {
            foreach (var gridId in _mapManager.FindGridIdsIntersecting(ev.Map, MapTrees.LightAabbFunc(ev.Light), true))
            {
                _gridTrees[ev.Map][gridId].LightTree.Remove(ev.Light);
            }
        }

        private void RemoveSprite(RenderTreeRemoveSpriteMessage ev)
        {
            foreach (var gridId in _mapManager.FindGridIdsIntersecting(ev.Map, MapTrees.SpriteAabbFunc(ev.Sprite), true))
            {
                _gridTrees[ev.Map][gridId].SpriteTree.Remove(ev.Sprite);
            }
        }

        private void PointLightRadiusChanged(PointLightRadiusChangedMessage ev)
        {
            QueueUpdateLight(ev.PointLightComponent);
        }

        private void EntParentChanged(EntParentChangedMessage ev)
        {
            UpdateEntity(ev.Entity);
        }

        private void EntMoved(MoveEvent ev)
        {
            UpdateEntity(ev.Sender);
        }

        private void UpdateEntity(IEntity entity)
        {
            if (entity.TryGetComponent(out SpriteComponent? spriteComponent))
            {
                if (!spriteComponent.TreeUpdateQueued)
                {
                    spriteComponent.TreeUpdateQueued = true;

                    _spriteQueue.Add(spriteComponent);
                }
            }

            if (entity.TryGetComponent(out PointLightComponent? light))
            {
                QueueUpdateLight(light);
            }

            foreach (var child in entity.Transform.ChildEntityUids)
            {
                UpdateEntity(EntityManager.GetEntity(child));
            }
        }

        private void QueueUpdateLight(PointLightComponent light)
        {
            if (!light.TreeUpdateQueued)
            {
                light.TreeUpdateQueued = true;

                _lightQueue.Add(light);
            }
        }

        private void EntMapIdChanged(EntMapIdChangedMessage ev)
        {
            // Nullspace is a valid map ID for stuff to have but we also aren't gonna bother indexing it.
            // So that's why there's a GetValueOrDefault.
            var oldMapTrees = _gridTrees.GetValueOrDefault(ev.OldMapId);
            var newMapTrees = _gridTrees.GetValueOrDefault(ev.Entity.Transform.MapID);

            // TODO: MMMM probably a better way to do this.
            if (ev.Entity.TryGetComponent(out SpriteComponent? sprite))
            {
                if (oldMapTrees != null)
                {
                    foreach (var (_, gridTree) in oldMapTrees)
                    {
                        gridTree.SpriteTree.Remove(sprite);
                    }
                }

                var bounds = MapTrees.SpriteAabbFunc(sprite);

                foreach (var gridId in _mapManager.FindGridIdsIntersecting(ev.Entity.Transform.MapID, bounds, true))
                {
                    var gridBounds = gridId == GridId.Invalid
                        ? bounds : bounds.Translated(-_mapManager.GetGrid(gridId).WorldPosition);

                    newMapTrees?[gridId].SpriteTree.AddOrUpdate(sprite, gridBounds);
                }
            }

            if (ev.Entity.TryGetComponent(out PointLightComponent? light))
            {
                if (oldMapTrees != null)
                {
                    foreach (var (_, gridTree) in oldMapTrees)
                    {
                        gridTree.LightTree.Remove(light);
                    }
                }

                var bounds = MapTrees.LightAabbFunc(light);

                foreach (var gridId in _mapManager.FindGridIdsIntersecting(ev.Entity.Transform.MapID, bounds, true))
                {
                    var gridBounds = gridId == GridId.Invalid
                        ? bounds : bounds.Translated(-_mapManager.GetGrid(gridId).WorldPosition);

                    newMapTrees?[gridId].LightTree.AddOrUpdate(light, gridBounds);
                }
            }
        }

        private void MapManagerOnMapDestroyed(object? sender, MapEventArgs e)
        {
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
            _gridTrees[mapId].Remove(gridId);
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var queuedUpdateSprite in _spriteQueue)
            {
                var map = queuedUpdateSprite.Owner.Transform.MapID;
                if (map == MapId.Nullspace)
                {
                    continue;
                }

                var mapTree = _gridTrees[map];

                foreach (var gridId in _mapManager.FindGridIdsIntersecting(map,
                    MapTrees.SpriteAabbFunc(queuedUpdateSprite), true))
                {
                    mapTree[gridId].SpriteTree.AddOrUpdate(queuedUpdateSprite);
                }

                queuedUpdateSprite.TreeUpdateQueued = false;
            }

            foreach (var queuedUpdateLight in _lightQueue)
            {
                var map = queuedUpdateLight.Owner.Transform.MapID;
                if (map == MapId.Nullspace)
                {
                    continue;
                }

                var mapTree = _gridTrees[map];

                foreach (var gridId in _mapManager.FindGridIdsIntersecting(map,
                    MapTrees.LightAabbFunc(queuedUpdateLight), true))
                {
                    mapTree[gridId].LightTree.AddOrUpdate(queuedUpdateLight);
                }

                queuedUpdateLight.TreeUpdateQueued = false;
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

    internal struct RenderTreeRemoveSpriteMessage
    {
        public RenderTreeRemoveSpriteMessage(SpriteComponent sprite, MapId map)
        {
            Sprite = sprite;
            Map = map;
        }

        public SpriteComponent Sprite { get; }
        public MapId Map { get; }
    }

    internal struct RenderTreeRemoveLightMessage
    {
        public RenderTreeRemoveLightMessage(PointLightComponent light, MapId map)
        {
            Light = light;
            Map = map;
        }

        public PointLightComponent Light { get; }
        public MapId Map { get; }
    }
}
