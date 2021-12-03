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

            _mapManager.MapCreated += OnMapCreated;

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
                IEntity tempQualifier = EntityManager.GetEntity(grid.GridEntityId);
                yield return IoCManager.Resolve<IEntityManager>().GetComponent<OccluderTreeComponent>(tempQualifier.Uid);
            }

            IEntity tempQualifier1 = _mapManager.GetMapEntity(mapId);
            yield return IoCManager.Resolve<IEntityManager>().GetComponent<OccluderTreeComponent>(tempQualifier1.Uid);
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
            var capacity = (int) Math.Min(256, Math.Ceiling(component.Owner.Transform.ChildCount / TreeGrowthRate) * TreeGrowthRate);

            component.Tree = new DynamicTree<OccluderComponent>(ExtractAabbFunc, capacity: capacity);
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.GetEntity(ev.EntityUid).EnsureComponent<OccluderTreeComponent>();
        }

        private OccluderTreeComponent? GetOccluderTree(OccluderComponent component)
        {
            var entity = component.Owner;

            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted || entity.Transform.MapID == MapId.Nullspace) return null;

            var parent = entity.Transform.Parent?.Owner;

            while (true)
            {
                if (parent == null) break;

                if (IoCManager.Resolve<IEntityManager>().TryGetComponent(parent.Uid, out OccluderTreeComponent? comp)) return comp;
                parent = parent.Transform.Parent?.Owner;
            }

            return null;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.MapCreated -= OnMapCreated;
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
            while (_updates.TryDequeue(out var occluderUpdate))
            {
                OccluderTreeComponent? tree;
                var component = occluderUpdate.Component;

                switch (occluderUpdate)
                {
                    case OccluderAddEvent:
                        if (component.Tree != null) break;
                        tree = GetOccluderTree(component);
                        if (tree == null) break;
                        component.Tree = tree;
                        tree.Tree.Add(component);
                        break;
                    case OccluderUpdateEvent:
                        var oldTree = component.Tree;
                        tree = GetOccluderTree(component);
                        if (oldTree != tree)
                        {
                            oldTree?.Tree.Remove(component);
                            tree?.Tree.Add(component);
                            component.Tree = tree;
                            break;
                        }

                        tree?.Tree.Update(component);

                        break;
                    case OccluderRemoveEvent:
                        tree = component.Tree;
                        tree?.Tree.Remove(component);
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

        private void OnMapCreated(object? sender, MapEventArgs e)
        {
            if (e.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntity(e.Map).EnsureComponent<OccluderTreeComponent>();
        }

        private static Box2 ExtractAabbFunc(in OccluderComponent o)
        {
            return o.BoundingBox.Translated(o.Owner.Transform.LocalPosition);
        }

        public IEnumerable<RayCastResults> IntersectRayWithPredicate(MapId mapId, in Ray ray, float maxLength,
            Func<IEntity, bool>? predicate = null, bool returnOnFirstHit = true)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<RayCastResults>();
            var list = new List<RayCastResults>();

            var endPoint = ray.Position + ray.Direction * maxLength;
            var worldBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint), Vector2.ComponentMax(ray.Position, endPoint));

            foreach (var comp in GetOccluderTrees(mapId, worldBox))
            {
                var transform = comp.Owner.Transform;
                var matrix = transform.InvWorldMatrix;
                var treeRot = transform.WorldRotation;

                var relativeAngle = new Angle(-treeRot.Theta).RotateVec(ray.Direction);

                var treeRay = new Ray(matrix.Transform(ray.Position), relativeAngle);

                comp.Tree.QueryRay(ref list,
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
