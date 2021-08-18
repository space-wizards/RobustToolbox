using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Joints;

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
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private Queue<JointEvent> _events = new();

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedPhysicsSystem));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_events.TryDequeue(out var @event))
            {
                switch (@event)
                {
                    case AddJointEvent add:
                        AddJoint(add);
                        break;
                    case RemoveJointEvent remove:
                        RemoveJoint(remove);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void AddJointDeferred(PhysicsComponent bodyA, PhysicsComponent bodyB, Joint joint)
        {
            var msg = new AddJointEvent(bodyA, bodyB, joint);
            _events.Enqueue(msg);
        }

        public void RemoveJointDeferred(Joint joint)
        {
            var msg = new RemoveJointEvent(joint);
            _events.Enqueue(msg);
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
        public DistanceJoint CreateDistanceJoint(PhysicsComponent bodyA, PhysicsComponent bodyB, Vector2? anchorA = null, Vector2? anchorB = null, string? id = null)
        {
            anchorA ??= Vector2.Zero;
            anchorB ??= Vector2.Zero;

            var joint = new DistanceJoint(bodyA, bodyB, anchorA.Value, anchorB.Value, _configManager);
            id ??= GetJointId(joint);
            joint.ID = id;
            AddJointDeferred(bodyA, bodyB, joint);

            return joint;
        }
        #endregion

        public static void LinearStiffness(
            float frequencyHertz,
            float dampingRatio,
            PhysicsComponent bodyA,
            PhysicsComponent bodyB,
            out float stiffness, out float damping)
        {
            var massA = bodyA.Mass;
            var massB = bodyB.Mass;

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
        private void AddJoint(AddJointEvent @event)
        {
            var bodyA = @event.BodyA;
            var bodyB = @event.BodyB;

            // Maybe make this method AddOrUpdate so we can have an Add one that explicitly throws if present?
            var mapidA = bodyA.Owner.Transform.MapID;

            if (mapidA == MapId.Nullspace ||
                mapidA != bodyB.Owner.Transform.MapID)
            {
                Logger.ErrorS("physics", $"Tried to add joint to ineligible bodies");
                return;
            }

            var joint = @event.Joint;

            if (string.IsNullOrEmpty(joint.ID))
            {
                Logger.ErrorS("physics", $"Can't add a joint with no ID");
                return;
            }

            var jointComponentA = bodyA.Owner.EnsureComponent<JointComponent>();
            var jointComponentB = bodyB.Owner.EnsureComponent<JointComponent>();
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
            Logger.DebugS("physics", $"Added joint {@event.Joint.ID}");


            jointsA.Add(joint.ID, joint);
            jointsB.Add(joint.ID, joint);

            // If the joint prevents collisions, then flag any contacts for filtering.
            if (!joint.CollideConnected)
            {
                FilterContactsForJoint(joint);
            }

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

            ComponentManager.RemoveComponent<JointComponent>(body.Owner.Uid);
        }
        private void RemoveJoint(RemoveJointEvent @event)
        {
            var joint = @event.Joint;

            var bodyA = joint.BodyA;
            var bodyB = joint.BodyB;

            // Originally I logged these but because of prediction the client can just nuke them multiple times in a row
            // because each body has its own JointComponent, bleh.
            if (!bodyA.Owner.TryGetComponent(out JointComponent? jointComponentA))
            {
                return;
            }

            if (!bodyB.Owner.TryGetComponent(out JointComponent? jointComponentB))
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

            Logger.DebugS("physics", $"Removed joint {@event.Joint.ID}");

            // Wake up connected bodies.
            bodyA.Awake = true;
            bodyB.Awake = true;

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

            /*
            if (jointComponentA.Joints.Count == 0)
                bodyA.Owner.RemoveComponent<JointComponent>();

            if (jointComponentB.Joints.Count == 0)
                bodyB.Owner.RemoveComponent<JointComponent>();
            */

            jointComponentA.Dirty();
            jointComponentB.Dirty();
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
