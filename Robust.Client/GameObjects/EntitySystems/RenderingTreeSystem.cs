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

        private readonly Dictionary<MapId, MapTrees> _mapTrees = new();

        private readonly List<SpriteComponent> _spriteQueue = new();
        private readonly List<PointLightComponent> _lightQueue = new();

        internal DynamicTree<SpriteComponent> GetSpriteTreeForMap(MapId map)
        {
            return _mapTrees[map].SpriteTree;
        }

        internal DynamicTree<PointLightComponent> GetLightTreeForMap(MapId map)
        {
            return _mapTrees[map].LightTree;
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SpriteSystem));
            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));

            _mapManager.MapCreated += MapManagerOnMapCreated;
            _mapManager.MapDestroyed += MapManagerOnMapDestroyed;

            SubscribeLocalEvent<EntMapIdChangedMessage>(EntMapIdChanged);
            SubscribeLocalEvent<MoveEvent>(EntMoved);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<PointLightRadiusChangedMessage>(PointLightRadiusChanged);
            SubscribeLocalEvent<RenderTreeRemoveSpriteMessage>(RemoveSprite);
            SubscribeLocalEvent<RenderTreeRemoveLightMessage>(RemoveLight);
        }

        // For these next 2 methods (the Remove* ones):
        // If the Transform is removed BEFORE the Sprite/Light,
        // then the MapIdChanged code will handle and remove it (because MapId gets set to nullspace).
        // Otherwise these will still have their past MapId and that's all we need..
        private void RemoveLight(RenderTreeRemoveLightMessage ev)
        {
            _mapTrees[ev.Map].LightTree.Remove(ev.Light);
        }

        private void RemoveSprite(RenderTreeRemoveSpriteMessage ev)
        {
            _mapTrees[ev.Map].SpriteTree.Remove(ev.Sprite);
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
            var oldMapTrees = _mapTrees.GetValueOrDefault(ev.OldMapId);
            var newMapTrees = _mapTrees.GetValueOrDefault(ev.Entity.Transform.MapID);

            if (ev.Entity.TryGetComponent(out SpriteComponent? sprite))
            {
                oldMapTrees?.SpriteTree.Remove(sprite);

                newMapTrees?.SpriteTree.AddOrUpdate(sprite);
            }

            if (ev.Entity.TryGetComponent(out PointLightComponent? light))
            {
                oldMapTrees?.LightTree.Remove(light);

                newMapTrees?.LightTree.AddOrUpdate(light);
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

            _mapTrees.Add(e.Map, new MapTrees());
        }

        public override void FrameUpdate(float frameTime)
        {
            foreach (var queuedUpdateSprite in _spriteQueue)
            {
                var transform = queuedUpdateSprite.Owner.Transform;
                var map = transform.MapID;
                if (map == MapId.Nullspace)
                {
                    continue;
                }
                var updateMapTree = _mapTrees[map].SpriteTree;

                updateMapTree.AddOrUpdate(queuedUpdateSprite);
                queuedUpdateSprite.TreeUpdateQueued = false;
            }

            foreach (var queuedUpdateLight in _lightQueue)
            {
                var transform = queuedUpdateLight.Owner.Transform;
                var map = transform.MapID;
                if (map == MapId.Nullspace)
                {
                    continue;
                }
                var updateMapTree = _mapTrees[map].LightTree;

                updateMapTree.AddOrUpdate(queuedUpdateLight);
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

            private static Box2 SpriteAabbFunc(in SpriteComponent value)
            {
                var worldPos = value.Owner.Transform.WorldPosition;

                return new Box2(worldPos, worldPos);
            }

            private static Box2 LightAabbFunc(in PointLightComponent value)
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
