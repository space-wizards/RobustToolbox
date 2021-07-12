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
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        internal MapId MapId { get; set; }

        private SharedBroadphaseSystem _broadPhaseSystem = default!;

        public readonly ContactHead ContactList;
        public int ContactCount { get; private set; }
        private const int ContactPoolInitialSize = 64;

        internal Stack<Contact> ContactPoolList = new(ContactPoolInitialSize);

        // Didn't use the eventbus because muh allocs on something being run for every collision every frame.
        /// <summary>
        ///     Invoked whenever a KinematicController body collides. The first body is always guaranteed to be a KinematicController
        /// </summary>
        internal event Action<Fixture, Fixture, float, Manifold>? KinematicControllerCollision;

        // TODO: Need to migrate the interfaces to comp bus when possible
        // TODO: Also need to clean the station up to not have 160 contacts on roundstart
        // TODO: CollideMultiCore
        private List<Contact> _startCollisions = new();
        private List<Contact> _endCollisions = new();

        public ContactManager()
        {
            ContactList = new ContactHead();
            ContactCount = 0;
        }

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _broadPhaseSystem = EntitySystem.Get<SharedBroadphaseSystem>();
            InitializePool();
        }

        private void InitializePool()
        {
            for (var i = 0; i < ContactPoolInitialSize; i++)
            {
                ContactPoolList.Push(new Contact(null, 0, null, 0));
            }
        }

        /// <summary>
        ///     Go through the cached broadphase movement and update contacts.
        /// </summary>
        internal void AddPair(in FixtureProxy proxyA, in FixtureProxy proxyB)
        {
            Fixture fixtureA = proxyA.Fixture;
            Fixture fixtureB = proxyB.Fixture;

            var indexA = proxyA.ChildIndex;
            var indexB = proxyB.ChildIndex;

            PhysicsComponent bodyA = fixtureA.Body;
            PhysicsComponent bodyB = fixtureB.Body;

            // Are the fixtures on the same body?
            if (bodyA.Owner.Uid.Equals(bodyB.Owner.Uid)) return;

            // Broadphase has already done the faster check for collision mask / layers
            // so no point duplicating

            // Does a contact already exist?
            for (var ceB = bodyB.ContactEdges; ceB != null; ceB = ceB?.Next)
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
            Contact c = Contact.Create(this, fixtureA, indexA, fixtureB, indexB);

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

        internal static bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
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
            ContactPoolList.Push(contact);
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

                var proxyA = fixtureA.Proxies[indexA];
                var proxyB = fixtureB.Proxies[indexB];
                var broadphaseA = fixtureA.Body.Broadphase;
                var broadphaseB = fixtureB.Body.Broadphase;

                var overlap = false;

                // We can have cross-broadphase proxies hence need to change them to worldspace
                if (broadphaseA != null && broadphaseB != null)
                {
                    if (broadphaseA == broadphaseB)
                    {
                        overlap = proxyA.AABB.Intersects(proxyB.AABB);
                    }
                    else
                    {
                        var proxyAWorldAABB = proxyA.AABB.Translated(fixtureA.Body.Broadphase!.Owner.Transform.WorldPosition);
                        var proxyBWorldAABB = proxyB.AABB.Translated(fixtureB.Body.Broadphase!.Owner.Transform.WorldPosition);
                        overlap = proxyAWorldAABB.Intersects(proxyBWorldAABB);
                    }
                }

                // Here we destroy contacts that cease to overlap in the broad-phase.
                if (!overlap)
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

                var fixtureA = contact.FixtureA!;
                var fixtureB = contact.FixtureB!;
                var bodyA = fixtureA.Body;
                var bodyB = fixtureB.Body;
                var manifold = contact.Manifold;

                _entityManager.EventBus.RaiseLocalEvent(bodyA.Owner.Uid, new StartCollideEvent(fixtureA, fixtureB, manifold));
                _entityManager.EventBus.RaiseLocalEvent(bodyB.Owner.Uid, new StartCollideEvent(fixtureB, fixtureA, manifold));

#pragma warning disable 618
                foreach (var comp in bodyA.Owner.GetAllComponents<IStartCollide>().ToArray())
                {
                    if (bodyB.Deleted) break;
                    comp.CollideWith(fixtureA, fixtureB, contact.Manifold);
                }

                foreach (var comp in bodyB.Owner.GetAllComponents<IStartCollide>().ToArray())
                {
                    if (bodyA.Deleted) break;
                    comp.CollideWith(fixtureB, fixtureA, contact.Manifold);
                }
#pragma warning restore 618
            }

            foreach (var contact in _endCollisions)
            {
                var fixtureA = contact.FixtureA!;
                var fixtureB = contact.FixtureB!;
                var bodyA = fixtureA.Body;
                var bodyB = fixtureB.Body;
                var manifold = contact.Manifold;

                _entityManager.EventBus.RaiseLocalEvent(bodyA.Owner.Uid, new EndCollideEvent(fixtureA, fixtureB, manifold));
                _entityManager.EventBus.RaiseLocalEvent(bodyB.Owner.Uid, new EndCollideEvent(fixtureB, fixtureA, manifold));

#pragma warning disable 618
                foreach (var comp in bodyA.Owner.GetAllComponents<IEndCollide>().ToArray())
                {
                    if (bodyB.Deleted) break;
                    comp.CollideWith(fixtureA, fixtureB, manifold);
                }

                foreach (var comp in bodyB.Owner.GetAllComponents<IEndCollide>().ToArray())
                {
                    if (bodyA.Deleted) break;
                    comp.CollideWith(fixtureB, fixtureA, manifold);
                }
#pragma warning restore 618
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
                    KinematicControllerCollision?.Invoke(contact.FixtureA!, contact.FixtureB!, frameTime, contact.Manifold);
                }
                else if (bodyB.BodyType == BodyType.KinematicController)
                {
                    KinematicControllerCollision?.Invoke(contact.FixtureB!, contact.FixtureA!, frameTime, contact.Manifold);
                }
            }
        }

        public void PostSolve()
        {

        }
    }

    public delegate void BroadPhaseDelegate(in FixtureProxy proxyA, in FixtureProxy proxyB);

    #region Collide Events Classes

    public abstract class CollideEvent : EntityEventArgs
    {
        public Fixture OurFixture { get; }
        public Fixture OtherFixture { get; }
        public Manifold Manifold { get; }

        public CollideEvent(Fixture ourFixture, Fixture otherFixture, Manifold manifold)
        {
            OurFixture = ourFixture;
            OtherFixture = otherFixture;
            Manifold = manifold;
        }
    }

    public sealed class StartCollideEvent : CollideEvent
    {
        public StartCollideEvent(Fixture ourFixture, Fixture otherFixture, Manifold manifold)
            : base(ourFixture, otherFixture, manifold)
        {
        }
    }

    public sealed class EndCollideEvent : CollideEvent
    {
        public EndCollideEvent(Fixture ourFixture, Fixture otherFixture, Manifold manifold)
            : base(ourFixture, otherFixture, manifold)
        {
        }
    }

    public sealed class PreventCollideEvent : CancellableEntityEventArgs
    {
        public IPhysBody BodyA;
        public IPhysBody BodyB;

        public PreventCollideEvent(IPhysBody ourBody, IPhysBody otherBody)
        {
            BodyA = ourBody;
            BodyB = otherBody;
        }
    }

    #endregion
}
