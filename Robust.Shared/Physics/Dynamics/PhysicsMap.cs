using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
    public sealed class PhysicsMap
    {
        // AKA world.

        internal ContactManager ContactManager = new();

        /// <summary>
        ///     All bodies present on this map.
        /// </summary>
        public HashSet<PhysicsComponent> Bodies = new();

        /// <summary>
        ///     All awake bodies on this map.
        /// </summary>
        public HashSet<PhysicsComponent> AwakeBodies = new();

        /// <summary>
        ///     Get all the joints on this map
        /// </summary>
        public List<Joint> Joints { get; private set; } = new();

        /// <summary>
        ///     Temporarily store island-bodies for easier iteration.
        /// </summary>
        private HashSet<PhysicsComponent> _islandSet = new();

        /// <summary>
        ///     Physics controllers for this map.
        /// </summary>
        private List<AetherController> _controllers = new();

        // Queued map changes
        private HashSet<PhysicsComponent> _queuedBodyAdd = new();
        private HashSet<PhysicsComponent> _queuedBodyRemove = new();

        private HashSet<Joint> _queuedJointAdd = new();
        private HashSet<Joint> _queuedJointRemove = new();

        private HashSet<PhysicsComponent> _queuedWake = new();
        private HashSet<PhysicsComponent> _queuedSleep = new();

        /// <summary>
        ///     We'll re-use contacts where possible to save on allocations.
        /// </summary>
        internal Queue<Contact> _contactPool = new(256);

        private PhysicsIsland _island = default!;

        /// <summary>
        ///     To build islands we do a depth-first search of all colliding bodies and group them together.
        ///     This stack is used to store bodies that are colliding.
        /// </summary>
        private PhysicsComponent[] _stack = new PhysicsComponent[64];

        /// <summary>
        ///     Store last tick's invDT
        /// </summary>
        private float _invDt0;

        public MapId MapId { get; }

        public PhysicsMap(MapId mapId)
        {
            MapId = mapId;
        }

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            ContactManager.Initialize();
            ContactManager.MapId = MapId;
            _island = new PhysicsIsland();
            _island.Initialize();

            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (var controller in EntitySystem.Get<SharedPhysicsSystem>().ControllerTypes)
            {
                _controllers.Add((AetherController) typeFactory.CreateInstance(controller));
            }

            foreach (var controller in _controllers)
            {
                controller.Initialize();
            }
        }

        #region AddRemove
        public void AddBody(PhysicsComponent body)
        {
            // DebugTools.Assert(!_queuedBodyAdd.Contains(body));
            _queuedBodyAdd.Add(body);
        }

        public void AddAwakeBody(PhysicsComponent body)
        {
            _queuedWake.Add(body);
        }

        public void RemoveBody(PhysicsComponent body)
        {
            // DebugTools.Assert(!_queuedBodyRemove.Contains(body));
            _queuedBodyRemove.Add(body);
        }

        public void RemoveSleepBody(PhysicsComponent body)
        {
            _queuedSleep.Add(body);
        }

        public void AddJoint(Joint joint)
        {
            _queuedJointAdd.Add(joint);
        }

        public void RemoveJoint(Joint joint)
        {
            _queuedJointRemove.Add(joint);
        }

        #endregion

        #region Queue
        private void ProcessChanges()
        {
            ProcessAddQueue();
            ProcessRemoveQueue();
            ProcessWakeQueue();
            ProcessSleepQueue();
            ProcessAddedJoints();
            ProcessRemovedJoints();
        }

        private void ProcessAddQueue()
        {
            foreach (var body in _queuedBodyAdd)
            {
                // TODO: Kinda dodgy with this and wake shit.
                if (body.Awake)
                {
                    _queuedWake.Remove(body);
                    AwakeBodies.Add(body);
                }
                Bodies.Add(body);
                body.PhysicsMap = this;
            }

            _queuedBodyAdd.Clear();
        }

        private void ProcessRemoveQueue()
        {
            foreach (var body in _queuedBodyRemove)
            {
                Bodies.Remove(body);
                AwakeBodies.Remove(body);
            }

            _queuedBodyRemove.Clear();
        }

        private void ProcessWakeQueue()
        {
            foreach (var body in _queuedWake)
            {
                if (!Bodies.Contains(body) || !body.Awake) continue;
                AwakeBodies.Add(body);
            }

            _queuedWake.Clear();
        }

        private void ProcessSleepQueue()
        {
            foreach (var body in _queuedSleep)
            {
                if (body.Awake) continue;

                AwakeBodies.Remove(body);
            }

            _queuedSleep.Clear();
        }

        private void ProcessAddedJoints()
        {
            foreach (var joint in _queuedJointAdd)
            {
                // Connect to the world list.
                Joints.Add(joint);

                // Connect to the bodies' doubly linked lists.
                joint.EdgeA.Joint = joint;
                joint.EdgeA.Other = joint.BodyB;
                joint.EdgeA.Prev = null;
                joint.EdgeA.Next = joint.BodyA.JointEdges;

                if (joint.BodyA.JointEdges != null)
                    joint.BodyA.JointEdges.Prev = joint.EdgeA;

                joint.BodyA.JointEdges = joint.EdgeA;


                joint.EdgeB.Joint = joint;
                joint.EdgeB.Other = joint.BodyA;
                joint.EdgeB.Prev = null;
                joint.EdgeB.Next = joint.BodyB.JointEdges;

                if (joint.BodyB.JointEdges != null)
                    joint.BodyB.JointEdges.Prev = joint.EdgeB;

                joint.BodyB.JointEdges = joint.EdgeB;

                PhysicsComponent bodyA = joint.BodyA;
                PhysicsComponent bodyB = joint.BodyB;

                // If the joint prevents collisions, then flag any contacts for filtering.
                if (!joint.CollideConnected)
                {
                    ContactEdge? edge = bodyB.ContactEdges;
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
                // Note: creating a joint doesn't wake the bodies.
            }

            _queuedJointAdd.Clear();
        }

        private void ProcessRemovedJoints()
        {
            foreach (var joint in _queuedJointRemove)
            {
                bool collideConnected = joint.CollideConnected;

                // Remove from the world list.
                Joints.Remove(joint);

                // Disconnect from island graph.
                PhysicsComponent bodyA = joint.BodyA;
                PhysicsComponent bodyB = joint.BodyB;

                // Wake up connected bodies.
                bodyA.Awake = true;

                bodyB.Awake = true;

                // Remove from body 1.
                if (joint.EdgeA.Prev != null)
                {
                    joint.EdgeA.Prev.Next = joint.EdgeA.Next;
                }

                if (joint.EdgeA.Next != null)
                {
                    joint.EdgeA.Next.Prev = joint.EdgeA.Prev;
                }

                if (joint.EdgeA == bodyA.JointEdges)
                {
                    bodyA.JointEdges = joint.EdgeA.Next;
                }

                joint.EdgeA.Prev = null;
                joint.EdgeA.Next = null;

                // Remove from body 2
                if (joint.EdgeB.Prev != null)
                {
                    joint.EdgeB.Prev.Next = joint.EdgeB.Next;
                }

                if (joint.EdgeB.Next != null)
                {
                    joint.EdgeB.Next.Prev = joint.EdgeB.Prev;
                }

                if (joint.EdgeB == bodyB.JointEdges)
                {
                    bodyB.JointEdges = joint.EdgeB.Next;
                }

                joint.EdgeB.Prev = null;
                joint.EdgeB.Next = null;

                // If the joint prevents collisions, then flag any contacts for filtering.
                if (!collideConnected)
                {
                    ContactEdge? edge = bodyB.ContactEdges;
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

            _queuedJointRemove.Clear();
        }

        #endregion

        /// <summary>
        ///     Where the magic happens.
        /// </summary>
        /// <param name="frameTime"></param>
        /// <param name="prediction"></param>
        public void Step(float frameTime, bool prediction)
        {
            // The original doesn't call ProcessChanges quite so much but stuff like collision behaviors
            // can edit things during the solver so we'll just handle it as it comes up.
            ProcessChanges();

            // Box2D does this at the end of a step and also here when there's a fixture update.
            // Given external stuff can move bodies we'll just do this here.
            ContactManager.FindNewContacts(MapId);

            var invDt = frameTime > 0.0f ? 1.0f / frameTime : 0.0f;
            var dtRatio = _invDt0 * frameTime;

            foreach (var controller in _controllers)
            {
                controller.UpdateBeforeSolve(frameTime);
            }

            ContactManager.Collide();

            // TODO: May move this as a PostSolve once we have broadphase collisions where contacts can be generated
            // even though the bodies may not technically be colliding
            if (!prediction)
                ContactManager.PreSolve();

            // Remove all deleted entities etc.
            ProcessChanges();

            // Integrate velocities, solve velocity constraints, and do integration.
            Solve(frameTime, dtRatio, invDt, prediction);

            // SolveTOI

            foreach (var controller in _controllers)
            {
                controller.UpdateAfterSolve(frameTime);
            }

            ClearForces();

            _invDt0 = invDt;
        }

        private void Solve(float frameTime, float dtRatio, float invDt, bool prediction)
        {
            // Re-size island for worst-case -> TODO Probably smaller than this given everything's awake at the start?
            _island.Reset(AwakeBodies.Count, ContactManager.ContactList.Count, Joints.Count);

            foreach (var contact in ContactManager.ContactList)
            {
                contact.IslandFlag = false;
            }

            foreach (var joint in Joints)
            {
                joint.IslandFlag = false;
            }

            // Build and simulated islands from awake bodies.
            // Ideally you don't need a stack size for all bodies but we'll optimise it later.
            var stackSize = Bodies.Count;
            if (stackSize > _stack.Length)
            {
                Array.Resize(ref _stack, Math.Max(_stack.Length * 2, stackSize));
            }

            // Build the relevant islands / graphs for all bodies.
            foreach (var seed in AwakeBodies)
            {
                // Sloth change: If client's running prediction we won't run physics for non-predicted bodies (that is unless a predicted body is in the same island).
                if (prediction && !seed.Predict ||
                    seed.Island ||
                    !seed.CanCollide ||
                    seed.BodyType == BodyType.Static) continue;

                // Start of a new island
                _island.Clear();
                var stackCount = 0;
                _stack[stackCount++] = seed;

                _islandSet.Add(seed);
                seed.Island = true;

                while (stackCount > 0)
                {
                    var body = _stack[--stackCount];
                    _island.Add(body);
                    _islandSet.Add(body);
                    body.Awake = true;

                    // Static bodies don't propagate islands
                    if (body.BodyType == BodyType.Static) continue;

                    for (var contactEdge = body.ContactEdges; contactEdge != null; contactEdge = contactEdge.Next)
                    {
                        var contact = contactEdge.Contact!;

                        // Has this contact already been added to an island?
                        if (contact.IslandFlag) continue;

                        // Is this contact solid and touching?
                        if (!contact.Enabled || !contact.IsTouching) continue;

                        // Skip sensors.
                        if (contact.FixtureA?.Hard != true || contact.FixtureB?.Hard != true) continue;

                        _island.Add(contact);
                        contact.IslandFlag = true;

                        var other = contactEdge.Other!;

                        // Was the other body already added to this island?
                        if (other.Island) continue;

                        DebugTools.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;

                        if (!_islandSet.Contains(body))
                            _islandSet.Add(body);

                        other.Island = true;
                    }

                    for (JointEdge? je = body.JointEdges; je != null; je = je.Next)
                    {
                        if (je.Joint.IslandFlag)
                        {
                            continue;
                        }

                        PhysicsComponent other = je.Other;

                        // Don't simulate joints connected to inactive bodies.
                        if (!other.CanCollide) continue;

                        _island.Add(je.Joint);
                        je.Joint.IslandFlag = true;

                        if (other.Island) continue;

                        DebugTools.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;

                        if (!_islandSet.Contains(body))
                            _islandSet.Add(body);

                        other.Island = true;
                    }
                }

                _island.Solve(frameTime, dtRatio, invDt,  prediction);

                // Post-solve cleanup for island
                for (var i = 0; i < _island.BodyCount; i++)
                {
                    var body = _island.Bodies[i];

                    // Static bodies can participate in other islands
                    if (body.BodyType == BodyType.Static)
                    {
                        body.Island = false;
                    }
                }
            }

            foreach (var body in _islandSet)
            {
                if (!body.Island || body.Deleted)
                {
                    continue;
                }

                body.Island = false;
                DebugTools.Assert(body.BodyType != BodyType.Static);

                // TODO: Update BroadPhase -> Maybe just have LocalPosition update broadphase directly?
                // Still need to suss this as MoveEvent is currently being used.
            }

            _islandSet.Clear();

            foreach (var body in AwakeBodies)
            {
                if (body.Deleted) continue;

                foreach (var controller in body.GetControllers())
                {
                    controller.UpdateAfterProcessing();
                }
            }

            ContactManager.PostSolve();
        }

        private void ClearForces()
        {
            foreach (var body in AwakeBodies)
            {
                body.Force = Vector2.Zero;
                body.Torque = 0.0f;

                foreach (var controller in body.GetControllers())
                {
                    controller.Impulse = Vector2.Zero;
                    controller.LinearVelocity = Vector2.Zero;
                }
            }
        }
    }
}
