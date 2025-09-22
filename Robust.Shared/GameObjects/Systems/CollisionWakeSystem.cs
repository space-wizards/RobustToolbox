using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Robust.Shared.GameObjects
{
    public sealed class CollisionWakeSystem : EntitySystem
    {
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        private EntityQuery<CollisionWakeComponent> _query;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollisionWakeComponent, ComponentShutdown>(OnRemove);

            SubscribeLocalEvent<CollisionWakeComponent, JointAddedEvent>(OnJointAdd);
            SubscribeLocalEvent<CollisionWakeComponent, JointRemovedEvent>(OnJointRemove);

            SubscribeLocalEvent<CollisionWakeComponent, EntParentChangedMessage>(OnParentChange);

            _query = GetEntityQuery<CollisionWakeComponent>();
        }

        public void SetEnabled(EntityUid uid, bool enabled, CollisionWakeComponent? component = null)
        {
            if (!_query.Resolve(uid, ref component, false) || component.Enabled == enabled)
                return;

            component.Enabled = enabled;

            if (component.Enabled)
                UpdateCanCollide(uid, component);
            else if (TryComp(uid, out PhysicsComponent? physics))
                _physics.SetCanCollide(uid, true, body: physics);

            Dirty(uid, component);
        }

        private void OnRemove(EntityUid uid, CollisionWakeComponent component, ComponentShutdown args)
        {
            if (component.Enabled
                && !Terminating(uid)
                && TryComp(uid, out PhysicsComponent? physics))
            {
                _physics.SetCanCollide(uid, true, body: physics);
            }
        }

        private void OnParentChange(EntityUid uid, CollisionWakeComponent component, ref EntParentChangedMessage args)
        {
            if (component.LifeStage < ComponentLifeStage.Initialized)
                return;

            UpdateCanCollide(uid, component, xform: args.Transform);
        }

        private void OnJointRemove(EntityUid uid, CollisionWakeComponent component, JointRemovedEvent args)
        {
            UpdateCanCollide(uid, component, args.OurBody);
        }

        private void OnJointAdd(EntityUid uid, CollisionWakeComponent component, JointAddedEvent args)
        {
            // Bypass UpdateCanCollide() as joint count will always be bigger than 0:
            if (component.Enabled)
                _physics.SetCanCollide(uid, true);
        }

        internal void UpdateCanCollide(Entity<PhysicsComponent> entity, bool checkTerminating = true, bool dirty = true)
        {
            if (_query.TryGetComponent(entity, out var wakeComp))
                UpdateCanCollide(entity.Owner, wakeComp, entity.Comp, checkTerminating: checkTerminating, dirty: dirty);
        }

        internal void UpdateCanCollide(
            EntityUid uid,
            CollisionWakeComponent component,
            PhysicsComponent? body = null,
            TransformComponent? xform = null,
            bool checkTerminating = true,
            bool dirty = true)
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
            var canCollide = body.Awake ||
                             body.ContactCount > 0 ||
                              (TryComp(uid, out JointComponent? jointComponent) && jointComponent.JointCount > 0) ||
                              xform.GridUid == null;

            _physics.SetCanCollide(uid, canCollide, dirty, body: body);
        }
    }
}
