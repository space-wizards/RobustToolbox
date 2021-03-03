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

using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        internal MapId MapId { get; set; }

        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        /// <summary>
        ///     Called when the broadphase finds two fixtures close to each other.
        /// </summary>
        public BroadPhaseDelegate OnBroadPhaseCollision;

        /// <summary>
        /// The set of active contacts.
        /// </summary>
        internal HashSet<Contact> ActiveContacts = new(128);

        /// <summary>
        /// A temporary copy of active contacts that is used during updates so
        /// the hash set can have members added/removed during the update.
        /// This list is cleared after every update.
        /// </summary>
        private List<Contact> ActiveList = new(128);

        private List<ICollideBehavior> _collisionBehaviors = new();
        private List<IPostCollide> _postCollideBehaviors = new();

        public ContactManager()
        {
            OnBroadPhaseCollision = AddPair;
        }

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
        }

        public void FindNewContacts(MapId mapId)
        {
            foreach (var broadPhase in _broadPhaseSystem.GetBroadPhases(mapId))
            {
                broadPhase.UpdatePairs(OnBroadPhaseCollision);
            }
        }

        internal void UpdateContacts(ContactEdge? contactEdge, bool value)
        {
            if (value)
            {
                while (contactEdge != null)
                {
                    var contact = contactEdge.Contact!;
                    if (!ActiveContacts.Contains(contact))
                    {
                        ActiveContacts.Add(contact);
                    }
                    contactEdge = contactEdge.Next;
                }
            }
            else
            {
                while (contactEdge != null)
                {
                    var contact = contactEdge.Contact!;

                    if (!contactEdge.Other!.Awake)
                    {
                        if (ActiveContacts.Contains(contact))
                        {
                            ActiveContacts.Remove(contact);
                        }
                    }

                    contactEdge = contactEdge.Next;
                }
            }
        }

        /// <summary>
        ///     Go through the cached broadphase movement and update contacts.
        /// </summary>
        /// <param name="gridId"></param>
        /// <param name="proxyA"></param>
        /// <param name="proxyB"></param>
        private void AddPair(GridId gridId, in FixtureProxy proxyA, in FixtureProxy proxyB)
        {
            Fixture fixtureA = proxyA.Fixture;
            Fixture fixtureB = proxyB.Fixture;

            int indexA = proxyA.ChildIndex;
            int indexB = proxyB.ChildIndex;

            PhysicsComponent bodyA = fixtureA.Body;
            PhysicsComponent bodyB = fixtureB.Body;

            // Are the fixtures on the same body?
            if (bodyA.Owner.Uid.Equals(bodyB.Owner.Uid)) return;

            // Box2D checks the mask / layer below but IMO doing it before contact is better.
            // Check default filter
            if (!ShouldCollide(fixtureA, fixtureB))
                return;

            // Does a contact already exist?
            var edge = bodyB.ContactEdges;

            while (edge != null)
            {
                if (edge.Other == bodyA)
                {
                    Fixture fA = edge.Contact?.FixtureA!;
                    Fixture fB = edge.Contact?.FixtureB!;
                    var iA = edge.Contact!.ChildIndexA;
                    var iB = edge.Contact!.ChildIndexB;

                    if (fA == fixtureA && fB == fixtureB && iA == indexA && iB == indexB)
                    {
                        // A contact already exists.
                        return;
                    }

                    if (fA == fixtureB && fB == fixtureA && iA == indexB && iB == indexA)
                    {
                        // A contact already exists.
                        return;
                    }
                }

                edge = edge.Next;
            }

            // Does a joint override collision? Is at least one body dynamic?
            if (!bodyB.ShouldCollide(bodyA))
                return;

            //FPE feature: BeforeCollision delegate
            /*
            if (fixtureA.BeforeCollision != null && fixtureA.BeforeCollision(fixtureA, fixtureB) == false)
                return;

            if (fixtureB.BeforeCollision != null && fixtureB.BeforeCollision(fixtureB, fixtureA) == false)
                return;
            */

            // Call the factory.
            Contact c = Contact.Create(gridId, fixtureA, indexA, fixtureB, indexB);

            // Contact creation may swap fixtures.
            fixtureA = c.FixtureA!;
            fixtureB = c.FixtureB!;
            bodyA = fixtureA.Body;
            bodyB = fixtureB.Body;

            // Insert into the world.
            ActiveContacts.Add(c);

            // Connect to island graph.

            // Connect to body A
            c.NodeA.Contact = c;
            c.NodeA.Other = bodyB;

            c.NodeA.Previous = null;
            c.NodeA.Next = bodyA.ContactEdges;

            if (bodyA.ContactEdges != null)
            {
                bodyA.ContactEdges.Previous = c.NodeA;
            }
            bodyA.ContactEdges = c.NodeA;

            // Connect to body B
            c.NodeB.Contact = c;
            c.NodeB.Other = bodyA;

            c.NodeB.Previous = null;
            c.NodeB.Next = bodyB.ContactEdges;

            if (bodyB.ContactEdges != null)
            {
                bodyB.ContactEdges.Previous = c.NodeB;
            }
            bodyB.ContactEdges = c.NodeB;

            // Wake up the bodies
            if (fixtureA.Hard && fixtureB.Hard)
            {
                bodyA.Awake = true;
                bodyB.Awake = true;
            }
        }

        private bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
        {
            // TODO: Should we only be checking one side's mask? I think maybe fixtureB? IDK
            return !((fixtureA.CollisionMask & fixtureB.CollisionLayer) == 0x0 &&
                     (fixtureB.CollisionMask & fixtureA.CollisionLayer) == 0x0);
        }

        public void Destroy(Contact contact)
        {
            Fixture fixtureA = contact.FixtureA!;
            Fixture fixtureB = contact.FixtureB!;
            PhysicsComponent bodyA = fixtureA.Body;
            PhysicsComponent bodyB = fixtureB.Body;

            if (contact.IsTouching)
            {
                //Report the separation to both participants:
                // TODO: Needs to do like a comp message and system message
                // fixtureA?.OnSeparation(fixtureA, fixtureB);

                //Reverse the order of the reported fixtures. The first fixture is always the one that the
                //user subscribed to.
                // fixtureB.OnSeparation(fixtureB, fixtureA);

                // EndContact(contact);
            }

            // Remove from body 1
            if (contact.NodeA.Previous != null)
            {
                contact.NodeA.Previous.Next = contact.NodeA.Next;
            }

            if (contact.NodeA.Next != null)
            {
                contact.NodeA.Next.Previous = contact.NodeA.Previous;
            }

            if (contact.NodeA == bodyA.ContactEdges)
            {
                bodyA.ContactEdges = contact.NodeA.Next;
            }

            // Remove from body 2
            if (contact.NodeB.Previous != null)
            {
                contact.NodeB.Previous.Next = contact.NodeB.Next;
            }

            if (contact.NodeB.Next != null)
            {
                contact.NodeB.Next.Previous = contact.NodeB.Previous;
            }

            if (contact.NodeB == bodyB.ContactEdges)
            {
                bodyB.ContactEdges = contact.NodeB.Next;
            }

            ActiveContacts.Remove(contact);

            contact.Destroy();
        }

        internal void Collide()
        {
            ActiveList.Clear();
            // TODO: We need to handle collisions during prediction but also handle the start / stop colliding shit during sim ONLY

            // Update awake contacts
            ActiveList.AddRange(ActiveContacts);

            // Can be changed while enumerating
            foreach (var contact in ActiveList)
            {
                Fixture fixtureA = contact.FixtureA!;
                Fixture fixtureB = contact.FixtureB!;

                int indexA = contact.ChildIndexA;
                int indexB = contact.ChildIndexB;
                PhysicsComponent bodyA = fixtureA.Body;
                PhysicsComponent bodyB = fixtureB.Body;

                // Do not try to collide disabled bodies
                // FPE just continues here but in our case I think it's better to also destroy the contact.
                if (!bodyA.CanCollide || !bodyB.CanCollide)
                {
                    Contact cNuke = contact;
                    Destroy(cNuke);
                    continue;
                }

                // Is this contact flagged for filtering?
                if (contact.FilterFlag)
                {
                    // Should these bodies collide?
                    if (!bodyB.ShouldCollide(bodyA))
                    {
                        Contact cNuke = contact;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check default filtering
                    if (!ShouldCollide(fixtureA, fixtureB))
                    {
                        Contact cNuke = contact;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check user filtering.
                    /*
                    if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = c;
                        Destroy(cNuke);
                        continue;
                    }
                    */

                    // Clear the filtering flag.
                    contact.FilterFlag = false;
                }

                var activeA = bodyA.Awake && bodyA.BodyType != BodyType.Static;
                var activeB = bodyB.Awake && bodyB.BodyType != BodyType.Static;

                // At least one body must be awake and it must be dynamic or kinematic.
                if (!activeA && !activeB)
                {
                    ActiveContacts.Remove(contact);
                    continue;
                }

                // TODO: Need to handle moving grids
                bool? overlap = false;

                // Sloth addition: Kind of hacky and might need to be removed at some point.
                // One of the bodies was probably put into nullspace so we need to remove I think.
                if (fixtureA.Proxies.ContainsKey(contact.GridId) && fixtureB.Proxies.ContainsKey(contact.GridId))
                {
                    var proxyIdA = fixtureA.Proxies[contact.GridId][indexA].ProxyId;
                    var proxyIdB = fixtureB.Proxies[contact.GridId][indexB].ProxyId;

                    var broadPhase = _broadPhaseSystem.GetBroadPhase(MapId, contact.GridId);

                    overlap = broadPhase?.TestOverlap(proxyIdA, proxyIdB);
                }

                // Here we destroy contacts that cease to overlap in the broad-phase.
                if (overlap == false)
                {
                    Contact cNuke = contact;
                    Destroy(cNuke);
                    continue;
                }

                // The contact persists.
                contact.Update(this);
            }

            ActiveList.Clear();
        }

        public void PreSolve(float frameTime)
        {
            // TODO: Optimise this coz it allocates a fuckton
            ActiveList.AddRange(ActiveContacts);

            // We'll do pre and post-solve around all islands rather than each specific island as it seems cleaner with race conditions.
            foreach (var contact in ActiveList)
            {
                if (!contact.IsTouching || !contact.Enabled) continue;

                // God this area's hard to read but tl;dr run ICollideBehavior and IPostCollide and try to optimise it a little.
                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;

                if (!bodyA.Entity.Deleted)
                {
                    foreach (var behavior in bodyA.Owner.GetAllComponents<ICollideBehavior>())
                    {
                        _collisionBehaviors.Add(behavior);
                    }

                    foreach (var behavior in _collisionBehaviors)
                    {
                        if (bodyB.Deleted) break;
                        behavior.CollideWith(bodyA, bodyB, frameTime, contact.Manifold);
                    }

                    _collisionBehaviors.Clear();
                }

                if (!bodyB.Entity.Deleted)
                {
                    foreach (var behavior in bodyB.Owner.GetAllComponents<ICollideBehavior>())
                    {
                        _collisionBehaviors.Add(behavior);
                    }

                    foreach (var behavior in _collisionBehaviors)
                    {
                        if (bodyA.Deleted) break;
                        behavior.CollideWith(bodyB, bodyA, frameTime, contact.Manifold);
                    }

                    _collisionBehaviors.Clear();
                }

                if (!bodyA.Entity.Deleted)
                {
                    foreach (var behavior in bodyA.Owner.GetAllComponents<IPostCollide>())
                    {
                        _postCollideBehaviors.Add(behavior);
                    }

                    foreach (var behavior in _postCollideBehaviors)
                    {
                        behavior.PostCollide(bodyA, bodyB);
                        if (bodyB.Deleted) break;
                    }

                    _postCollideBehaviors.Clear();
                }

                if (!bodyB.Entity.Deleted)
                {
                    foreach (var behavior in bodyB.Owner.GetAllComponents<IPostCollide>())
                    {
                        _postCollideBehaviors.Add(behavior);
                    }

                    foreach (var behavior in _postCollideBehaviors)
                    {
                        behavior.PostCollide(bodyB, bodyA);
                        if (bodyA.Deleted) break;
                    }

                    _postCollideBehaviors.Clear();
                }
            }

            ActiveList.Clear();
        }

        public void PostSolve()
        {

        }
    }

    public delegate void BroadPhaseDelegate(GridId gridId, in FixtureProxy proxyA, in FixtureProxy proxyB);
}
