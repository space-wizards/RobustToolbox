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
            SetCollide(uid, component);
        }

        private void OnAnchor(EntityUid uid, CollideOnAnchorComponent component, ref AnchorStateChangedEvent args)
        {
            SetCollide(uid, component);
        }

        private void SetCollide(EntityUid uid, CollideOnAnchorComponent component)
        {
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body) ||
                !EntityManager.TryGetComponent(uid, out TransformComponent? xform)) return;

            var enabled = component.Enable;

            if (!xform.Anchored)
            {
                enabled ^= true;
            }

            body.CanCollide = enabled;
        }
    }
}
