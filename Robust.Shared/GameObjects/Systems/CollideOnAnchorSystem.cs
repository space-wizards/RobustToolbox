namespace Robust.Shared.GameObjects
{
    public sealed class CollideOnAnchorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollideOnAnchorComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<CollideOnAnchorComponent, AnchorStateChangedEvent>(OnAnchor);
        }

        private void OnStartup(EntityUid uid, CollideOnAnchorComponent component, ComponentStartup args)
        {
            if (!EntityManager.TryGetComponent(uid, out TransformComponent? transformComponent)) return;

            SetCollide(uid, component, transformComponent.Anchored);
        }

        private void OnAnchor(EntityUid uid, CollideOnAnchorComponent component, ref AnchorStateChangedEvent args)
        {
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

            body.CanCollide = enabled;
        }
    }
}
