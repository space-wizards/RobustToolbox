using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;
using PhysicsComponent = Robust.Shared.GameObjects.PhysicsComponent;

namespace Robust.Shared.Physics.Dynamics
{
    public sealed class PhysicsMap
    {
        // TODO: FixedRotation. I hope most of the rigidbody bugs are from this not being set so need to add it and pray.
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        // AKA world.

        internal ContactManager ContactManager = new();

        /// <summary>
        ///     Change the global gravity vector.
        /// </summary>
        public Vector2 Gravity
        {
            get => _gravity;
            set
            {
                if (_gravity.EqualsApprox(value)) return;

                // Force every body awake just in case.
                foreach (var body in Bodies)
                {
                    if (body.BodyType != BodyType.Dynamic) continue;
                    body.Awake = true;
                }

                _gravity = value;
            }
        }

        private Vector2 _gravity;

        /// <summary>
        ///     All bodies present on this map.
        /// </summary>
        public HashSet<PhysicsComponent> Bodies = new();

        /// <summary>
        ///     All awake bodies on this map.
        /// </summary>
        public HashSet<PhysicsComponent> AwakeBodies = new();

        /// <summary>
        ///     Temporary body storage during solving.
        /// </summary>
        private List<PhysicsComponent> _awakeBodyList = new();

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
        internal Queue<Contact> ContactPool = new(128);

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
        public void AddBodyDeferred(PhysicsComponent body)
        {
            // DebugTools.Assert(!_queuedBodyAdd.Contains(body));
            _queuedBodyAdd.Add(body);
        }

        public void AddAwakeBody(PhysicsComponent body)
        {
            _queuedWake.Add(body);
        }

        public void RemoveBodyDeferred(PhysicsComponent body)
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
            // TODO: Need static helper class to easily create Joints
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
                AddBody(body);
            }

            _queuedBodyAdd.Clear();
        }

        public void AddBody(PhysicsComponent body)
        {
            if (Bodies.Contains(body)) return;

            // TODO: Kinda dodgy with this and wake shit.
            // Look at my note under ProcessWakeQueue
            if (body.Awake && body.BodyType != BodyType.Static)
            {
                _queuedWake.Remove(body);
                AwakeBodies.Add(body);
            }
            Bodies.Add(body);
            body.PhysicsMap = this;
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
                // Sloth note: So FPE doesn't seem to handle static bodies being woken gracefully as they never sleep
                // (No static body's an island so can't increase their min sleep time).
                // AFAIK not adding it to woken bodies shouldn't matter for anything tm...
                if (!Bodies.Contains(body) || !body.Awake || body.BodyType == BodyType.Static) continue;
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
                // TODO: Optimise dafuk out of this.
                if (Joints.Contains(joint)) continue;

                // Just end me, I fucken hate how garbage the physics compstate is.

                // because EACH body will have a joint update we needs to check if.

                PhysicsComponent? bodyA;
                PhysicsComponent? bodyB;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (joint.BodyA == null || joint.BodyB == null)
                {
                    if (!_entityManager.TryGetEntity(joint.BodyAUid, out var bodyAEntity) ||
                        !_entityManager.TryGetEntity(joint.BodyBUid, out var bodyBEntity))
                    {
                        continue;
                    }

                    if (!bodyAEntity.TryGetComponent(out bodyA) ||
                        !bodyBEntity.TryGetComponent(out bodyB))
                    {
                        continue;
                    }

                    // TODO: Need to mark all this shit as nullable coming in from the state probably.
                    joint.BodyA = bodyAEntity.GetComponent<PhysicsComponent>();
                    joint.BodyB = bodyBEntity.GetComponent<PhysicsComponent>();
                }
                else
                {
                    bodyA = joint.BodyA;
                    bodyB = joint.BodyB;
                }

                // BodyA and BodyB should share joints so we can just check if BodyA already has this joint.
                for (var je = bodyA.JointEdges; je != null; je = je.Next)
                {
                    if (je.Joint.Equals(joint)) continue;
                }

                // Connect to the world list.
                Joints.Add(joint);

                // Connect to the bodies' doubly linked lists.
                joint.EdgeA.Joint = joint;
                joint.EdgeA.Other = bodyB;
                joint.EdgeA.Prev = null;
                joint.EdgeA.Next = bodyA.JointEdges;

                if (bodyA.JointEdges != null)
                    bodyA.JointEdges.Prev = joint.EdgeA;

                bodyA.JointEdges = joint.EdgeA;

                joint.EdgeB.Joint = joint;
                joint.EdgeB.Other = bodyA;
                joint.EdgeB.Prev = null;
                joint.EdgeB.Next = bodyB.JointEdges;

                if (bodyB.JointEdges != null)
                    bodyB.JointEdges.Prev = joint.EdgeB;

                bodyB.JointEdges = joint.EdgeB;

                joint.BodyAUid = bodyA.Owner.Uid;
                joint.BodyBUid = bodyB.Owner.Uid;

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

                bodyA.Dirty();
                bodyB.Dirty();
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
            // Unfortunately this NEEDS to be predicted to make pushing remotely fucking good.
            ContactManager.FindNewContacts(MapId);

            var invDt = frameTime > 0.0f ? 1.0f / frameTime : 0.0f;
            var dtRatio = _invDt0 * frameTime;

            foreach (var controller in _controllers)
            {
                controller.UpdateBeforeSolve(prediction, this, frameTime);
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

            // TODO: SolveTOI

            foreach (var controller in _controllers)
            {
                controller.UpdateAfterSolve(prediction, this, frameTime);
            }

            ClearForces();

            _invDt0 = invDt;
        }

        private void Solve(float frameTime, float dtRatio, float invDt, bool prediction)
        {
            // Re-size island for worst-case -> TODO Probably smaller than this given everything's awake at the start?
            _island.Reset(Bodies.Count, ContactManager.ActiveContacts.Count, Joints.Count);

            DebugTools.Assert(_islandSet.Count == 0);

            foreach (var contact in ContactManager.ActiveContacts)
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

            _awakeBodyList.AddRange(AwakeBodies);

            // Build the relevant islands / graphs for all bodies.
            foreach (var seed in _awakeBodyList)
            {
                // I tried not running prediction for non-contacted entities but unfortunately it looked like shit
                // when contact broke so if you want to try that then GOOD LUCK.
                if (// prediction && !seed.Predict ||
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

                _island.Solve(Gravity, frameTime, dtRatio, invDt, prediction);

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
            _awakeBodyList.Clear();

            ContactManager.PostSolve();
        }

        private void ClearForces()
        {
            foreach (var body in AwakeBodies)
            {
                body.Force = Vector2.Zero;
                body.Torque = 0.0f;
            }
        }
    }
}
