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
            SubscribeLocalEvent<CollisionWakeComponent, PhysicsInitializedEvent>(OnInit);
            SubscribeLocalEvent<CollisionWakeComponent, ComponentRemove>(OnRemove);

            SubscribeLocalEvent<CollisionWakeComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<CollisionWakeComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<CollisionWakeComponent, PhysicsWakeEvent>(OnWake);
            SubscribeLocalEvent<CollisionWakeComponent, PhysicsSleepEvent>(OnSleep);

            SubscribeLocalEvent<CollisionWakeComponent, JointAddedEvent>(OnJointAdd);
            SubscribeLocalEvent<CollisionWakeComponent, JointRemovedEvent>(OnJointRemove);

            SubscribeLocalEvent<CollisionWakeComponent, EntParentChangedMessage>(OnParentChange);
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

        private void OnRemove(EntityUid uid, CollisionWakeComponent component, ComponentRemove args)
        {
            if (component.Enabled
                && !Terminating(uid)
                && TryComp(uid, out PhysicsComponent? physics))
            {
                physics.CanCollide = true;
            }
        }

        private void OnParentChange(EntityUid uid, CollisionWakeComponent component, ref EntParentChangedMessage args)
        {
            UpdateCanCollide(uid, component, xform: args.Transform);
        }

        private void OnInit(EntityUid uid, CollisionWakeComponent component, ref PhysicsInitializedEvent args)
        {
            UpdateCanCollide(uid, component, checkTerminating: false);
        }

        private void OnJointRemove(EntityUid uid, CollisionWakeComponent component, JointRemovedEvent args)
        {
            UpdateCanCollide(uid, component, args.OurBody);
        }

        private void OnJointAdd(EntityUid uid, CollisionWakeComponent component, JointAddedEvent args)
        {
            // Bypass UpdateCanCollide() as joint count will always be bigger than 0:
            if (component.Enabled)
                args.OurBody.CanCollide = true;
        }

        private void OnWake(EntityUid uid, CollisionWakeComponent component, ref PhysicsWakeEvent args)
        {
            UpdateCanCollide(uid, component, args.Body, checkTerminating: false);
        }

        private void OnSleep(EntityUid uid, CollisionWakeComponent component, ref PhysicsSleepEvent args)
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
