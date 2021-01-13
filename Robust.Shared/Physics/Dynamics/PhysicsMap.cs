using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class PhysicsMap
    {
        // AKA world.

        private ContactManager _contactManager = new();

        /// <summary>
        ///     All bodies present on this map.
        /// </summary>
        public HashSet<PhysicsComponent> Bodies = new();

        /// <summary>
        ///     All awake bodies on this map.
        /// </summary>
        public HashSet<PhysicsComponent> AwakeBodies = new();

        /// <summary>
        ///     Temporarily store island-bodies for easier iteration.
        /// </summary>
        private HashSet<PhysicsComponent> _islandSet = new();

        // Queued map changes
        private HashSet<PhysicsComponent> _queuedBodyAdd = new();
        private HashSet<PhysicsComponent> _queuedBodyRemove = new();

        private HashSet<PhysicsComponent> _queuedWake = new();
        private HashSet<PhysicsComponent> _queuedSleep = new();

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

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _contactManager.Initialize();
            _island = new PhysicsIsland();
            _island.Initialize();
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

        // TODO: Someday joints too.

        #endregion

        #region Queue
        private void ProcessChanges()
        {
            ProcessAddQueue();
            ProcessRemoveQueue();
            ProcessWakeQueue();
            ProcessSleepQueue();
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
            }

            _queuedBodyAdd.Clear();
        }

        private void ProcessRemoveQueue()
        {
            foreach (var body in _queuedBodyRemove)
            {
                Bodies.Remove(body);

                if (body.Awake)
                {
                    AwakeBodies.Remove(body);
                }
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

            var invDt = frameTime > 0.0f ? 1.0f / frameTime : 0.0f;
            var dtRatio = _invDt0 * frameTime;

            // Update controllers
            foreach (var body in AwakeBodies)
            {
                foreach (var controller in body.GetControllers())
                {
                    controller.UpdateBeforeProcessing();
                }
            }

            _contactManager.Collide(this, prediction, frameTime);

            // TODO: May move this as a PostSolve once we have broadphase collisions where contacts can be generated
            // even though the bodies may not technically be colliding
            if (!prediction)
                _contactManager.PreSolve();

            // Remove all deleted entities etc.
            ProcessChanges();

            // Integrate velocities, solve velocity constraints, and do integration.
            Solve(frameTime, prediction);

            // SolveTOI

            ClearForces();

            _invDt0 = invDt;
        }

        private void Solve(float frameTime, bool prediction)
        {
            // Re-size island for worst-case -> TODO Probably smaller than this given everything's awake at the start?
            _island.Reset(AwakeBodies.Count, _contactManager.ContactList.Count);

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
                if (seed.Island || !seed.CanCollide || seed.BodyType == BodyType.Static) continue;

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

                    for (var i = 0; i < body.ContactEdges.Count; i++)
                    {
                        var contactEdge = body.ContactEdges[i];
                        var contact = contactEdge.Contact;

                        if (contact.IslandFlag || !contact.Manifold.Hard) continue;

                        _island.Add(contact);
                        contact.IslandFlag = true;

                        var other = contactEdge.Other;

                        // If it was already added to this island.
                        if (other.Island) continue;

                        DebugTools.Assert(stackCount < stackSize);

                        _stack[stackCount++] = other;
                        _islandSet.Add(body);
                        other.Island = true;
                    }

                    // TODO: Joint edges
                }

                _island.Solve(frameTime, prediction);

                // Post-solve cleanup for island
                foreach (var body in _island.Bodies)
                {
                    if (body == null) break;

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

                // TODO: Update BroadPhase
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

            _contactManager.PostSolve();
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
