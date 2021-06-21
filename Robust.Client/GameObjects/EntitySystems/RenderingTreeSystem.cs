using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

        private HashSet<EntityUid> _checkedChildren = new();

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

            // Due to how recursion works, this must be done.
            SubscribeLocalEvent<MoveEvent>(AnythingMoved);

            SubscribeLocalEvent<SpriteComponent, EntMapIdChangedMessage>(SpriteMapChanged);
            SubscribeLocalEvent<SpriteComponent, EntParentChangedMessage>(SpriteParentChanged);
            SubscribeLocalEvent<SpriteComponent, ComponentRemove>(RemoveSprite);
            SubscribeLocalEvent<SpriteComponent, SpriteUpdateEvent>(HandleSpriteUpdate);

            SubscribeLocalEvent<PointLightComponent, EntMapIdChangedMessage>(LightMapChanged);
            SubscribeLocalEvent<PointLightComponent, EntParentChangedMessage>(LightParentChanged);
            SubscribeLocalEvent<PointLightComponent, PointLightRadiusChangedEvent>(PointLightRadiusChanged);
            SubscribeLocalEvent<PointLightComponent, RenderTreeRemoveLightEvent>(RemoveLight);
            SubscribeLocalEvent<PointLightComponent, PointLightUpdateEvent>(HandleLightUpdate);
        }

        private void HandleLightUpdate(EntityUid uid, PointLightComponent component, PointLightUpdateEvent args)
        {
            if (component.TreeUpdateQueued) return;
            QueueLightUpdate(component);
        }

        private void HandleSpriteUpdate(EntityUid uid, SpriteComponent component, SpriteUpdateEvent args)
        {
            if (component.TreeUpdateQueued) return;
            QueueSpriteUpdate(component);
        }

        private void AnythingMoved(MoveEvent args)
        {
            AnythingMovedSubHandler(args.Sender.Transform);
        }

        private void AnythingMovedSubHandler(ITransformComponent sender)
        {
            // To avoid doing redundant updates (and we don't need to update a grid's children ever)
            if (!_checkedChildren.Add(sender.Owner.Uid) ||
                sender.Owner.HasComponent<MapGridComponent>() ||
                sender.Owner.HasComponent<MapComponent>()) return;

            // This recursive search is needed, as MoveEvent is defined to not care about indirect events like children.
            // WHATEVER YOU DO, DON'T REPLACE THIS WITH SPAMMING EVENTS UNLESS YOU HAVE A GUARANTEE IT WON'T LAG THE GC.
            // (Struct-based events ok though)
            if (sender.Owner.TryGetComponent(out SpriteComponent? sprite))
                QueueSpriteUpdate(sprite);

            if (sender.Owner.TryGetComponent(out PointLightComponent? light))
                QueueLightUpdate(light);

            foreach (ITransformComponent child in sender.Children)
            {
                AnythingMovedSubHandler(child);
            }
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
            if (_gridTrees.TryGetValue(component.TreeMapId, out var gridTrees))
            {
                if (gridTrees.TryGetValue(component.TreeGridId, out var tree))
                {
                    tree.SpriteTree.Remove(component);
                }
            }

            component.TreeGridId = GridId.Invalid;
            component.TreeMapId = MapId.Nullspace;
        }

        private void QueueSpriteUpdate(SpriteComponent component)
        {
            if (component.TreeUpdateQueued) return;

            component.TreeUpdateQueued = true;
            _spriteQueue.Add(component);
        }
        #endregion

        #region LightHandlers
        private void LightMapChanged(EntityUid uid, PointLightComponent component, EntMapIdChangedMessage args)
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
            if (_gridTrees.TryGetValue(component.TreeMapId, out var gridTrees))
            {
                if (gridTrees.TryGetValue(component.TreeGridId, out var tree))
                {
                    tree.LightTree.Remove(component);
                }
            }

            component.TreeMapId = MapId.Nullspace;
            component.TreeGridId = GridId.Invalid;
        }

        private void QueueLightUpdate(PointLightComponent component)
        {
            if (component.TreeUpdateQueued) return;

            component.TreeUpdateQueued = true;
            _lightQueue.Add(component);
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
                // Don't use ClearSprite / ClearLight as we'll just clear the whole tree at the end.
                foreach (var comp in gridTree.LightTree)
                {
                    comp.TreeMapId = MapId.Nullspace;
                    comp.TreeGridId = GridId.Invalid;
                }

                foreach (var comp in gridTree.SpriteTree)
                {
                    comp.TreeMapId = MapId.Nullspace;
                    comp.TreeGridId = GridId.Invalid;
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
                sprite.TreeMapId = MapId.Nullspace;
                sprite.TreeGridId = GridId.Invalid;
            }

            foreach (var light in gridTree.LightTree)
            {
                light.TreeMapId = MapId.Nullspace;
                light.TreeGridId = GridId.Invalid;
            }

            // Clear in case
            gridTree.LightTree.Clear();
            gridTree.SpriteTree.Clear();
            _gridTrees[mapId].Remove(gridId);
        }

        public override void FrameUpdate(float frameTime)
        {
            _checkedChildren.Clear();

            foreach (var sprite in _spriteQueue)
            {
                sprite.TreeUpdateQueued = false;
                var mapId = sprite.Owner.Transform.MapID;

                if (!sprite.Visible || sprite.ContainerOccluded)
                {
                    ClearSprite(sprite);
                    continue;
                }

                // If we're on a new map then clear the old one.
                if (sprite.TreeMapId != mapId)
                {
                    ClearSprite(sprite);
                }

                sprite.TreeMapId = mapId;

                if (mapId == MapId.Nullspace) continue;

                var mapTree = _gridTrees[mapId];

                var oldGridId = sprite.TreeGridId;
                var gridId = sprite.Owner.Transform.GridID;

                if (oldGridId != gridId)
                {
                    if (mapTree.TryGetValue(oldGridId, out var tree))
                    {
                        tree.SpriteTree.Remove(sprite);
                    }

                    sprite.TreeGridId = gridId;
                }
            }

            foreach (var light in _lightQueue)
            {
                light.TreeUpdateQueued = false;
                var mapId = light.Owner.Transform.MapID;

                if (!light.Enabled || light.ContainerOccluded)
                {
                    ClearLight(light);
                    continue;
                }

                // If we're on a new map then clear the old one.
                if (light.TreeMapId != mapId)
                {
                    ClearLight(light);
                }

                light.TreeMapId = mapId;

                if (mapId == MapId.Nullspace) continue;

                var mapTree = _gridTrees[mapId];

                var oldGridId = light.TreeGridId;
                var gridId = light.Owner.Transform.GridID;

                if (oldGridId != gridId)
                {
                    if (mapTree.TryGetValue(oldGridId, out var tree))
                    {
                        tree.LightTree.Remove(light);
                    }

                    light.TreeGridId = gridId;
                }
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
