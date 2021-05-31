using System.Linq;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public sealed class CollisionWakeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollisionWakeComponent, PhysicsWakeMessage>(HandleWake);
            SubscribeLocalEvent<CollisionWakeComponent, PhysicsSleepMessage>(HandleSleep);
            SubscribeLocalEvent<CollisionWakeComponent, CollisionWakeStateMessage>(HandleCollisionWakeState);

            SubscribeLocalEvent<CollisionWakeComponent, JointAddedEvent>(HandleJointAdd);
            SubscribeLocalEvent<CollisionWakeComponent, JointRemovedEvent>(HandleJointRemove);
        }

        private void HandleJointRemove(EntityUid uid, CollisionWakeComponent component, JointRemovedEvent args)
        {
            if (component.Owner.TryGetComponent(out PhysicsComponent? body) && body.Joints.Any()) return;

            // Force an update
            component.RaiseStateChange();
        }

        private void HandleJointAdd(EntityUid uid, CollisionWakeComponent component, JointAddedEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out PhysicsComponent body)) return;
            body.CanCollide = true;
        }

        private void HandleWake(EntityUid uid, CollisionWakeComponent component, PhysicsWakeMessage args)
        {
            if (!component.Enabled) return;

            args.Body.CanCollide = true;
        }

        private void HandleSleep(EntityUid uid, CollisionWakeComponent component, PhysicsSleepMessage args)
        {
            if (!component.Enabled) return;

            args.Body.CanCollide = false;
        }

        private void HandleCollisionWakeState(EntityUid uid, CollisionWakeComponent component, CollisionWakeStateMessage args)
        {
            if (!ComponentManager.TryGetComponent<PhysicsComponent>(uid, out var body)) return;

            body.CanCollide = !component.Enabled || body.Awake || body.Joints.Any();
        }
    }

    public sealed class CollisionWakeStateMessage : EntityEventArgs { }
}
