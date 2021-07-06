using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
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
        internal const string LoggerSawmill = "rendertree";

        // Nullspace is not indexed. Keep that in mind.

        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly List<SpriteComponent> _spriteQueue = new();
        private readonly List<PointLightComponent> _lightQueue = new();

        private HashSet<EntityUid> _checkedChildren = new();

        /// <summary>
        /// <see cref="CVars.MaxLightRadius"/>
        /// </summary>
        public float MaxLightRadius { get; private set; }

        internal IEnumerable<RenderingTreeComponent> GetRenderTrees(MapId mapId, Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) yield break;

            var enclosed = false;

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                yield return EntityManager.GetEntity(grid.GridEntityId).GetComponent<RenderingTreeComponent>();

                // if we're enclosed then we know no other grids relevant + don't need the map's rendertree
                if (grid.WorldBounds.Encloses(in worldAABB))
                {
                    enclosed = true;
                    break;
                }
            }

            if (!enclosed)
                yield return _mapManager.GetMapEntity(mapId).GetComponent<RenderingTreeComponent>();
        }

        internal IEnumerable<DynamicTree<SpriteComponent>> GetSpriteTrees(MapId mapId, Box2 worldAABB)
        {
            foreach (var comp in GetRenderTrees(mapId, worldAABB))
            {
                yield return comp.SpriteTree;
            }
        }

        internal IEnumerable<DynamicTree<PointLightComponent>> GetLightTrees(MapId mapId, Box2 worldAABB)
        {
            foreach (var comp in GetRenderTrees(mapId, worldAABB))
            {
                yield return comp.LightTree;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SpriteSystem));
            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));

            _mapManager.MapCreated += MapManagerOnMapCreated;
            _mapManager.OnGridCreated += MapManagerOnGridCreated;

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

            SubscribeLocalEvent<RenderingTreeComponent, ComponentRemove>(HandleTreeRemove);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.MaxLightRadius, value => MaxLightRadius = value, true);
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
                sender.Owner.HasComponent<RenderingTreeComponent>()) return;

            // This recursive search is needed, as MoveEvent is defined to not care about indirect events like children.
            // WHATEVER YOU DO, DON'T REPLACE THIS WITH SPAMMING EVENTS UNLESS YOU HAVE A GUARANTEE IT WON'T LAG THE GC.
            // (Struct-based events ok though)
            // Ironically this was lagging the GC lolz
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
            if (component.RenderTree == null) return;

            component.RenderTree.SpriteTree.Remove(component);
            component.RenderTree = null;
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
            if (component.RenderTree == null) return;

            component.RenderTree.LightTree.Remove(component);
            component.RenderTree = null;
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
            _mapManager.OnGridCreated -= MapManagerOnGridCreated;
        }

        private void HandleTreeRemove(EntityUid uid, RenderingTreeComponent component, ComponentRemove args)
        {
            foreach (var sprite in component.SpriteTree)
            {
                sprite.RenderTree = null;
            }

            foreach (var light in component.LightTree)
            {
                light.RenderTree = null;
            }

            component.SpriteTree.Clear();
            component.LightTree.Clear();
        }

        private void MapManagerOnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace)
            {
                return;
            }

            _mapManager.GetMapEntity(e.Map).EnsureComponent<RenderingTreeComponent>();
        }

        private void MapManagerOnGridCreated(MapId mapId, GridId gridId)
        {
            EntityManager.GetEntity(_mapManager.GetGrid(gridId).GridEntityId).EnsureComponent<RenderingTreeComponent>();
        }

        internal static RenderingTreeComponent? GetRenderTree(IEntity entity)
        {
            if (entity.Transform.MapID == MapId.Nullspace ||
                entity.HasComponent<RenderingTreeComponent>()) return null;

            var parent = entity.Transform.Parent?.Owner;

            while (true)
            {
                if (parent == null) break;

                if (parent.TryGetComponent(out RenderingTreeComponent? comp)) return comp;
                parent = parent.Transform.Parent?.Owner;
            }

            return null;
        }

        public override void FrameUpdate(float frameTime)
        {
            _checkedChildren.Clear();

            foreach (var sprite in _spriteQueue)
            {
                sprite.TreeUpdateQueued = false;
                if (!sprite.Visible || sprite.ContainerOccluded)
                {
                    ClearSprite(sprite);
                    continue;
                }

                var oldMapTree = sprite.RenderTree;
                var newMapTree = GetRenderTree(sprite.Owner);
                // TODO: Temp PVS guard
                var worldPos = sprite.Owner.Transform.WorldPosition;

                if (float.IsNaN(worldPos.X) || float.IsNaN(worldPos.Y))
                {
                    ClearSprite(sprite);
                    continue;
                }

                var aabb = RenderingTreeComponent.SpriteAabbFunc(sprite, worldPos);

                // If we're on a new map then clear the old one.
                if (oldMapTree != newMapTree)
                {
                    ClearSprite(sprite);
                    newMapTree?.SpriteTree.Add(sprite, aabb);
                }
                else
                {
                    newMapTree?.SpriteTree.Update(sprite, aabb);
                }

                sprite.RenderTree = newMapTree;
            }

            foreach (var light in _lightQueue)
            {
                light.TreeUpdateQueued = false;

                if (!light.Enabled || light.ContainerOccluded)
                {
                    ClearLight(light);
                    continue;
                }

                var oldMapTree = light.RenderTree;
                var newMapTree = GetRenderTree(light.Owner);
                // TODO: Temp PVS guard
                var worldPos = light.Owner.Transform.WorldPosition;

                if (float.IsNaN(worldPos.X) || float.IsNaN(worldPos.Y))
                {
                    ClearLight(light);
                    continue;
                }

                // TODO: Events need a bit of cleanup so we only validate this on initialize and radius changed events
                // this is fine for now IMO as it's 1 float check for every light that moves
                if (light.Radius > MaxLightRadius)
                {
                    Logger.WarningS(LoggerSawmill, $"Light radius for {light.Owner} set above max radius of {MaxLightRadius}. This may lead to pop-in.");
                }

                var treePos = newMapTree?.Owner.Transform.WorldPosition ?? Vector2.Zero;
                var aabb = RenderingTreeComponent.LightAabbFunc(light, worldPos).Translated(-treePos);

                // If we're on a new map then clear the old one.
                if (oldMapTree != newMapTree)
                {
                    ClearLight(light);
                    newMapTree?.LightTree.Add(light, aabb);
                }
                else
                {
                    newMapTree?.LightTree.Update(light, aabb);
                }

                light.RenderTree = newMapTree;
            }

            _spriteQueue.Clear();
            _lightQueue.Clear();
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
