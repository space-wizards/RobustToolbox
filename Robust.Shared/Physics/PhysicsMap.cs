using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     All of the physics components on a particular map.
    /// </summary>
    /// <remarks>
    ///     What you'd call a "World" in some other engines.
    /// </remarks>
    internal sealed class PhysicsMap
    {
        // TODO: Licences on all the shit.

        [Dependency] private readonly IContactManager _contactManager = default!;

        private HashSet<PhysicsComponent> _bodies = new HashSet<PhysicsComponent>();

        private HashSet<PhysicsComponent> _awakeBodies = new HashSet<PhysicsComponent>();

        // TODO: JointList

        private PhysicsIsland _island = new PhysicsIsland();

        private PhysicsComponent[] _stack = new PhysicsComponent[64];

        /// <summary>
        ///     Whether the simulation will run. Bodies added and removed are still processed regardless.
        /// </summary>
        public bool Enabled { get; set; }

        public bool AddBody(PhysicsComponent body)
        {
            if (body.Awake)
                _awakeBodies.Add(body);

            if (!_bodies.Add(body))
                return false;

            // TODO: Also set map on the body?

            // TODO: SetTransformIgnoreContacts

            if (Enabled)
                body.CreateProxies();

            _contactManager.FindNewContacts();

            return true;
        }

        public bool RemoveBody(PhysicsComponent body)
        {
            _awakeBodies.Remove(body);

            if (!_bodies.Remove(body))
                return false;

            // TODO: The other shit

            return true;
        }

        /// <summary>
        ///     Solve physics for this map.
        ///     Go through and build a small an island as possible to solve.
        /// </summary>
        /// <param name="frameTime"></param>
        public void Solve(float frameTime)
        {
            // TODO: Solve grids first and handle their movement as they're isolated.
            // Then, check for entities in the grid's new worldspace (reason for this is if we're moving in space
            // then we might overlap something that is inactive). Wake any entities in this region.

            // Also need to make sure broadphase stores trees per grid so when we move entities they're not updated
            // and shitting up our performance.

            // Then continue as normal.

            // Also Acruid / PJB talked a bit about it in Jan 2020.

            // Island reset
            // If use IslandSet?

            // TODO: Need to make IslandSet and shit member variables then reset it all here.
            foreach (var contact in _contactManager.ActiveContacts)
            {

            }

            // This also seems wasteful as fuckl
            foreach (var body in _awakeBodies)
            {

            }

            var stackSize = _awakeBodies.Count;
            if (stackSize > _stack.Length)
                _stack = new PhysicsComponent[Math.Max(_stack.Length * 2, stackSize)];

            var awakeBodyList = new List<PhysicsComponent>(_awakeBodies);
            var islandSet = new HashSet<PhysicsComponent>();

            foreach (var seed in awakeBodyList)
            {
                // So essentially we start at a body and will go through everything touching it, adding it to the island
                // Once there's no more left we then have our island to solve.
                // Rinse and repeat until every body is donesies.

                // TODO: If we check Static elsewhere we can remove this check.
                if (seed.Island || !seed.Enabled || seed.BodyType == BodyType.Static)
                    continue;

                _island.Clear();
                var stackCount = 0;
                _stack[stackCount++] = seed;

                // TODO: Probably don't need the Contains here and elsewhere
                // TODO: Aether2D calls this body coz wat
                if (!islandSet.Contains(seed))
                    islandSet.Add(seed);

                seed.Island = true;

                // Depth-first search on the constraint graph.
                while (stackCount > 0)
                {
                    var body = _stack[--stackCount];
                    DebugTools.Assert(body.Enabled);
                    _island.Add(body);

                    body.Awake = true;

                    // To keep island as small as possible don't propagate with static bodies.
                    // TODO: Might need to dump this as walls need behaviors
                    if (body.BodyType == BodyType.Static)
                        continue;

                    // TODO: Better way to do this shit?
                    for (var contactEdge = body.ContactList; contactEdge != null; contactEdge = contactEdge.Next)
                    {
                        var contact = contactEdge.Contact;

                        // If contact already in an island then skip
                        if (contact.IslandFlag || !contact.Enabled || !contact.IsTouching)
                            continue;

                        // Skip non-hard because I guess we'll find out later on the next time on dragon ball Z.
                        if (contact.FixtureA.IsSensor || contact.FixtureB.IsSensor)
                            continue;

                        _island.Add(contact);
                        contact.IslandFlag = true;

                        // Check if other body was already added to this island.
                        var other = contactEdge.Other;

                        if (other.Island)
                            continue;

                        DebugTools.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;

                        if (!islandSet.Contains(body))
                            islandSet.Add(body);

                        other.Island = true;
                    }

                    for (var jointEdge = body.JointList; jointEdge != null; jointEdge = jointEdge.Next)
                    {
                        if (jointEdge.Joint.IslandFlag)
                            continue;

                        var other = jointEdge.Other;

                        // WIP from Aether2D
                        ////Enter here when it's a non-fixed joint. Non-fixed joints have a other body.
                        if (other == null)
                        {
                            _island.Add(jointEdge.Joint);
                            jointEdge.Joint.IslandFlag = true;
                        }
                        else
                        {
                            if (!other.Enabled)
                                continue;

                            _island.Add(jointEdge.Joint);
                            jointEdge.Joint.IslandFlag = true;

                            if (other.Island)
                                continue;

                            DebugTools.Assert(stackCount < stackSize);

                            _stack[stackCount++] = other;

                            if (!islandSet.Contains(body))
                                islandSet.Add(body);

                            other.Island = true;
                        }
                    }
                }

                // wew this island is done
                _island.Solve(frameTime);

                // Let static bodies participate in as many islands as they want.
                for (var i = 0; i < _island.BodyCount; i++)
                {
                    var body = _island.Bodies[i];
                    if (body.BodyType == BodyType.Static)
                        body.Island = false;
                }
            }

            // TODO: Synchronize fixtures and check out of range bodies
            foreach (var body in islandSet)
            {
                // No island thus no move
                if (!body.Island)
                    continue;

                DebugTools.Assert(body.BodyType != BodyType.Static);

                // Update fixtures for broadphase
                body.SynchronizeFixtures();
            }

            // TODO: If optimise TOI

            _contactManager.FindNewContacts();
        }

        private void SolveTOI()
        {

        }
    }
}
