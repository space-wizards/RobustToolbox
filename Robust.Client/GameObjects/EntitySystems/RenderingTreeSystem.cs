using System;
using System.Collections.Generic;
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

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Keeps track of <see cref="DynamicTree{T}"/>s for various rendering-related components.
    /// </summary>
    [UsedImplicitly]
    public sealed class RenderingTreeSystem : EntitySystem
    {
        [Dependency] private readonly TransformSystem _xformSystem = default!;

        internal const string LoggerSawmill = "rendertree";

        // Nullspace is not indexed. Keep that in mind.

        [Dependency] private readonly IMapManager _mapManager = default!;

        private readonly List<SpriteComponent> _spriteQueue = new();
        private readonly List<PointLightComponent> _lightQueue = new();

        private readonly HashSet<EntityUid> _checkedChildren = new();

        /// <summary>
        /// <see cref="CVars.MaxLightRadius"/>
        /// </summary>
        public float MaxLightRadius { get; private set; }

        internal IEnumerable<RenderingTreeComponent> GetRenderTrees(MapId mapId, Box2Rotated worldBounds)
        {
            if (mapId == MapId.Nullspace) yield break;

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                var tempQualifier = grid.GridEntityId;
                yield return EntityManager.GetComponent<RenderingTreeComponent>(tempQualifier);
            }

            var tempQualifier1 = _mapManager.GetMapEntityId(mapId);
            yield return EntityManager.GetComponent<RenderingTreeComponent>(tempQualifier1);
        }

        internal IEnumerable<RenderingTreeComponent> GetRenderTrees(MapId mapId, Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) yield break;

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                var tempQualifier = grid.GridEntityId;
                yield return EntityManager.GetComponent<RenderingTreeComponent>(tempQualifier);
            }

            var tempQualifier1 = _mapManager.GetMapEntityId(mapId);
            yield return EntityManager.GetComponent<RenderingTreeComponent>(tempQualifier1);
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdatesBefore.Add(typeof(SpriteSystem));
            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));

            SubscribeLocalEvent<MapChangedEvent>(MapManagerOnMapCreated);

            SubscribeLocalEvent<GridInitializeEvent>(MapManagerOnGridCreated);

            // Due to how recursion works, this must be done.
            // Note that this also implicitly handles parent changes.
            SubscribeLocalEvent<MoveEvent>(AnythingMoved);

            SubscribeLocalEvent<SpriteComponent, ComponentRemove>(RemoveSprite);
            SubscribeLocalEvent<SpriteComponent, UpdateSpriteTreeEvent>(HandleSpriteUpdate);

            SubscribeLocalEvent<PointLightComponent, PointLightRadiusChangedEvent>(PointLightRadiusChanged);
            SubscribeLocalEvent<PointLightComponent, PointLightUpdateEvent>(HandleLightUpdate);

            SubscribeLocalEvent<RenderingTreeComponent, ComponentInit>(OnTreeInit);
            SubscribeLocalEvent<RenderingTreeComponent, ComponentRemove>(OnTreeRemove);

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.MaxLightRadius, value => MaxLightRadius = value, true);
        }

        private void OnTreeInit(EntityUid uid, RenderingTreeComponent component, ComponentInit args)
        {
            component.LightTree = new(LightAabbFunc);
            component.SpriteTree = new(SpriteAabbFunc);
        }

        private void HandleLightUpdate(EntityUid uid, PointLightComponent component, PointLightUpdateEvent args)
        {
            if (component.TreeUpdateQueued) return;
            QueueLightUpdate(component);
        }

        private void HandleSpriteUpdate(EntityUid uid, SpriteComponent component, UpdateSpriteTreeEvent args)
        {
            _spriteQueue.Add(component);
        }

        private void AnythingMoved(ref MoveEvent args)
        {
            var pointQuery = EntityManager.GetEntityQuery<PointLightComponent>();
            var spriteQuery = EntityManager.GetEntityQuery<SpriteComponent>();
            var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
            var renderingQuery = EntityManager.GetEntityQuery<RenderingTreeComponent>();

            AnythingMovedSubHandler(args.Sender, args.Component, xformQuery, pointQuery, spriteQuery, renderingQuery);
        }

        private void AnythingMovedSubHandler(
            EntityUid uid,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PointLightComponent> pointQuery,
            EntityQuery<SpriteComponent> spriteQuery,
            EntityQuery<RenderingTreeComponent> renderingQuery)
        {
            // To avoid doing redundant updates (and we don't need to update a grid's children ever)
            if (!_checkedChildren.Add(uid) || renderingQuery.HasComponent(uid)) return;

            // This recursive search is needed, as MoveEvent is defined to not care about indirect events like children.
            // WHATEVER YOU DO, DON'T REPLACE THIS WITH SPAMMING EVENTS UNLESS YOU HAVE A GUARANTEE IT WON'T LAG THE GC.
            // (Struct-based events ok though)
            // Ironically this was lagging the GC lolz
            if (spriteQuery.TryGetComponent(uid, out var sprite))
                QueueSpriteUpdate(sprite);

            if (pointQuery.TryGetComponent(uid, out var light))
                QueueLightUpdate(light);

            var childEnumerator = xform.ChildEnumerator;

            while (childEnumerator.MoveNext(out var child))
            {
                if (xformQuery.TryGetComponent(uid, out var childXform))
                    AnythingMovedSubHandler(child.Value, childXform, xformQuery, pointQuery, spriteQuery, renderingQuery);
            }
        }

        // For the RemoveX methods
        // If the Transform is removed BEFORE the Sprite/Light,
        // then the MapIdChanged code will handle and remove it (because MapId gets set to nullspace).
        // Otherwise these will still have their past MapId and that's all we need..

        #region SpriteHandlers

        private void RemoveSprite(EntityUid uid, SpriteComponent component, ComponentRemove args)
        {
            ClearSprite(component);
        }

        private void ClearSprite(SpriteComponent component)
        {
            if (component.RenderTree == null) return;

            component.RenderTree.SpriteTree.Remove(new() { Component = component });
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

        private void PointLightRadiusChanged(EntityUid uid, PointLightComponent component, PointLightRadiusChangedEvent args)
        {
            QueueLightUpdate(component);
        }

        public void ClearLight(PointLightComponent component)
        {
            if (component.RenderTree == null) return;

            component.RenderTree.LightTree.Remove(new() { Component = component });
            component.RenderTree = null;
        }

        private void QueueLightUpdate(PointLightComponent component)
        {
            if (component.TreeUpdateQueued) return;

            component.TreeUpdateQueued = true;
            _lightQueue.Add(component);
        }
        #endregion

        private void OnTreeRemove(EntityUid uid, RenderingTreeComponent component, ComponentRemove args)
        {
            foreach (var sprite in component.SpriteTree)
            {
                sprite.Component.RenderTree = null;
            }

            foreach (var light in component.LightTree)
            {
                light.Component.RenderTree = null;
            }

            component.SpriteTree.Clear();
            component.LightTree.Clear();
        }

        private void MapManagerOnMapCreated(MapChangedEvent e)
        {
            if (e.Destroyed || e.Map == MapId.Nullspace)
            {
                return;
            }

            EntityManager.EnsureComponent<RenderingTreeComponent>(_mapManager.GetMapEntityId(e.Map));
        }

        private void MapManagerOnGridCreated(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<RenderingTreeComponent>(_mapManager.GetGrid(ev.EntityUid).GridEntityId);
        }

        private RenderingTreeComponent? GetRenderTree(EntityUid entity, TransformComponent xform, EntityQuery<TransformComponent> xforms)
        {
            var lookups = EntityManager.GetEntityQuery<RenderingTreeComponent>();

            if (!EntityManager.EntityExists(entity) ||
                xform.MapID == MapId.Nullspace ||
                lookups.HasComponent(entity)) return null;

            var parent = xform.ParentUid;

            while (parent.IsValid())
            {
                if (lookups.TryGetComponent(parent, out var comp)) return comp;
                parent = xforms.GetComponent(parent).ParentUid;
            }

            return null;
        }

        private bool IsVisible(SpriteComponent component)
        {
            return component.Visible && !component.ContainerOccluded && !component.Deleted;
        }

        public override void FrameUpdate(float frameTime)
        {
            _checkedChildren.Clear();

            var xforms = EntityManager.GetEntityQuery<TransformComponent>();

            foreach (var sprite in _spriteQueue)
            {
                sprite.TreeUpdateQueued = false;
                if (!IsVisible(sprite))
                {
                    ClearSprite(sprite);
                    continue;
                }

                var xform = xforms.GetComponent(sprite.Owner);
                var oldMapTree = sprite.RenderTree;
                var newMapTree = GetRenderTree(sprite.Owner, xform, xforms);
                // TODO: Temp PVS guard
                var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(xform, xforms);

                if (float.IsNaN(worldPos.X) || float.IsNaN(worldPos.Y))
                {
                    ClearSprite(sprite);
                    continue;
                }

                var aabb = SpriteAabbFunc(sprite, xform, worldPos, worldRot, xforms);

                // If we're on a new map then clear the old one.
                if (oldMapTree != newMapTree)
                {
                    ClearSprite(sprite);
                    newMapTree?.SpriteTree.Add((sprite,xform) , aabb);
                }
                else
                {
                    newMapTree?.SpriteTree.Update((sprite, xform), aabb);
                }

                sprite.RenderTree = newMapTree;
            }

            foreach (var light in _lightQueue)
            {
                light.TreeUpdateQueued = false;

                if (light.Deleted || !light.Enabled || light.ContainerOccluded)
                {
                    ClearLight(light);
                    continue;
                }

                var xform = xforms.GetComponent(light.Owner);
                var oldMapTree = light.RenderTree;
                var newMapTree = GetRenderTree(light.Owner, xform, xforms);
                // TODO: Temp PVS guard
                var worldPos = _xformSystem.GetWorldPosition(xform, xforms);

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
                var aabb = LightAabbFunc(light, xform, worldPos, xforms);

                // If we're on a new map then clear the old one.
                if (oldMapTree != newMapTree)
                {
                    ClearLight(light);
                    newMapTree?.LightTree.Add((light, xform), aabb);
                }
                else
                {
                    newMapTree?.LightTree.Update((light, xform), aabb);
                }

                light.RenderTree = newMapTree;
            }

            _spriteQueue.Clear();
            _lightQueue.Clear();
        }

        private Box2 SpriteAabbFunc(in ComponentTreeEntry<SpriteComponent> entry)
        {
            var xforms = EntityManager.GetEntityQuery<TransformComponent>();

            var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(entry.Transform, xforms);

            return SpriteAabbFunc(entry.Component, entry.Transform, worldPos, worldRot, xforms);
        }

        private Box2 LightAabbFunc(in ComponentTreeEntry<PointLightComponent> entry)
        {
            var xforms = EntityManager.GetEntityQuery<TransformComponent>();
            var worldPos = _xformSystem.GetWorldPosition(entry.Transform, xforms);
            var tree = GetRenderTree(entry.Uid, entry.Transform, xforms);
            var boxSize = entry.Component.Radius * 2;

            var localPos = tree == null ? worldPos : _xformSystem.GetInvWorldMatrix(tree.Owner, xforms).Transform(worldPos);
            return Box2.CenteredAround(localPos, (boxSize, boxSize));
        }

        private Box2 SpriteAabbFunc(SpriteComponent value, TransformComponent xform, Vector2 worldPos, Angle worldRot, EntityQuery<TransformComponent> xforms)
        {
            var bounds = value.CalculateRotatedBoundingBox(worldPos, worldRot);
            var tree = GetRenderTree(value.Owner, xform, xforms);

            return tree == null ? bounds.CalcBoundingBox() : _xformSystem.GetInvWorldMatrix(tree.Owner, xforms).TransformBox(bounds);
        }

        private Box2 LightAabbFunc(PointLightComponent value, TransformComponent xform, Vector2 worldPos, EntityQuery<TransformComponent> xforms)
        {
            // Lights are circles so don't need entity's rotation
            var tree = GetRenderTree(value.Owner, xform, xforms);
            var boxSize = value.Radius * 2;

            var localPos = tree == null ? worldPos : xforms.GetComponent(tree.Owner).InvWorldMatrix.Transform(worldPos);
            return Box2.CenteredAround(localPos, (boxSize, boxSize));
        }
    }
}
