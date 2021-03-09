/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;
using PhysicsComponent = Robust.Shared.GameObjects.PhysicsComponent;

namespace Robust.Shared.Physics.Dynamics
{
    public sealed class PhysicsMap
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private SharedPhysicsSystem _physicsSystem = default!;

        internal ContactManager ContactManager = new();

        private bool _autoClearForces;

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

        // TODO: Given physics bodies are a common thing to be listening for on moveevents it's probably beneficial to have 2 versions; one that includes the entity
        // and one that includes the body
        private List<(ITransformComponent, IPhysBody)> _deferredUpdates = new();

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

        private HashSet<Joint> _queuedJointAdd = new();
        private HashSet<Joint> _queuedJointRemove = new();

        private HashSet<PhysicsComponent> _queuedWake = new();
        private HashSet<PhysicsComponent> _queuedSleep = new();

        private Queue<CollisionChangeMessage> _queuedCollisionMessages = new();

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
            _physicsSystem = EntitySystem.Get<SharedPhysicsSystem>();
        }

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            ContactManager.Initialize();
            ContactManager.MapId = MapId;
            _island = new PhysicsIsland();
            _island.Initialize();

            _autoClearForces = _configManager.GetCVar(CVars.AutoClearForces);
            _configManager.OnValueChanged(CVars.AutoClearForces, value => _autoClearForces = value);
        }

        #region AddRemove
        public void AddAwakeBody(PhysicsComponent body)
        {
            _queuedWake.Add(body);
        }

        public void RemoveBody(PhysicsComponent body)
        {
            Bodies.Remove(body);
            AwakeBodies.Remove(body);
            body.DestroyContacts();
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
            ProcessBodyChanges();
            ProcessWakeQueue();
            ProcessSleepQueue();
            ProcessAddedJoints();
            ProcessRemovedJoints();
        }

        private void ProcessBodyChanges()
        {
            while (_queuedCollisionMessages.Count > 0)
            {
                var message = _queuedCollisionMessages.Dequeue();

                if (!message.Body.Deleted && message.Body.CanCollide)
                {
                    AddBody(message.Body);
                }
                else
                {
                    RemoveBody(message.Body);
                }
            }
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

            foreach (var controller in _physicsSystem.Controllers)
            {
                controller.UpdateBeforeMapSolve(prediction, this, frameTime);
            }

            ContactManager.Collide();
            // Don't run collision behaviors during FrameUpdate?
            if (!prediction)
                ContactManager.PreSolve(frameTime);

            // Remove all deleted entities etc.
            ProcessChanges();

            // Integrate velocities, solve velocity constraints, and do integration.
            Solve(frameTime, dtRatio, invDt, prediction);

            // TODO: SolveTOI

            foreach (var controller in _physicsSystem.Controllers)
            {
                controller.UpdateAfterMapSolve(prediction, this, frameTime);
            }

            // Box2d recommends clearing (if you are) during fixed updates rather than variable if you are using it
            if (!prediction && _autoClearForces)
                ClearForces();

            _invDt0 = invDt;
        }

        /// <summary>
        ///     Go through all of the deferred MoveEvents and then run them
        /// </summary>
        public void ProcessQueue()
        {
            // We'll store the WorldAABB on the MoveEvent given a lot of stuff ends up re-calculating it.
            foreach (var (transform, physics) in _deferredUpdates)
            {
                transform.RunDeferred(physics.GetWorldAABB(_mapManager));
            }

            _deferredUpdates.Clear();
        }

        private void Solve(float frameTime, float dtRatio, float invDt, bool prediction)
        {
            // Re-size island for worst-case -> TODO Probably smaller than this given everything's awake at the start?
            _island.Reset(Bodies.Count, ContactManager.ContactCount, Joints.Count);

            DebugTools.Assert(_islandSet.Count == 0);

            for (Contact? c = ContactManager.ContactList.Next; c != ContactManager.ContactList; c = c.Next)
            {
                c!.IslandFlag = false;
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
                // prediction && !seed.Predict ||
                // AHHH need a way to ignore paused for mapping (seed.Paused && !seed.Owner.TryGetComponent(out IMoverComponent)) ||
                if ((prediction && !seed.Predict) ||
                    (seed.Paused && !seed.IgnorePaused) ||
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

                    // Static bodies don't propagate islands
                    if (body.BodyType == BodyType.Static) continue;

                    // As static bodies can never be awake (unlike Farseer) we'll set this after the check.
                    body.Awake = true;

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

                _island.Solve(Gravity, frameTime, dtRatio, invDt, prediction, _deferredUpdates);

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

                // So Box2D would update broadphase here buutttt we'll just wait until MoveEvent queue is used.
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
