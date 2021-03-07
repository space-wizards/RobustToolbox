// Copyright (c) 2017 Kastellanos Nikolaos

/* Original source Farseer Physics Engine:
 * Copyright (c) 2014 Ian Qvist, http://farseerphysics.codeplex.com
 * Microsoft Permissive License (Ms-PL) v1.1
 */

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
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision;
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


        public readonly ContactHead ContactList;
        public int ContactCount { get; private set; }
        internal readonly ContactHead ContactPoolList;

        // Didn't use the eventbus because muh allocs on something being run for every collision every frame.
        /// <summary>
        ///     Invoked whenever a KinematicController body collides. The first body is always guaranteed to be a KinematicController
        /// </summary>
        internal event Action<IPhysBody, IPhysBody, float, Manifold>? KinematicControllerCollision;

        // TODO: Need to migrate the interfaces to comp bus when possible
        // TODO: Also need to clean the station up to not have 160 contacts on roundstart
        // TODO: CollideMultiCore
        private List<Contact> _startCollisions = new();
        private List<Contact> _endCollisions = new();

        public ContactManager()
        {
            ContactList = new ContactHead();
            ContactCount = 0;
            ContactPoolList = new ContactHead();
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

            for (ContactEdge? ceB = bodyB.ContactEdges; ceB != null; ceB = ceB?.Next)
            {
                if (ceB.Other == bodyA)
                {
                    Fixture fA = ceB.Contact?.FixtureA!;
                    Fixture fB = ceB.Contact?.FixtureB!;
                    var iA = ceB.Contact!.ChildIndexA;
                    var iB = ceB.Contact!.ChildIndexB;

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

                ceB = ceB.Next;
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

            // Sloth: IDK why Farseer and Aether2D have this shit but fuck it.
            if (c == null) return;

            // Contact creation may swap fixtures.
            fixtureA = c.FixtureA!;
            fixtureB = c.FixtureB!;
            bodyA = fixtureA.Body;
            bodyB = fixtureB.Body;

            // Insert into world
            c.Prev = ContactList;
            c.Next = c.Prev.Next;
            c.Prev.Next = c;
            c.Next!.Prev = c;
            ContactCount++;

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

            // Remove from the world
            contact.Prev!.Next = contact.Next;
            contact.Next!.Prev = contact.Prev;
            contact.Next = null;
            contact.Prev = null;
            ContactCount--;

            // Remove from body 1
            if (contact.NodeA == bodyA.ContactEdges)
                bodyA.ContactEdges = contact.NodeA.Next;
            if (contact.NodeA.Previous != null)
                contact.NodeA.Previous.Next = contact.NodeA.Next;
            if (contact.NodeA.Next != null)
                contact.NodeA.Next.Previous = contact.NodeA.Previous;

            // Remove from body 2
            if (contact.NodeB == bodyB.ContactEdges)
                bodyB.ContactEdges = contact.NodeB.Next;
            if (contact.NodeB.Previous != null)
                contact.NodeB.Previous.Next = contact.NodeB.Next;
            if (contact.NodeB.Next != null)
                contact.NodeB.Next.Previous = contact.NodeB.Previous;

            contact.Destroy();

            // Insert into the pool.
            contact.Next = ContactPoolList.Next;
            ContactPoolList.Next = contact;
        }

        internal void Collide()
        {
            // Can be changed while enumerating
            // TODO: check for null instead?
            for (var contact = ContactList.Next; contact != ContactList;)
            {
                if (contact == null) break;
                Fixture fixtureA = contact.FixtureA!;
                Fixture fixtureB = contact.FixtureB!;
                int indexA = contact.ChildIndexA;
                int indexB = contact.ChildIndexB;

                PhysicsComponent bodyA = fixtureA.Body;
                PhysicsComponent bodyB = fixtureB.Body;

                //Do no try to collide disabled bodies
                if (!bodyA.CanCollide || !bodyB.CanCollide)
                {
                    contact = contact.Next;
                    continue;
                }

                // Is this contact flagged for filtering?
                if (contact.FilterFlag)
                {
                    // Should these bodies collide?
                    if (bodyB.ShouldCollide(bodyA) == false)
                    {
                        Contact cNuke = contact;
                        contact = contact.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check default filtering
                    if (ShouldCollide(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = contact;
                        contact = contact.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check user filtering.
                    /*
                    if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = c;
                        c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }
                    */

                    // Clear the filtering flag.
                    contact.FilterFlag = false;
                }

                bool activeA = bodyA.Awake && bodyA.BodyType != BodyType.Static;
                bool activeB = bodyB.Awake && bodyB.BodyType != BodyType.Static;

                // At least one body must be awake and it must be dynamic or kinematic.
                if (activeA == false && activeB == false)
                {
                    contact = contact.Next;
                    continue;
                }

                bool? overlap = null;

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
                    contact = contact.Next;
                    Destroy(cNuke);
                    continue;
                }

                // The contact persists.
                contact.Update(this, _startCollisions, _endCollisions);

                contact = contact.Next;
            }

            foreach (var contact in _startCollisions)
            {
                // It's possible for contacts to get nuked by other collision behaviors running on an entity deleting it
                // so we'll do this (TODO: Maybe it's shitty design and we should move to PostCollide? Though we still need to check for each contact anyway I guess).
                if (!contact.IsTouching) continue;

                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;

                foreach (var comp in bodyA.Entity.GetAllComponents<IStartCollide>().ToArray())
                {
                    if (bodyB.Deleted) break;
                    comp.CollideWith(bodyA, bodyB, contact.Manifold);
                }

                foreach (var comp in bodyB.Entity.GetAllComponents<IStartCollide>().ToArray())
                {
                    if (bodyA.Deleted) break;
                    comp.CollideWith(bodyB, bodyA, contact.Manifold);
                }
            }

            foreach (var contact in _endCollisions)
            {
                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;

                foreach (var comp in bodyA.Entity.GetAllComponents<IEndCollide>().ToArray())
                {
                    if (bodyB.Deleted) break;
                    comp.CollideWith(bodyA, bodyB, contact.Manifold);
                }

                foreach (var comp in bodyB.Entity.GetAllComponents<IEndCollide>().ToArray())
                {
                    if (bodyA.Deleted) break;
                    comp.CollideWith(bodyB, bodyA, contact.Manifold);
                }
            }

            _startCollisions.Clear();
            _endCollisions.Clear();
        }

        public void PreSolve(float frameTime)
        {
            // We'll do pre and post-solve around all islands rather than each specific island as it seems cleaner with race conditions.
            for (var contact = ContactList.Next; contact != ContactList; contact = contact?.Next)
            {
                if (contact == null || !contact.IsTouching || !contact.Enabled)
                {
                    continue;
                }

                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;

                // Didn't use an EntitySystemMessage as this is called FOR EVERY COLLISION AND IS REALLY EXPENSIVE
                // so we just use the Action. Also we'll sort out BodyA / BodyB for anyone listening first.
                if (bodyA.BodyType == BodyType.KinematicController)
                {
                    KinematicControllerCollision?.Invoke(bodyA, bodyB, frameTime, contact.Manifold);
                }
                else if (bodyB.BodyType == BodyType.KinematicController)
                {
                    KinematicControllerCollision?.Invoke(bodyB, bodyA, frameTime, contact.Manifold);
                }
            }
        }

        public void PostSolve()
        {

        }
    }

    public delegate void BroadPhaseDelegate(GridId gridId, in FixtureProxy proxyA, in FixtureProxy proxyB);
}
