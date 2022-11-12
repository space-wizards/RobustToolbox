using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems
{
    // These exist as a means to defer joint additions / removals so we can use HandleComponentState gracefully without
    // exploding for modifying components.
    // Actual subscriptions should use the other joint events.
    internal sealed class AddJointEvent : JointEvent
    {
        public PhysicsComponent BodyA { get; }
        public PhysicsComponent BodyB { get; }

        public AddJointEvent(PhysicsComponent bodyA, PhysicsComponent bodyB, Joint joint) : base(joint)
        {
            BodyA = bodyA;
            BodyB = bodyB;
        }
    }

    internal sealed class RemoveJointEvent : JointEvent
    {
        public RemoveJointEvent(Joint joint) : base(joint) {}
    }

    internal abstract class JointEvent
    {
        public Joint Joint { get; }

        public JointEvent(Joint joint)
        {
            Joint = joint;
        }
    }

    public abstract class SharedJointSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        // To avoid issues with component states we'll queue up all dirty joints and check it every tick to see if
        // we can delete the component.
        private readonly HashSet<JointComponent> _dirtyJoints = new();
        protected readonly HashSet<Joint> AddedJoints = new();

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("physics");
            UpdatesOutsidePrediction = true;

            UpdatesBefore.Add(typeof(SharedPhysicsSystem));
            SubscribeLocalEvent<JointComponent, ComponentShutdown>(OnJointShutdown);
            SubscribeLocalEvent<JointComponent, ComponentInit>(OnJointInit);
        }

        #region Lifetime

        private void OnJointInit(EntityUid uid, JointComponent component, ComponentInit args)
        {
            foreach (var (id, joint) in component.Joints)
            {
                var other = uid == joint.BodyAUid ? joint.BodyBUid : joint.BodyAUid;

                // Client may not yet know about the other entity.
                // But whenever that other entity enters PVS, its own joint initialization should hopefully run this again anyways.
                if (!TryComp(joint.BodyAUid, out PhysicsComponent? bodyA) || !TryComp(joint.BodyBUid, out PhysicsComponent? bodyB) || !TryComp(other, out JointComponent? otherComp))
                    continue;

                if (!otherComp.Joints.ContainsKey(id))
                {
                    // This can happen if the other joint handled its state before this entity was initialized. In this
                    // case we need to re-add the joint to the other entity.
                    if (uid == joint.BodyAUid)
                        InitJoint(joint, bodyA, bodyB, component, otherComp, ignoreExisting: true);
                    else
                        InitJoint(joint, bodyA, bodyB, otherComp, component, ignoreExisting: true);
                    continue;
                }

                _physics.WakeBody(bodyA);
                _physics.WakeBody(bodyB);

                // Raise broadcast last so we can do both sides of directed first.
                var vera = new JointAddedEvent(joint, bodyA, bodyB);
                RaiseLocalEvent(bodyA.Owner, vera);
                var smug = new JointAddedEvent(joint, bodyB, bodyA);
                RaiseLocalEvent(bodyB.Owner, smug);
                EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);
            }
        }

        private void OnJointShutdown(EntityUid uid, JointComponent component, ComponentShutdown args)
        {
            foreach (var joint in component.Joints.Values)
            {
                RemoveJoint(joint);
            }
        }

        #endregion

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var joint in AddedJoints)
            {
                InitJoint(joint);
            }

            AddedJoints.Clear();

            foreach (var joint in _dirtyJoints)
            {
                if (joint.Deleted || joint.JointCount != 0) continue;
                EntityManager.RemoveComponent<JointComponent>(joint.Owner);
            }

            _dirtyJoints.Clear();
        }

        private void InitJoint(Joint joint,
            PhysicsComponent? bodyA = null,
            PhysicsComponent? bodyB = null,
            JointComponent? jointComponentA = null,
            JointComponent? jointComponentB = null,
            bool ignoreExisting = false)
        {
            var aUid = joint.BodyAUid;
            var bUid = joint.BodyBUid;

            if (!Resolve(aUid, ref bodyA, false) || !Resolve(bUid, ref bodyB, false))
                return;

            DebugTools.Assert(Transform(aUid).MapID == Transform(bUid).MapID, "Attempted to initialize cross-map joint");

            jointComponentA ??= EnsureComp<JointComponent>(bodyA.Owner);
            jointComponentB ??= EnsureComp<JointComponent>(bodyB.Owner);
            DebugTools.Assert(jointComponentA.Owner == aUid && jointComponentB.Owner == bUid);

            var jointsA = jointComponentA.Joints;
            var jointsB = jointComponentB.Joints;


            _sawmill.Debug($"Initializing joint {joint.ID}");

            // Check for existing joints
            if (!ignoreExisting && jointsA.TryGetValue(joint.ID, out var existing))
            {
                if (existing.BodyBUid != bUid)
                {
                    _sawmill.Error($"While adding joint {joint.ID} to {ToPrettyString(bUid)}, the connected entity {ToPrettyString(aUid)} already had a joint with the same ID connected to another entity {ToPrettyString(existing.BodyBUid)}.");
                    return;
                }

                // If they both already have it we should be gucci
                // This can occur because of client states coming in blah blah
                // The reason for this is we defer everything until Update
                // (and the reason we defer is to avoid modifying components during iteration when we do the EnsureComponent)
                if (jointsB.ContainsKey(joint.ID))
                {
                    DebugTools.Assert(jointsB[joint.ID].BodyAUid == aUid);
                    return;
                }

                _sawmill.Error($"While adding joint {joint.ID} to {ToPrettyString(bUid)}, the joint already existed for the connected entity {ToPrettyString(aUid)}.");
            }
            else if (!ignoreExisting && jointsB.TryGetValue(joint.ID, out existing))
            {
                if (existing.BodyAUid != aUid)
                {
                    _sawmill.Error($"While adding joint {joint.ID} to {ToPrettyString(aUid)}, the connected entity {ToPrettyString(bUid)} already had a joint with the same ID connected to another entity {ToPrettyString(existing.BodyAUid)}.");
                    return;
                }

                _sawmill.Error($"While adding joint {joint.ID} to {ToPrettyString(aUid)}, the joint already existed for the connected entity {ToPrettyString(bUid)}.");
            }

            jointsA.TryAdd(joint.ID, joint);
            jointsB.TryAdd(joint.ID, joint);

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (!joint.CollideConnected)
            {
                FilterContactsForJoint(joint, bodyA, bodyB);
            }

            _physics.WakeBody(bodyA);
            _physics.WakeBody(bodyB);
            Dirty(bodyA);
            Dirty(bodyB);
            Dirty(jointComponentA);
            Dirty(jointComponentB);

            // Also flag these for checking juusssttt in case.
            _dirtyJoints.Add(jointComponentA);
            _dirtyJoints.Add(jointComponentB);
            // Note: creating a joint doesn't wake the bodies.

            // Raise broadcast last so we can do both sides of directed first.
            var vera = new JointAddedEvent(joint, bodyA, bodyB);
            EntityManager.EventBus.RaiseLocalEvent(bodyA.Owner, vera, false);
            var smug = new JointAddedEvent(joint, bodyB, bodyA);
            EntityManager.EventBus.RaiseLocalEvent(bodyB.Owner, smug, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);
        }

        private static string GetJointId(Joint joint)
        {
            var id = joint.ID;
            return !string.IsNullOrEmpty(id) ? id : joint.GetHashCode().ToString();
        }

        #region Helpers

        /// <summary>
        /// Create a DistanceJoint between 2 bodies. This should be called content-side whenever you need one.
        /// </summary>
        public DistanceJoint CreateDistanceJoint(EntityUid bodyA, EntityUid bodyB, Vector2? anchorA = null, Vector2? anchorB = null, string? id = null)
        {
            anchorA ??= Vector2.Zero;
            anchorB ??= Vector2.Zero;

            var joint = new DistanceJoint(Comp<PhysicsComponent>(bodyA), Comp<PhysicsComponent>(bodyB), anchorA.Value, anchorB.Value);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        /// <summary>
        /// Create a MouseJoint between 2 bodies. This should be called content-side whenever you need one.
        /// </summary>
        public MouseJoint CreateMouseJoint(EntityUid bodyA, EntityUid bodyB, Vector2? anchorA = null, Vector2? anchorB = null, string? id = null)
        {
            anchorA ??= Vector2.Zero;
            anchorB ??= Vector2.Zero;

            var joint = new MouseJoint(bodyA, bodyB, anchorA.Value, anchorB.Value);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        public PrismaticJoint CreatePrismaticJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
        {
            var joint = new PrismaticJoint(bodyA, bodyB);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        public PrismaticJoint CreatePrismaticJoint(EntityUid bodyA, EntityUid bodyB, Vector2 worldAnchor, Vector2 worldAxis, string? id = null)
        {
            var joint = new PrismaticJoint(bodyA, bodyB, worldAnchor, worldAxis, EntityManager);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        public RevoluteJoint CreateRevoluteJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
        {
            var joint = new RevoluteJoint(bodyA, bodyB);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        public WeldJoint GetOrCreateWeldJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
        {
            if (id != null &&
                EntityManager.TryGetComponent(bodyA, out JointComponent? jointComponent) &&
                jointComponent.Joints.TryGetValue(id, out var weldJoint))
            {
                return (WeldJoint) weldJoint;
            }

            var joint = new WeldJoint(bodyA, bodyB);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        public WeldJoint CreateWeldJoint(EntityUid bodyA, EntityUid bodyB, string? id = null)
        {
            var joint = new WeldJoint(bodyA, bodyB);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJoint(joint);

            return joint;
        }

        #endregion

        public static void LinearStiffness(
            float frequencyHertz,
            float dampingRatio,
            float massA,
            float massB,
            out float stiffness, out float damping)
        {
            float mass;
            if (massA > 0.0f && massB > 0.0f)
            {
                mass = massA * massB / (massA + massB);
            }
            else if (massA > 0.0f)
            {
                mass = massA;
            }
            else
            {
                mass = massB;
            }

            var omega = 2.0f * MathF.PI * frequencyHertz;
            stiffness = mass * omega * omega;
            damping = 2.0f * mass * dampingRatio * omega;
        }

        public static void AngularStiffness(
            float frequencyHertz,
            float dampingRatio,
            PhysicsComponent bodyA,
            PhysicsComponent bodyB,
            out float stiffness, out float damping)
        {
            var IA = bodyA.Inertia;
            var IB = bodyB.Inertia;

            float I;
            if (IA > 0.0f && IB > 0.0f)
            {
                I = IA * IB / (IA + IB);
            }
            else if (IA > 0.0f)
            {
                I = IA;
            }
            else
            {
                I = IB;
            }

            float omega = 2.0f * MathF.PI * frequencyHertz;
            stiffness = I * omega * omega;
            damping = 2.0f * I * dampingRatio * omega;
        }

        #region Joints

        protected void AddJoint(Joint joint, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
        {
            if (!Resolve(joint.BodyAUid, ref bodyA) || !Resolve(joint.BodyBUid, ref bodyB))
                return;

            if (!joint.CollideConnected)
                FilterContactsForJoint(joint, bodyA, bodyB);

            // Maybe make this method AddOrUpdate so we can have an Add one that explicitly throws if present?
            var mapidA = EntityManager.GetComponent<TransformComponent>(bodyA.Owner).MapID;

            if (mapidA == MapId.Nullspace ||
                mapidA != EntityManager.GetComponent<TransformComponent>(bodyB.Owner).MapID)
            {
                _sawmill.Error($"Tried to add joint to ineligible bodies");
                return;
            }

            if (string.IsNullOrEmpty(joint.ID))
            {
                _sawmill.Error($"Can't add a joint with no ID");
                DebugTools.Assert($"Can't add a joint with no ID");
                return;
            }

            // Need to defer this for prediction reasons, yay!
            AddedJoints.Add(joint);
        }

        public void ClearJoints(EntityUid uid, JointComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            // TODO PERFORMANCE
            // This will re-fetch the joint & body component for this entity ( & ever connected
            // entity), for each and every joint. at the very least, we could pass in the joint & physics comp. As long
            // as most entities only have a single joint, fetching connected components probably isn't worth it.
            foreach (var a in component.Joints.Values.ToArray())
            {
                RemoveJoint(a);
            }
        }

        [Obsolete("Use the other ClearJoints overload")]
        public void ClearJoints(JointComponent joint)
        {
            ClearJoints(joint.Owner, joint);
        }

        public void RemoveJoint(Joint joint)
        {
            var bodyAUid = joint.BodyAUid;
            var bodyBUid = joint.BodyBUid;

            // Originally I logged these but because of prediction the client can just nuke them multiple times in a row
            // because each body has its own JointComponent, bleh.
            if (!EntityManager.TryGetComponent<JointComponent>(bodyAUid, out var jointComponentA))
            {
                return;
            }

            if (!EntityManager.TryGetComponent<JointComponent>(bodyBUid, out var jointComponentB))
            {
                return;
            }

            if (!jointComponentA.Joints.Remove(joint.ID))
            {
                return;
            }

            if (!jointComponentB.Joints.Remove(joint.ID))
            {
                return;
            }

            _sawmill.Debug($"Removed joint {joint.ID}");

            // Wake up connected bodies.
            if (EntityManager.TryGetComponent<PhysicsComponent>(bodyAUid, out var bodyA) &&
                MetaData(bodyAUid).EntityLifeStage < EntityLifeStage.Terminating &&
                !_container.IsEntityInContainer(bodyAUid))
            {
                _physics.SetCanCollide(bodyA, true);
                _physics.SetAwake(bodyA, true);
            }

            if (EntityManager.TryGetComponent<PhysicsComponent>(bodyBUid, out var bodyB) &&
                MetaData(bodyBUid).EntityLifeStage < EntityLifeStage.Terminating &&
                !_container.IsEntityInContainer(bodyBUid))
            {
                _physics.SetCanCollide(bodyB, true);
                _physics.SetAwake(bodyB, true);
            }

            if (!jointComponentA.Deleted)
            {
                Dirty(jointComponentA);
            }

            if (!jointComponentB.Deleted)
            {
                Dirty(jointComponentB);
            }

            if (jointComponentA.Deleted && jointComponentB.Deleted)
                return;

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (!joint.CollideConnected)
            {
                FilterContactsForJoint(joint);
            }

            if (bodyA == null)
            {
                _sawmill.Debug($"Removing joint from entioty {ToPrettyString(bodyAUid)} without a physics component?");
            }
            else if (bodyB == null)
            {
                _sawmill.Debug($"Removing joint from entioty {ToPrettyString(bodyBUid)} without a physics component?");
            }
            else
            {
                var vera = new JointRemovedEvent(joint, bodyA, bodyB);
                EntityManager.EventBus.RaiseLocalEvent(bodyA.Owner, vera, false);
                var smug = new JointRemovedEvent(joint, bodyB, bodyA);
                EntityManager.EventBus.RaiseLocalEvent(bodyB.Owner, smug, false);
                EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);
            }

            // We can't just check up front due to how prediction works.
            _dirtyJoints.Add(jointComponentA);
            _dirtyJoints.Add(jointComponentB);
        }

        #endregion

        internal void FilterContactsForJoint(Joint joint, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)
        {
            if (!Resolve(joint.BodyAUid, ref bodyA) || !Resolve(joint.BodyBUid, ref bodyB))
                return;

            var node = bodyB.Contacts.First;

            while (node != null)
            {
                var contact = node.Value;
                node = node.Next;

                if (contact.FixtureA?.Body == bodyA ||
                    contact.FixtureB?.Body == bodyA)
                {
                    // Flag the contact for filtering at the next time step (where either
                    // body is awake).
                    contact.Flags |= ContactFlags.Filter;
                }
            }
        }
    }
}
