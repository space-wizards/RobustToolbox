using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
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
        // To avoid issues with component states we'll queue up all dirty joints and check it every tick to see if
        // we can delete the component.
        private HashSet<JointComponent> _dirtyJoints = new();

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedPhysicsSystem));
            SubscribeLocalEvent<JointComponent, ComponentShutdown>(HandleShutdown);
        }

        private IEnumerable<Joint> GetAllJoints()
        {
            foreach (var jointComp in EntityManager.EntityQuery<JointComponent>(true))
            {
                foreach (var (_, joint) in jointComp.Joints)
                {
                    yield return joint;
                }
            }
        }

        private void HandleShutdown(EntityUid uid, JointComponent component, ComponentShutdown args)
        {
            foreach (var joint in component.Joints.Values)
            {
                RemoveJoint(joint);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var joint in _dirtyJoints)
            {
                if (joint.Deleted || joint.JointCount != 0) continue;
                EntityManager.RemoveComponent<JointComponent>(joint.Owner.Uid);
            }

            _dirtyJoints.Clear();
        }

        private static string GetJointId(Joint joint)
        {
            var id = joint.ID;
            return !string.IsNullOrEmpty(id) ? id : joint.GetHashCode().ToString();
        }

        #region Helpers
        /// <summary>
        /// Create a DistanceJoint between 2 bodies. This should be called content-side whenever you need one.
        /// BodyA and BodyB on the joint are sorted so may not necessarily match what you pass in.
        /// </summary>
        public DistanceJoint CreateDistanceJoint(EntityUid bodyA, EntityUid bodyB, Vector2? anchorA = null, Vector2? anchorB = null, string? id = null)
        {
            anchorA ??= Vector2.Zero;
            anchorB ??= Vector2.Zero;

            var joint = new DistanceJoint(bodyA, bodyB, anchorA.Value, anchorB.Value);
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
        protected void AddJoint(Joint joint)
        {
            var bodyA = joint.BodyA;
            var bodyB = joint.BodyB;

            // Maybe make this method AddOrUpdate so we can have an Add one that explicitly throws if present?
            var mapidA = bodyA.Owner.Transform.MapID;

            if (mapidA == MapId.Nullspace ||
                mapidA != bodyB.Owner.Transform.MapID)
            {
                Logger.ErrorS("physics", $"Tried to add joint to ineligible bodies");
                return;
            }

            if (string.IsNullOrEmpty(joint.ID))
            {
                Logger.ErrorS("physics", $"Can't add a joint with no ID");
                DebugTools.Assert($"Can't add a joint with no ID");
                return;
            }

            var jointComponentA = EntityManager.EnsureComponent<JointComponent>(bodyA.Owner);
            var jointComponentB = EntityManager.EnsureComponent<JointComponent>(bodyB.Owner);
            var jointsA = jointComponentA.Joints;
            var jointsB = jointComponentB.Joints;

            if (jointsA.ContainsKey(joint.ID))
            {
                // If they both already have it we should be gucci
                // This can occur because of client states coming in blah blah
                // The reason for this is we defer everything until Update
                // (and the reason we defer is to avoid modifying components during iteration when we do the EnsureComponent)
                if (jointsB.ContainsKey(joint.ID)) return;

                Logger.ErrorS("physics", $"Existing joint {joint.ID} on {bodyA.Owner}");
                return;
            }

            if (jointsB.ContainsKey(joint.ID))
            {
                Logger.ErrorS("physics", $"Existing joint {joint.ID} on {bodyB.Owner}");
                return;
            }
            Logger.DebugS("physics", $"Added joint {joint.ID}");


            jointsA.Add(joint.ID, joint);
            jointsB.Add(joint.ID, joint);

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (!joint.CollideConnected)
            {
                FilterContactsForJoint(joint);
            }

            bodyA.WakeBody();
            bodyB.WakeBody();
            bodyA.Dirty();
            bodyB.Dirty();
            jointComponentA.Dirty();
            jointComponentB.Dirty();
            // Note: creating a joint doesn't wake the bodies.

            // Raise broadcast last so we can do both sides of directed first.
            var vera = new JointAddedEvent(joint, bodyA, bodyB);
            EntityManager.EventBus.RaiseLocalEvent(bodyA.Owner.Uid, vera, false);
            var smug = new JointAddedEvent(joint, bodyB, bodyA);
            EntityManager.EventBus.RaiseLocalEvent(bodyB.Owner.Uid, smug, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);
        }

        public void ClearJoints(PhysicsComponent body)
        {
            if (!body.Owner.HasComponent<JointComponent>()) return;

            EntityManager.RemoveComponent<JointComponent>(body.Owner.Uid);
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

            Logger.DebugS("physics", $"Removed joint {joint.ID}");

            // Wake up connected bodies.
            if (EntityManager.TryGetComponent<PhysicsComponent>(bodyAUid, out var bodyA))
            {
                bodyA.Awake = true;
            }

            if (EntityManager.TryGetComponent<PhysicsComponent>(bodyBUid, out var bodyB))
            {
                bodyB.Awake = true;
            }

            if (!jointComponentA.Deleted)
            {
                jointComponentA.Dirty();
            }

            if (!jointComponentB.Deleted)
            {
                jointComponentB.Dirty();
            }

            if (jointComponentA.Deleted && jointComponentB.Deleted)
                return;

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (!joint.CollideConnected)
            {
                FilterContactsForJoint(joint);
            }

            var vera = new JointRemovedEvent(joint, bodyA, bodyB);
            EntityManager.EventBus.RaiseLocalEvent(bodyA.Owner.Uid, vera, false);
            var smug = new JointRemovedEvent(joint, bodyB, bodyA);
            EntityManager.EventBus.RaiseLocalEvent(bodyB.Owner.Uid, smug, false);
            EntityManager.EventBus.RaiseEvent(EventSource.Local, vera);

            // We can't just check up front due to how prediction works.
            _dirtyJoints.Add(jointComponentA);
            _dirtyJoints.Add(jointComponentB);
        }
        #endregion

        internal void FilterContactsForJoint(Joint joint)
        {
            var bodyA = joint.BodyA;
            var bodyB = joint.BodyB;

            var edge = bodyB.ContactEdges;
            while (edge != null)
            {
                if (edge.Other == bodyA)
                {
                    // Flag the contact for filtering at the next time step (where either
                    // body is awake).
                    edge.Contact!.FilterFlag = true;
                }

                edge = edge.Next;
            }
        }
    }
}
