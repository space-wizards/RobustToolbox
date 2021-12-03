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
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        internal MapId MapId { get; set; }

        public readonly ContactHead ContactList;
        public int ContactCount { get; private set; }
        private const int ContactPoolInitialSize = 64;

        internal Stack<Contact> ContactPoolList = new(ContactPoolInitialSize);

        // Didn't use the eventbus because muh allocs on something being run for every collision every frame.
        /// <summary>
        ///     Invoked whenever a KinematicController body collides. The first body is always guaranteed to be a KinematicController
        /// </summary>
        internal event Action<Fixture, Fixture, float, Vector2>? KinematicControllerCollision;

        private int _contactMultithreadThreshold;
        private int _contactMinimumThreads;

        // TODO: Also need to clean the station up to not have 160 contacts on roundstart

        public ContactManager()
        {
            ContactList = new ContactHead();
            ContactCount = 0;
        }

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.ContactMultithreadThreshold, OnContactMultithreadThreshold, true);
            configManager.OnValueChanged(CVars.ContactMinimumThreads, OnContactMinimumThreads, true);

            InitializePool();
        }

        public void Shutdown()
        {
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CVars.ContactMultithreadThreshold, OnContactMultithreadThreshold);
            configManager.UnsubValueChanged(CVars.ContactMinimumThreads, OnContactMinimumThreads);
        }

        private void OnContactMultithreadThreshold(int value)
        {
            _contactMultithreadThreshold = value;
        }

        private void OnContactMinimumThreads(int value)
        {
            _contactMinimumThreads = value;
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

            // Broadphase has already done the faster check for collision mask / layers
            // so no point duplicating

            // Does a contact already exist?
            var edge = bodyB.ContactEdges;

            while (edge != null)
            {
                if (edge.Other == bodyA)
                {
                    var fA = edge.Contact!.FixtureA;
                    var fB = edge.Contact!.FixtureB;
                    var iA = edge.Contact.ChildIndexA;
                    var iB = edge.Contact.ChildIndexB;

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
                _entityManager.EventBus.RaiseLocalEvent(bodyA.Owner, new EndCollideEvent(fixtureA, fixtureB));
                _entityManager.EventBus.RaiseLocalEvent(bodyB.Owner, new EndCollideEvent(fixtureB, fixtureA));
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
            // Due to the fact some contacts may be removed (and we need to update this array as we iterate).
            // the length may not match the actual contact count, hence we track the index.
            var contacts = ArrayPool<Contact>.Shared.Rent(ContactCount);
            var index = 0;

            // Can be changed while enumerating
            // TODO: check for null instead?
            // Work out which contacts are still valid before we decide to update manifolds.
            for (var contact = ContactList.Next; contact != ContactList;)
            {
                if (contact == null) break;
                Fixture fixtureA = contact.FixtureA!;
                Fixture fixtureB = contact.FixtureB!;
                int indexA = contact.ChildIndexA;
                int indexB = contact.ChildIndexB;

                PhysicsComponent bodyA = fixtureA.Body;
                PhysicsComponent bodyB = fixtureB.Body;

                // Do not try to collide disabled bodies
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
                var broadphaseA = bodyA.Broadphase;
                var broadphaseB = bodyB.Broadphase;

                // TODO: IT MIGHT BE THE FATAABB STUFF FOR MOVEPROXY SO TRY THAT
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
                        // These should really be destroyed before map changes.
                        DebugTools.Assert(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(broadphaseA.Owner).MapID == IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(broadphaseB.Owner).MapID);

                        var proxyAWorldAABB = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(broadphaseA.Owner).WorldMatrix.TransformBox(proxyA.AABB);
                        var proxyBWorldAABB = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(broadphaseB.Owner).WorldMatrix.TransformBox(proxyB.AABB);
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

                contacts[index++] = contact;
                contact = contact.Next;
            }

            var status = ArrayPool<ContactStatus>.Shared.Rent(index);

            // To avoid race conditions with the dictionary we'll cache all of the transforms up front.
            // Caching should provide better perf than multi-threading the GetTransform() as we can also re-use
            // these in PhysicsIsland as well.
            for (var i = 0; i < index; i++)
            {
                var contact = contacts[i];
                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;

                _physicsManager.EnsureTransform(bodyA.OwnerUid);
                _physicsManager.EnsureTransform(bodyB.OwnerUid);
            }

            // Update contacts all at once.
            BuildManifolds(contacts, index, status);

            // Single-threaded so content doesn't need to worry about race conditions.
            for (var i = 0; i < index; i++)
            {
                var contact = contacts[i];

                switch (status[i])
                {
                    case ContactStatus.StartTouching:
                    {
                        if (!contact.IsTouching) continue;

                        var fixtureA = contact.FixtureA!;
                        var fixtureB = contact.FixtureB!;
                        var bodyA = fixtureA.Body;
                        var bodyB = fixtureB.Body;
                        var worldPoint = Transform.Mul(_physicsManager.EnsureTransform(bodyA), contact.Manifold.LocalPoint);

                        _entityManager.EventBus.RaiseLocalEvent(bodyA.Owner, new StartCollideEvent(fixtureA, fixtureB, worldPoint));
                        _entityManager.EventBus.RaiseLocalEvent(bodyB.Owner, new StartCollideEvent(fixtureB, fixtureA, worldPoint));
                        break;
                    }
                    case ContactStatus.Touching:
                        break;
                    case ContactStatus.EndTouching:
                    {
                        var fixtureA = contact.FixtureA;
                        var fixtureB = contact.FixtureB;

                        // If something under StartCollideEvent potentially nukes other contacts (e.g. if the entity is deleted)
                        // then we'll just skip the EndCollide.
                        if (fixtureA == null || fixtureB == null) continue;

                        var bodyA = fixtureA.Body;
                        var bodyB = fixtureB.Body;

                        _entityManager.EventBus.RaiseLocalEvent(bodyA.Owner, new EndCollideEvent(fixtureA, fixtureB));
                        _entityManager.EventBus.RaiseLocalEvent(bodyB.Owner, new EndCollideEvent(fixtureB, fixtureA));
                        break;
                    }
                    case ContactStatus.NoContact:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ArrayPool<Contact>.Shared.Return(contacts);
            ArrayPool<ContactStatus>.Shared.Return(status);
        }

        private void BuildManifolds(Contact[] contacts, int count, ContactStatus[] status)
        {
            if (count > _contactMultithreadThreshold * _contactMinimumThreads)
            {
                var (batches, batchSize) = SharedPhysicsSystem.GetBatch(count, _contactMultithreadThreshold);

                Parallel.For(0, batches, i =>
                {
                    var start = i * batchSize;
                    var end = Math.Min(start + batchSize, count);
                    UpdateContacts(contacts, start, end, status);
                });

            }
            else
            {
                UpdateContacts(contacts, 0, count, status);
            }
        }

        private void UpdateContacts(Contact[] contacts, int start, int end, ContactStatus[] status)
        {
            for (var i = start; i < end; i++)
            {
                status[i] = contacts[i].Update(_physicsManager);
            }
        }

        public void PreSolve(float frameTime)
        {
            Span<Vector2> points = stackalloc Vector2[2];

            // We'll do pre and post-solve around all islands rather than each specific island as it seems cleaner with race conditions.
            for (var contact = ContactList.Next; contact != ContactList; contact = contact?.Next)
            {
                if (contact is not {IsTouching: true, Enabled: true})
                {
                    continue;
                }

                var bodyA = contact.FixtureA!.Body;
                var bodyB = contact.FixtureB!.Body;
                contact.GetWorldManifold(_physicsManager, out var worldNormal, points);

                // Didn't use an EntitySystemMessage as this is called FOR EVERY COLLISION AND IS REALLY EXPENSIVE
                // so we just use the Action. Also we'll sort out BodyA / BodyB for anyone listening first.
                if (bodyA.BodyType == BodyType.KinematicController)
                {
                    KinematicControllerCollision?.Invoke(contact.FixtureA!, contact.FixtureB!, frameTime, -worldNormal);
                }
                else if (bodyB.BodyType == BodyType.KinematicController)
                {
                    KinematicControllerCollision?.Invoke(contact.FixtureB!, contact.FixtureA!, frameTime, worldNormal);
                }
            }
        }

        public void PostSolve()
        {

        }
    }

    #region Collide Events Classes

    public abstract class CollideEvent : EntityEventArgs
    {
        public Fixture OurFixture { get; }
        public Fixture OtherFixture { get; }

        public CollideEvent(Fixture ourFixture, Fixture otherFixture)
        {
            OurFixture = ourFixture;
            OtherFixture = otherFixture;
        }
    }

    public sealed class StartCollideEvent : CollideEvent
    {
        public Vector2 WorldPoint;

        public StartCollideEvent(Fixture ourFixture, Fixture otherFixture, Vector2 worldPoint)
            : base(ourFixture, otherFixture)
        {
            WorldPoint = worldPoint;
        }
    }

    public sealed class EndCollideEvent : CollideEvent
    {
        public EndCollideEvent(Fixture ourFixture, Fixture otherFixture)
            : base(ourFixture, otherFixture)
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

    internal enum ContactStatus : byte
    {
        NoContact = 0,
        StartTouching = 1,
        Touching = 2,
        EndTouching = 3,
    }
}
