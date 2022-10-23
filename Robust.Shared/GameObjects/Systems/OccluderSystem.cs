using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public abstract class OccluderSystem : EntitySystem
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        private const float TreeGrowthRate = 256;

        private Queue<OccluderEvent> _updates = new(64);

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<MapChangedEvent>(ev =>
            {
                if (ev.Created)
                    OnMapCreated(ev);
            });

            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInit);
            SubscribeLocalEvent<OccluderTreeComponent, ComponentInit>(HandleOccluderTreeInit);
            SubscribeLocalEvent<OccluderComponent, ComponentInit>(HandleOccluderInit);
            SubscribeLocalEvent<OccluderComponent, ComponentShutdown>(HandleOccluderShutdown);

            SubscribeLocalEvent<OccluderComponent, MoveEvent>(EntMoved);
            SubscribeLocalEvent<OccluderComponent, EntParentChangedMessage>(EntParentChanged);
            SubscribeLocalEvent<OccluderEvent>(ev => _updates.Enqueue(ev));
        }

        internal IEnumerable<OccluderTreeComponent> GetOccluderTrees(MapId mapId, Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) yield break;

            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                yield return EntityManager.GetComponent<OccluderTreeComponent>(grid.GridEntityId);
            }

            yield return EntityManager.GetComponent<OccluderTreeComponent>(_mapManager.GetMapEntityId(mapId));
        }

        private void HandleOccluderInit(EntityUid uid, OccluderComponent component, ComponentInit args)
        {
            if (!component.Enabled) return;
            _updates.Enqueue(new OccluderAddEvent(component));
        }

        private void HandleOccluderShutdown(EntityUid uid, OccluderComponent component, ComponentShutdown args)
        {
            if (!component.Enabled) return;
            _updates.Enqueue(new OccluderRemoveEvent(component));
        }

        private void HandleOccluderTreeInit(EntityUid uid, OccluderTreeComponent component, ComponentInit args)
        {
            var capacity = (int) Math.Min(256, Math.Ceiling(EntityManager.GetComponent<TransformComponent>(component.Owner).ChildCount / TreeGrowthRate) * TreeGrowthRate);

            component.Tree = new(ExtractAabbFunc, capacity: capacity);
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<OccluderTreeComponent>(ev.EntityUid);
        }

        private OccluderTreeComponent? GetOccluderTree(OccluderComponent component)
        {
            var entity = component.Owner;
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(entity, out var xform) || xform.MapID == MapId.Nullspace)
                return null;

            var query = GetEntityQuery<OccluderTreeComponent>();
            while (xform.ParentUid.IsValid())
            {
                if (query.TryGetComponent(xform.ParentUid, out var comp))
                    return comp;

                xform = xformQuery.GetComponent(xform.ParentUid);
            }

            return null;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _updates.Clear();
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
            var query = GetEntityQuery<TransformComponent>();
            while (_updates.TryDequeue(out var occluderUpdate))
            {
                OccluderTreeComponent? tree;
                var component = occluderUpdate.Component;

                switch (occluderUpdate)
                {
                    case OccluderAddEvent:
                        if (component.Tree != null || component.Deleted) break;
                        tree = GetOccluderTree(component);
                        if (tree == null) break;
                        component.Tree = tree;
                        tree.Tree.Add(new()
                        {
                            Component = component,
                            Transform = query.GetComponent(component.Owner)
                        });
                        break;
                    case OccluderUpdateEvent:
                        if (component.Deleted) break;
                        var oldTree = component.Tree;
                        tree = GetOccluderTree(component);
                        var entry = new ComponentTreeEntry<OccluderComponent>()
                        {
                            Component = component,
                            Transform = query.GetComponent(component.Owner)
                        };
                        if (oldTree != tree)
                        {
                            oldTree?.Tree.Remove(entry);
                            tree?.Tree.Add(entry);
                            component.Tree = tree;
                            break;
                        }

                        tree?.Tree.Update(entry);

                        break;
                    case OccluderRemoveEvent:
                        tree = component.Tree;
                        tree?.Tree.Remove(new() { Component = component });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"No implemented occluder update for {occluderUpdate.GetType()}");
                }
            }
        }

        private void EntMoved(EntityUid uid, OccluderComponent component, ref MoveEvent args)
        {
            _updates.Enqueue(new OccluderUpdateEvent(component));
        }

        private void EntParentChanged(EntityUid uid, OccluderComponent component, ref EntParentChangedMessage args)
        {
            _updates.Enqueue(new OccluderUpdateEvent(component));
        }

        private void OnMapCreated(MapChangedEvent e)
        {
            if (e.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntityId(e.Map).EnsureComponent<OccluderTreeComponent>();
        }

        private Box2 ExtractAabbFunc(in ComponentTreeEntry<OccluderComponent> entry)
        {
            return entry.Component.BoundingBox.Translated(entry.Transform.LocalPosition);
        }

        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, in Ray ray, float maxLength,
            Func<EntityUid, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            // ReSharper disable once ConvertToLocalFunction
            var wrapper = (EntityUid uid, Func<EntityUid, bool>? wrapped)
                => wrapped != null && wrapped(uid);

            return IntersectRayWithPredicate(mapId, in ray, maxLength, predicate, wrapper, returnOnFirstHit);
        }

        public IEnumerable<RayCastResults> IntersectRayWithPredicate<TState>(MapId mapId, in Ray ray, float maxLength,
            TState state, Func<EntityUid, TState, bool> predicate, bool returnOnFirstHit = true)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<RayCastResults>();
            var list = new List<RayCastResults>();

            var endPoint = ray.Position + ray.Direction * maxLength;
            var worldBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint), Vector2.ComponentMax(ray.Position, endPoint));
            var xforms = EntityManager.GetEntityQuery<TransformComponent>();

            foreach (var comp in GetOccluderTrees(mapId, worldBox))
            {
                var transform = xforms.GetComponent(comp.Owner);
                var (_, treeRot, matrix) = transform.GetWorldPositionRotationInvMatrix(xforms);

                var relativeAngle = new Angle(-treeRot.Theta).RotateVec(ray.Direction);

                var treeRay = new Ray(matrix.Transform(ray.Position), relativeAngle);

                comp.Tree.QueryRay(ref list,
                    (ref List<RayCastResults> listState, in ComponentTreeEntry<OccluderComponent> value, in Vector2 point, float distFromOrigin) =>
                    {
                        if (distFromOrigin > maxLength)
                            return true;

                        if (!value.Component.Enabled)
                            return true;

                        if (predicate.Invoke(value.Uid, state))
                            return true;

                        var result = new RayCastResults(distFromOrigin, point, value.Uid);
                        listState.Add(result);
                        return !returnOnFirstHit;
                    }, treeRay);
            }

            return list;
        }
    }

    internal sealed class OccluderAddEvent : OccluderEvent
    {
        public OccluderAddEvent(OccluderComponent component) : base(component) {}
    }

    internal sealed class OccluderUpdateEvent : OccluderEvent
    {
        public OccluderUpdateEvent(OccluderComponent component) : base(component) {}
    }

    internal sealed class OccluderRemoveEvent : OccluderEvent
    {
        public OccluderRemoveEvent(OccluderComponent component) : base(component) {}
    }

    internal abstract class OccluderEvent : EntityEventArgs
    {
        public OccluderComponent Component { get; }

        public OccluderEvent(OccluderComponent component)
        {
            Component = component;
        }
    }
}
