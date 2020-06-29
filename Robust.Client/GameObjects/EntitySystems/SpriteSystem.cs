using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Physics;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Updates the layer animation for every visible sprite.
    /// </summary>
    [UsedImplicitly]
    public class SpriteSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        private readonly Dictionary<MapId, DynamicTree<SpriteComponent>> _mapTrees =
            new Dictionary<MapId, DynamicTree<SpriteComponent>>();

        // Queue of sprites to
        private readonly List<SpriteComponent> _updateTreeQueue = new List<SpriteComponent>();

        public DynamicTree<SpriteComponent> GetTreeForMap(MapId map)
        {
            return _mapTrees[map];
        }

        public override void Initialize()
        {
            base.Initialize();

            _mapManager.MapCreated += MapManagerOnMapCreated;
            _mapManager.MapDestroyed += MapManagerOnMapDestroyed;

            SubscribeLocalEvent<EntMapIdChangedMessage>(EntMapIdChanged);
            SubscribeLocalEvent<MoveEvent>(EntMoved);
            SubscribeLocalEvent<EntParentChangedMessage>(EntParentChanged);

            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));
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
            if (entity.TryGetComponent(out SpriteComponent spriteComponent))
            {
                if (!spriteComponent.TreeUpdateQueued)
                {
                    spriteComponent.TreeUpdateQueued = true;

                    _updateTreeQueue.Add(spriteComponent);
                }
            }

            foreach (var child in entity.Transform.Children)
            {
                UpdateEntity(child.Owner);
            }
        }

        private void EntMapIdChanged(EntMapIdChangedMessage ev)
        {
            if (!ev.Entity.TryGetComponent(out SpriteComponent sprite))
            {
                return;
            }

            var mapTree = _mapTrees[ev.OldMapId];
            mapTree.Remove(sprite);

            mapTree = _mapTrees[ev.Entity.Transform.MapID];
            mapTree.AddOrUpdate(sprite);
        }

        private void MapManagerOnMapDestroyed(object? sender, MapEventArgs e)
        {
            _mapTrees.Remove(e.Map);
        }

        private void MapManagerOnMapCreated(object? sender, MapEventArgs e)
        {
            _mapTrees.Add(e.Map, new DynamicTree<SpriteComponent>(TreeExtractAabbFunc));
        }

        private static Box2 TreeExtractAabbFunc(in SpriteComponent value)
        {
            var worldPos = value.Owner.Transform.WorldPosition;

            return new Box2(worldPos, worldPos);
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            foreach (var queuedUpdateSprite in _updateTreeQueue)
            {
                var transform = queuedUpdateSprite.Owner.Transform;
                var map = transform.MapID;
                var updateMapTree = _mapTrees[map];

                updateMapTree.AddOrUpdate(queuedUpdateSprite);
                queuedUpdateSprite.TreeUpdateQueued = false;
            }

            _updateTreeQueue.Clear();

            // So we could calculate the correct size of the entities based on the contents of their sprite...
            // Or we can just assume that no entity is larger than 10x10 and get a stupid easy check.
            var pvsBounds = _eyeManager.GetWorldViewport().Enlarged(5);

            var mapTree = _mapTrees[_eyeManager.CurrentMap];

            var pvsEntities = mapTree.Query(pvsBounds, true);

            foreach (var sprite in pvsEntities)
            {
                if (sprite.IsInert)
                {
                    continue;
                }

                sprite.FrameUpdate(frameTime);
            }
        }
    }
}
