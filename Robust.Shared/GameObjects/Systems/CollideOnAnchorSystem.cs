using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Robust.Shared.GameObjects
{
    public sealed class CollideOnAnchorSystem : EntitySystem
    {
        [Dependency] private SharedPhysicsSystem _physics = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollideOnAnchorComponent, ComponentStartup>(OnStartup);
            // Shouldn't need to handle re-anchor.
            SubscribeLocalEvent<CollideOnAnchorComponent, AnchorStateChangedEvent>(OnAnchor);
        }

        private void OnStartup(EntityUid uid, CollideOnAnchorComponent component, ComponentStartup args)
        {
            if (!EntityManager.TryGetComponent(uid, out TransformComponent? transformComponent)) return;

            SetCollide(uid, component, transformComponent.Anchored);
        }

        private void OnAnchor(EntityUid uid, CollideOnAnchorComponent component, ref AnchorStateChangedEvent args)
        {
            if (!args.Detaching)
                SetCollide(uid, component, args.Anchored);
        }

        private void SetCollide(EntityUid uid, CollideOnAnchorComponent component, bool anchored)
        {
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body)) return;

            var enabled = component.Enable;

            if (!anchored)
            {
                enabled ^= true;
            }

            _physics.SetCanCollide(body, enabled);
        }
    }
}
