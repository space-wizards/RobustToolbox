using Robust.Shared.GameStates;
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

            SubscribeLocalEvent<CollisionWakeComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<CollisionWakeComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<CollisionWakeComponent, PhysicsWakeMessage>(HandleWake);
            SubscribeLocalEvent<CollisionWakeComponent, PhysicsSleepMessage>(HandleSleep);

            SubscribeLocalEvent<CollisionWakeComponent, JointAddedEvent>(HandleJointAdd);
            SubscribeLocalEvent<CollisionWakeComponent, JointRemovedEvent>(HandleJointRemove);

            SubscribeLocalEvent<CollisionWakeComponent, EntParentChangedMessage>(HandleParentChange);
        }

        public void SetEnabled(EntityUid uid, bool enabled, CollisionWakeComponent? component = null)
        {
            if (!Resolve(uid, ref component) || component.Enabled == enabled)
                return;

            component.Enabled = enabled;

            if (component.Enabled)
                UpdateCanCollide(uid, component);
            else if (TryComp(uid, out PhysicsComponent? physics))
                physics.CanCollide = true;

            Dirty(component);
        }

        private void OnHandleState(EntityUid uid, CollisionWakeComponent component, ref ComponentHandleState args)
        {
            if (args.Current is CollisionWakeComponent.CollisionWakeState state)
                component.Enabled = state.Enabled;

            // Note, this explicitly does not update PhysicsComponent.CanCollide. The physics component should perform
            // its own state-handling logic. Additionally, if we wanted to set it you would have to ensure that things
            // like the join-component and physics component have already handled their states, otherwise CanCollide may
            // be set incorrectly and leave the client with a bad state.
        }

        private void OnGetState(EntityUid uid, CollisionWakeComponent component, ref ComponentGetState args)
        {
            args.State = new CollisionWakeComponent.CollisionWakeState(component.Enabled);
        }

        private void HandleRemove(EntityUid uid, CollisionWakeComponent component, ComponentRemove args)
        {
            if (component.Enabled
                && !Terminating(uid)
                && TryComp(uid, out PhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }

        private void HandleParentChange(EntityUid uid, CollisionWakeComponent component, ref EntParentChangedMessage args)
        {
            UpdateCanCollide(uid, component, xform: args.Transform);
        }

        private void HandleInitialize(EntityUid uid, CollisionWakeComponent component, EntityInitializedMessage args)
        {
            UpdateCanCollide(uid, component, checkTerminating: false);
        }

        private void HandleJointRemove(EntityUid uid, CollisionWakeComponent component, JointRemovedEvent args)
        {
            UpdateCanCollide(uid, component, args.OurBody);
        }

        private void HandleJointAdd(EntityUid uid, CollisionWakeComponent component, JointAddedEvent args)
        {
            // Bypass UpdateCanCollide() as joint count will always be bigger than 0:
            if (component.Enabled)
                args.OurBody.CanCollide = true;
        }

        private void HandleWake(EntityUid uid, CollisionWakeComponent component, PhysicsWakeMessage args)
        {
            UpdateCanCollide(uid, component, args.Body, checkTerminating: false);
        }

        private void HandleSleep(EntityUid uid, CollisionWakeComponent component, PhysicsSleepMessage args)
        {
            UpdateCanCollide(uid, component, args.Body);
        }

        private void UpdateCanCollide(
            EntityUid uid,
            CollisionWakeComponent component,
            IPhysBody? body = null,
            TransformComponent? xform = null,
            bool checkTerminating = true)
        {
            if (!component.Enabled)
                return;

            if (checkTerminating && Terminating(uid))
                return;

            if (!Resolve(uid, ref body, false) ||
                !Resolve(uid, ref xform) ||
                xform.MapID == MapId.Nullspace)
                return;

            // If we're attached to the map we'll also just never disable collision due to how grid movement works.
            body.CanCollide = body.Awake ||
                              (TryComp(uid, out JointComponent? jointComponent) && jointComponent.JointCount > 0) ||
                              xform.GridID == GridId.Invalid;
        }
    }
}
