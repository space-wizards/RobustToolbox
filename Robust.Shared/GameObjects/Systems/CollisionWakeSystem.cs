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
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<PhysicsWakeMessage>();
            UnsubscribeLocalEvent<PhysicsSleepMessage>();
        }

        private void HandleWake(PhysicsWakeMessage message)
        {
            if (!message.Body.Owner.HasComponent<CollisionWakeComponent>()) return;
            message.Body.CanCollide = true;
        }

        private void HandleSleep(PhysicsSleepMessage message)
        {
            if (!message.Body.Owner.HasComponent<CollisionWakeComponent>()) return;
            message.Body.CanCollide = false;
        }
    }
}
