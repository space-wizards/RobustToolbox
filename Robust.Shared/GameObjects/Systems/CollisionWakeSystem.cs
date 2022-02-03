using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public sealed class CollisionWakeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollisionWakeComponent, EntityInitializedMessage>(HandleInitialize);
            SubscribeLocalEvent<CollisionWakeComponent, ComponentRemove>(HandleRemove);

            SubscribeLocalEvent<CollisionWakeComponent, PhysicsWakeMessage>(HandleWake);
            SubscribeLocalEvent<CollisionWakeComponent, PhysicsSleepMessage>(HandleSleep);
            SubscribeLocalEvent<CollisionWakeComponent, CollisionWakeStateMessage>(HandleCollisionWakeState);

            SubscribeLocalEvent<CollisionWakeComponent, JointAddedEvent>(HandleJointAdd);
            SubscribeLocalEvent<CollisionWakeComponent, JointRemovedEvent>(HandleJointRemove);

            SubscribeLocalEvent<CollisionWakeComponent, EntParentChangedMessage>(HandleParentChange);
        }

        private void HandleRemove(EntityUid uid, CollisionWakeComponent component, ComponentRemove args)
        {
            if (!Terminating(uid) && TryComp(uid, out PhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }

        private bool CanRaiseEvent(EntityUid uid)
        {
            var meta = MetaData(uid);

            if (meta.EntityLifeStage >= EntityLifeStage.Terminating) return false;
            return true;
        }

        private void HandleParentChange(EntityUid uid, CollisionWakeComponent component, ref EntParentChangedMessage args)
        {
            if (!CanRaiseEvent(uid)) return;

            component.RaiseStateChange();
        }

        private void HandleInitialize(EntityUid uid, CollisionWakeComponent component, EntityInitializedMessage args)
        {
            component.RaiseStateChange();
        }

        private void HandleJointRemove(EntityUid uid, CollisionWakeComponent component, JointRemovedEvent args)
        {
            if (!CanRaiseEvent(uid)) return;

            component.RaiseStateChange();
        }

        private void HandleJointAdd(EntityUid uid, CollisionWakeComponent component, JointAddedEvent args)
        {
            component.RaiseStateChange();
        }

        private void HandleWake(EntityUid uid, CollisionWakeComponent component, PhysicsWakeMessage args)
        {
            component.RaiseStateChange();
        }

        private void HandleSleep(EntityUid uid, CollisionWakeComponent component, PhysicsSleepMessage args)
        {
            if (!CanRaiseEvent(uid)) return;

            component.RaiseStateChange();
        }

        private void HandleCollisionWakeState(EntityUid uid, CollisionWakeComponent component, CollisionWakeStateMessage args)
        {
            // If you really wanted you could optimise for each use case above and save some calls but
            // these are called pretty infrequently so I'm fine with this for now.

            // If we just got put into a container don't want to mess with our collision state.
            if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var body)) return;

            // If we're attached to the map we'll also just never disable collision due to how grid movement works.
            body.CanCollide = !component.Enabled ||
                              body.Awake ||
                              (EntityManager.TryGetComponent(uid, out JointComponent? jointComponent) && jointComponent.JointCount > 0) ||
                              EntityManager.GetComponent<TransformComponent>(uid).GridID == GridId.Invalid;
        }
    }

    public sealed class CollisionWakeStateMessage : EntityEventArgs { }
}
