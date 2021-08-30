using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects
{
    public sealed class ExtendedPVSRangeSystem : EntitySystem
    {
        [Dependency] private IEntityLookup _lookup = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ExtendedPVSRangeComponent, UpdatePVSRangeEvent>(HandleBoundsUpdate);
        }

        private void HandleBoundsUpdate(EntityUid uid, ExtendedPVSRangeComponent component, UpdatePVSRangeEvent args)
        {
            var existing = component.Bounds[args.Component];
            if (existing.Equals(args.Bounds)) return;

            component.Bounds[args.Component] = args.Bounds;
            _lookup.UpdateEntityTree(EntityManager.GetEntity(uid));
        }

        public void AddBounds(EntityUid uid, Component component, Box2 aabb)
        {
            var extended = ComponentManager.EnsureComponent<ExtendedPVSRangeComponent>(EntityManager.GetEntity(uid));
            extended.Bounds.Add(component, aabb);
        }

        public void RemoveBounds(EntityUid uid, Component component)
        {
            if (!ComponentManager.TryGetComponent<ExtendedPVSRangeComponent>(uid, out var extended)) return;
            extended.Bounds.Remove(component);
            if (extended.Bounds.Count == 0)
            {
                ComponentManager.RemoveComponent<ExtendedPVSRangeComponent>(uid);
            }
        }
    }

    public sealed class UpdatePVSRangeEvent : EntityEventArgs
    {
        public Component Component { get; init; } = default!;
        public Box2 Bounds { get; init; }
    }
}
