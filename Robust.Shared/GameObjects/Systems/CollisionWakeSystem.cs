using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public class CollisionWakeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PhysicsWakeMessage>(HandleWake);
            SubscribeLocalEvent<PhysicsSleepMessage>(HandleSleep);
            SubscribeLocalEvent<CollisionWakeComponent, CollisionWakeStateMessage>(HandleCollisionWakeState);
        }

        private void HandleWake(PhysicsWakeMessage message)
        {
            if (!message.Body.Owner.TryGetComponent<CollisionWakeComponent>(out var comp) || !comp.Enabled) return;
            message.Body.CanCollide = true;
        }

        private void HandleSleep(PhysicsSleepMessage message)
        {
            if (!message.Body.Owner.TryGetComponent<CollisionWakeComponent>(out var comp) || !comp.Enabled) return;
            message.Body.CanCollide = false;
        }

        private void HandleCollisionWakeState(EntityUid uid, CollisionWakeComponent component, CollisionWakeStateMessage args)
        {
            if(ComponentManager.TryGetComponent<IPhysBody>(uid, out var body))
                body.CanCollide = !component.Enabled || body.Awake;
        }
    }

    public sealed class CollisionWakeStateMessage : EntityEventArgs { }
}
