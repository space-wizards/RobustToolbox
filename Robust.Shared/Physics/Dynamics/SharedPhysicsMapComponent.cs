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
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;
using PhysicsComponent = Robust.Shared.GameObjects.PhysicsComponent;

namespace Robust.Shared.Physics.Dynamics
{
    public abstract class SharedPhysicsMapComponent : Component
    {
        [Dependency] private readonly IIslandManager _islandManager = default!;

        internal SharedBroadphaseSystem BroadphaseSystem = default!;
        internal SharedPhysicsSystem PhysicsSystem = default!;

        public override string Name => "PhysicsMap";

        internal ContactManager ContactManager = default!;

        public bool AutoClearForces;

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
        private List<(ITransformComponent Transform, PhysicsComponent Body)> _deferredUpdates = new();

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
        /// Temporary joint storage during solving
        /// </summary>
        private List<Joint> _joints = new();

        /// <summary>
        ///     Temporarily store island-bodies for easier iteration.
        /// </summary>
        private HashSet<PhysicsComponent> _islandSet = new();

        private HashSet<PhysicsComponent> _queuedWake = new();
        private HashSet<PhysicsComponent> _queuedSleep = new();

        private Queue<CollisionChangeMessage> _queuedCollisionMessages = new();

        private List<PhysicsComponent> _islandBodies = new(64);
        private List<Contact> _islandContacts = new(32);
        private List<Joint> _islandJoints = new(8);

        /// <summary>
        ///     To build islands we do a depth-first search of all colliding bodies and group them together.
        ///     This stack is used to store bodies that are colliding.
        /// </summary>
        private PhysicsComponent[] _stack = new PhysicsComponent[64];

        /// <summary>
        ///     Store last tick's invDT
        /// </summary>
        private float _invDt0;

        public MapId MapId => Owner.Transform.MapID;

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
        #endregion

        #region Queue
        private void ProcessChanges()
        {
            ProcessBodyChanges();
            ProcessWakeQueue();
            ProcessSleepQueue();
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
                if (!body.Awake || body.BodyType == BodyType.Static || !Bodies.Contains(body)) continue;
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
            BroadphaseSystem.FindNewContacts(MapId, prediction);

            var invDt = frameTime > 0.0f ? 1.0f / frameTime : 0.0f;
            var dtRatio = _invDt0 * frameTime;

            foreach (var controller in PhysicsSystem.Controllers)
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

            foreach (var controller in PhysicsSystem.Controllers)
            {
                controller.UpdateAfterMapSolve(prediction, this, frameTime);
            }

            // Box2d recommends clearing (if you are) during fixed updates rather than variable if you are using it
            if (!prediction && AutoClearForces)
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
                transform.RunDeferred(physics.GetWorldAABB());
            }

            _deferredUpdates.Clear();
        }

        private void Solve(float frameTime, float dtRatio, float invDt, bool prediction)
        {
            _islandManager.InitializePools();

            DebugTools.Assert(_islandSet.Count == 0);

            for (Contact? c = ContactManager.ContactList.Next; c != ContactManager.ContactList; c = c.Next)
            {
                c!.IslandFlag = false;
            }

            // Build and simulated islands from awake bodies.
            // Ideally you don't need a stack size for all bodies but we'll TODO: optimise it later.
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
                if (seed.Island ||
                    seed.Paused && !seed.IgnorePaused)
                {
                    continue;
                }

                if (prediction && !seed.Predict ||
                    !seed.CanCollide ||
                    seed.BodyType == BodyType.Static)
                {
                    continue;
                }

                // Start of a new island
                _islandBodies.Clear();
                _islandContacts.Clear();
                _islandJoints.Clear();
                var stackCount = 0;
                _stack[stackCount++] = seed;

                // TODO: Probably don't need _islandSet anymore.
                seed.Island = true;

                while (stackCount > 0)
                {
                    var body = _stack[--stackCount];
                    _islandBodies.Add(body);
                    _islandSet.Add(body);

                    // Static bodies don't propagate islands
                    if (body.BodyType == BodyType.Static) continue;

                    // As static bodies can never be awake (unlike Farseer) we'll set this after the check.
                    body.ForceAwake();

                    for (var contactEdge = body.ContactEdges; contactEdge != null; contactEdge = contactEdge.Next)
                    {
                        var contact = contactEdge.Contact!;

                        // Has this contact already been added to an island?
                        if (contact.IslandFlag) continue;

                        // Is this contact solid and touching?
                        if (!contact.Enabled || !contact.IsTouching) continue;

                        // Skip sensors.
                        if (contact.FixtureA?.Hard != true || contact.FixtureB?.Hard != true) continue;

                        _islandContacts.Add(contact);
                        contact.IslandFlag = true;

                        var other = contactEdge.Other!;

                        // Was the other body already added to this island?
                        if (other.Island) continue;

                        DebugTools.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;

                        other.Island = true;
                    }

                    if (!body.Owner.TryGetComponent(out JointComponent? jointComponent)) continue;

                    foreach (var (_, joint) in jointComponent.Joints)
                    {
                        if (joint.IslandFlag) continue;

                        var other = joint.BodyA == body ? joint.BodyB : joint.BodyA;

                        // Don't simulate joints connected to inactive bodies.
                        if (!other.CanCollide) continue;

                        _islandJoints.Add(joint);
                        joint.IslandFlag = true;

                        if (other.Island) continue;

                        DebugTools.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;

                        other.Island = true;
                    }
                }

                _islandManager
                    .AllocateIsland(_islandBodies.Count, _islandContacts.Count, _islandJoints.Count)
                    .Append(_islandBodies, _islandContacts, _islandJoints);

                _joints.AddRange(_islandJoints);

                // Allow static bodies to be re-used in other islands
                for (var i = 0; i < _islandBodies.Count; i++)
                {
                    var body = _islandBodies[i];

                    // Static bodies can participate in other islands
                    if (body.BodyType == BodyType.Static)
                    {
                        body.Island = false;
                    }
                }
            }

            SolveIslands(frameTime, dtRatio, invDt, prediction);
            Cleanup(frameTime);

            ContactManager.PostSolve();
        }

        protected virtual void Cleanup(float frameTime)
        {
            foreach (var body in _islandSet)
            {
                if (!body.Island || body.Deleted)
                {
                    continue;
                }

                body.IslandIndex.Clear();
                body.Island = false;
                DebugTools.Assert(body.BodyType != BodyType.Static);

                // So Box2D would update broadphase here buutttt we'll just wait until MoveEvent queue is used.
            }

            _islandSet.Clear();
            _awakeBodyList.Clear();

            foreach (var joint in _joints)
            {
                joint.IslandFlag = false;
            }

            _joints.Clear();
        }

        private void SolveIslands(float frameTime, float dtRatio, float invDt, bool prediction)
        {
            var islands = _islandManager.GetActive;
            // Islands are already pre-sorted
            var iBegin = 0;

            while (iBegin < islands.Count)
            {
                var island = islands[iBegin];

                island.Solve(Gravity, frameTime, dtRatio, invDt, prediction);
                iBegin++;
                // TODO: Submit rest in parallel if applicable
            }

            // TODO: parallel dispatch here

            // Update bodies sequentially to avoid race conditions. May be able to do this parallel someday
            // but easier to just do this for now.
            foreach (var island in islands)
            {
                island.UpdateBodies(_deferredUpdates);
                island.SleepBodies(prediction, frameTime);
            }
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
