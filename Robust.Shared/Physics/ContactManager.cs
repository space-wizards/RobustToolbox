using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Contacts;

namespace Robust.Shared.Physics
{
    // TODO
    internal interface IContactManager
    {
        void Initialize();
    }

    public sealed class ContactManager : IContactManager
    {
        // Stored per physicsmap (subject to change)

        #region Settings
        /// <summary>
        /// A threshold for activating multiple cores to solve VelocityConstraints.
        /// An Island with a contact count above this threshold will use multiple threads to solve VelocityConstraints.
        /// A value of 0 will always use multithreading. A value of (int.MaxValue) will never use multithreading.
        /// Typical values are {128 or 256}.
        /// </summary>
        public int VelocityConstraintsMultithreadThreshold = int.MaxValue;

        /// <summary>
        /// A threshold for activating multiple cores to solve PositionConstraints.
        /// An Island with a contact count above this threshold will use multiple threads to solve PositionConstraints.
        /// A value of 0 will always use multithreading. A value of (int.MaxValue) will never use multithreading.
        /// Typical values are {128 or 256}.
        /// </summary>
        public int PositionConstraintsMultithreadThreshold = int.MaxValue;

        /// <summary>
        /// A threshold for activating multiple cores to solve Collide.
        /// An World with a contact count above this threshold will use multiple threads to solve Collide.
        /// A value of 0 will always use multithreading. A value of (int.MaxValue) will never use multithreading.
        /// Typical values are {128 or 256}.
        /// </summary>
        public int CollideMultithreadThreshold = int.MaxValue;
        #endregion

        private Dictionary<GridId, ContactListHead> ContactList = default!;

        public int ContactCount { get; private set; }

        internal Dictionary<GridId, ContactListHead> _contactPoolList = default!;

        /// <summary>
        /// The set of active contacts.
        /// </summary>
		public Dictionary<GridId, HashSet<Contact>> ActiveContacts = new();

        /// <summary>
        /// A temporary copy of active contacts that is used during updates so
		/// the hash set can have members added/removed during the update.
		/// This list is cleared after every update.
        /// </summary>
		List<Contact> ActiveList = new();

        /// <summary>
        /// The filter used by the contact manager.
        /// </summary>
        public CollisionFilterDelegate? ContactFilter;

        /// <summary>
        /// Fires when a contact is created
        /// </summary>
        public BeginContactDelegate? BeginContact;

        /// <summary>
        /// Fires when a contact is deleted
        /// </summary>
        public EndContactDelegate? EndContact;

        /// <summary>
        /// Fires when the broadphase detects that two Fixtures are close to each other.
        /// </summary>
        public BroadphaseDelegate? OnBroadphaseCollision;

        /// <summary>
        /// Fires after the solver has run
        /// </summary>
        public PostSolveDelegate? PostSolve;

        /// <summary>
        /// Fires before the solver runs
        /// </summary>
        public PreSolveDelegate? PreSolve;

        internal SharedBroadPhaseSystem BroadPhase = default!;

        public void Initialize()
        {
            ContactList = new Dictionary<GridId, ContactListHead>();
            ContactCount = 0;
            _contactPoolList = new Dictionary<GridId, ContactListHead>();
            BroadPhase = EntitySystem.Get<SharedBroadPhaseSystem>();

            OnBroadphaseCollision = AddPair;
        }

        // Broad-phase callback.
        private void AddPair(GridId gridId, FixtureProxy proxyA, FixtureProxy proxyB)
        {
            Fixture? fixtureA = proxyA.Fixture;
            Fixture? fixtureB = proxyB.Fixture;

            int indexA = proxyA.ChildIndex;
            int indexB = proxyB.ChildIndex;

            PhysicsComponent? bodyA = fixtureA.Body;
            PhysicsComponent? bodyB = fixtureB.Body;

            // Are the fixtures on the same body?
            if (bodyA == bodyB)
            {
                return;
            }

            // Does a contact already exist?
            for (ContactEdge? ceB = bodyB.ContactList; ceB != null; ceB = ceB?.Next)
            {
                if (ceB.GridId != gridId) continue;

                if (ceB.Other == bodyA)
                {
                    Fixture? fA = ceB?.Contact?.FixtureA;
                    Fixture? fB = ceB?.Contact?.FixtureB;
                    int? iA = ceB?.Contact?.ChildIndexA;
                    int? iB = ceB?.Contact?.ChildIndexB;

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
            }

            // Does a joint override collision? Is at least one body dynamic?
            if (bodyB.ShouldCollide(bodyA) == false)
                return;

            //Check default filter
            if (ShouldCollide(fixtureA, fixtureB) == false)
                return;

            // Check user filtering.
            if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                return;

            //FPE feature: BeforeCollision delegate
            if (fixtureA.BeforeCollision != null && fixtureA.BeforeCollision(fixtureA, fixtureB) == false)
                return;

            if (fixtureB.BeforeCollision != null && fixtureB.BeforeCollision(fixtureB, fixtureA) == false)
                return;

            // Call the factory.
            Contact? c = Contact.Create(this, gridId, fixtureA, indexA, fixtureB, indexB);

            if (c == null)
                return;

            Debug.Assert(c.FixtureA != null & c.FixtureB != null);
            // Contact creation may swap fixtures.
            fixtureA = c.FixtureA;
            fixtureB = c.FixtureB;
            bodyA = fixtureA?.Body;
            bodyB = fixtureB?.Body;

            // TODO: What the fuck the c == null call is right up there
            Debug.Assert(c != null);
            // Insert into the world.

            if (!ContactList.TryGetValue(gridId, out var gridCon))
            {
                gridCon = new ContactListHead();
                ContactList[gridId] = gridCon;
            }

            c.Prev = gridCon;
            c.Next = c.Prev.Next;
            c.Prev.Next = c;

            if (c.Next != null)
                c.Next.Prev = c;

            ContactCount++;

            if (!ActiveContacts.TryGetValue(c.GridId, out var contacts))
            {
                contacts = new HashSet<Contact>();
                ActiveContacts[c.GridId] = contacts;
            }

			contacts.Add(c);
            // Connect to island graph.

            // Connect to body A
            c._nodeA.Contact = c;
            c._nodeA.Other = bodyB;

            c._nodeA.Prev = null;
            c._nodeA.Next = bodyA?.ContactList;
            if (bodyA?.ContactList != null)
            {
                bodyA.ContactList.Prev = c._nodeA;
            }

            if (bodyA != null)
                bodyA.ContactList = c._nodeA;

            // Connect to body B
            c._nodeB.Contact = c;
            c._nodeB.Other = bodyA;

            c._nodeB.Prev = null;
            c._nodeB.Next = bodyB?.ContactList;

            if (bodyB?.ContactList != null)
            {
                bodyB.ContactList.Prev = c._nodeB;
            }

            if (bodyB != null)
                bodyB.ContactList = c._nodeB;

            // Wake up the bodies
            if (bodyA != null && fixtureA?.IsSensor == false && bodyB != null && fixtureB?.IsSensor == false)
            {
                bodyA.Awake = true;
                bodyB.Awake = true;
            }
        }

        internal void FindNewContacts(MapId mapId)
        {
            if (OnBroadphaseCollision == null) return;
            BroadPhase.UpdatePairs(mapId, OnBroadphaseCollision);
        }

        internal void Destroy(Contact contact)
        {
            Fixture? fixtureA = contact.FixtureA;
            Fixture? fixtureB = contact.FixtureB;
            PhysicsComponent? bodyA = fixtureA?.Body;
            PhysicsComponent? bodyB = fixtureB?.Body;

            if (contact.IsTouching)
            {
                //Report the separation to both participants:
                if (fixtureA != null && fixtureA.OnSeparation != null)
                    fixtureA.OnSeparation(fixtureA, fixtureB, contact);

                //Reverse the order of the reported fixtures. The first fixture is always the one that the
                //user subscribed to.
                if (fixtureB != null && fixtureB.OnSeparation != null)
                    fixtureB.OnSeparation(fixtureB, fixtureA, contact);

                //Report the separation to both bodies:
                if (fixtureA != null && fixtureA.Body != null && fixtureA.Body.onSeparationEventHandler != null)
                    fixtureA.Body.onSeparationEventHandler(fixtureA, fixtureB, contact);

                //Reverse the order of the reported fixtures. The first fixture is always the one that the
                //user subscribed to.
                if (fixtureB != null && fixtureB.Body != null && fixtureB.Body.onSeparationEventHandler != null)
                    fixtureB.Body.onSeparationEventHandler(fixtureB, fixtureA, contact);

                if (EndContact != null)
                    EndContact(contact);
            }

            // Remove from the world.
            if (contact.Prev?.Next != null)
                contact.Prev.Next = contact.Next;

            if (contact.Next?.Prev != null)
                contact.Next.Prev = contact.Prev;

            contact.Next = null;
            contact.Prev = null;

            ContactCount--;

            // Remove from body 1
            if (contact._nodeA == bodyA?.ContactList)
                bodyA.ContactList = contact._nodeA.Next;
            if (contact._nodeA.Prev != null)
                contact._nodeA.Prev.Next = contact._nodeA.Next;
            if (contact._nodeA.Next != null)
                contact._nodeA.Next.Prev = contact._nodeA.Prev;

            // Remove from body 2
            if (contact._nodeB == bodyB?.ContactList)
                bodyB.ContactList = contact._nodeB.Next;
            if (contact._nodeB.Prev != null)
                contact._nodeB.Prev.Next = contact._nodeB.Next;
            if (contact._nodeB.Next != null)
                contact._nodeB.Next.Prev = contact._nodeB.Prev;

			if (ActiveContacts[contact.GridId].Contains(contact))
				ActiveContacts[contact.GridId].Remove(contact);

            contact.Destroy();

            // Insert into the pool.
            if (!_contactPoolList.TryGetValue(contact.GridId, out var poolCon))
            {
                poolCon = new ContactListHead();
                _contactPoolList[contact.GridId] = poolCon;
            }

            contact.Next = poolCon.Next;
            poolCon.Next = contact;
        }

        internal void Collide(GridId gridId)
        {
            /*
#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
            if (this.ContactCount > CollideMultithreadThreshold && System.Environment.ProcessorCount > 1)
            {
                CollideMultiCore();
                return;
            }
#endif
*/

            if (!ActiveContacts.TryGetValue(gridId, out var contacts))
                return;

            // Update awake contacts.
            ActiveList.AddRange(contacts);
            foreach (var tmpc in ActiveList)
            {
                Contact? c = tmpc;
                Debug.Assert(c.FixtureA != null && c.FixtureB != null);
                Fixture fixtureA = c.FixtureA;
                Fixture fixtureB = c.FixtureB;
                int indexA = c.ChildIndexA;
                int indexB = c.ChildIndexB;
                PhysicsComponent bodyA = fixtureA.Body;
                PhysicsComponent bodyB = fixtureB.Body;

                // Do no try to collide disabled bodies
                if (!bodyA.Enabled || !bodyB.Enabled)
                {
                    // TODO: Do we need this with USE_ACTIVE_CONTACT_SET? c = c.Next;
                    continue;
                }

                // Is this contact flagged for filtering?
                if (c.FilterFlag)
                {
                    // Should these bodies collide?
                    if (bodyB.ShouldCollide(bodyA) == false)
                    {
                        Contact cNuke = c;
                        // c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check default filtering
                    if (ShouldCollide(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = c;
                        // c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check user filtering.
                    if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = c;
                        // c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Clear the filtering flag.
                    c.FilterFlag = false;
                }

                bool activeA = bodyA.Awake && bodyA.BodyType != BodyType.Static;
                bool activeB = bodyB.Awake && bodyB.BodyType != BodyType.Static;

                // At least one body must be awake and it must be dynamic or kinematic.
                if (activeA == false && activeB == false)
                {
                    ActiveContacts[c.GridId].Remove(c);
                    // c = c.Next;
                    continue;
                }

                if (!fixtureA.Proxies.TryGetValue(gridId, out var proxiesA))
                {
                    return;
                }

                if (!fixtureB.Proxies.TryGetValue(gridId, out var proxiesB))
                {
                    return;
                }

                var proxyA = proxiesA[indexA];
                var proxyB = proxiesB[indexB];

                bool overlap = BroadPhase.TestOverlap(proxyA, proxyB);

                // Here we destroy contacts that cease to overlap in the broad-phase.
                if (!overlap)
                {
                    Contact cNuke = c;
                    Debug.Assert(c.Next != null);
                    //c = c.Next;
                    Destroy(cNuke);
                    continue;
                }

                // The contact persists.
                c.Update(this);

                Debug.Assert(c.Next != null);
                // c = c.Next;
            }

			ActiveList.Clear();
        }

        /// <summary>
        /// A temporary list of contacts to be updated during Collide().
        /// </summary>
        List<Contact> updateList = new List<Contact>();

        /*
#if NET40 || NET45 || NETSTANDARD2_0 || PORTABLE40 || PORTABLE45 || W10 || W8_1 || WP8_1
        internal void CollideMultiCore()
        {
            int lockOrder = 0;

            // Update awake contacts.
            ActiveList.AddRange(ActiveContacts);
            foreach (var tmpc in ActiveList)
            {
                Contact c = tmpc;
                Fixture fixtureA = c.FixtureA;
                Fixture fixtureB = c.FixtureB;
                int indexA = c.ChildIndexA;
                int indexB = c.ChildIndexB;
                Body bodyA = fixtureA.Body;
                Body bodyB = fixtureB.Body;

                //Do no try to collide disabled bodies
                if (!bodyA.Enabled || !bodyB.Enabled)
                {
                    c = c.Next;
                    continue;
                }

                // Is this contact flagged for filtering?
                if (c.FilterFlag)
                {
                    // Should these bodies collide?
                    if (bodyB.ShouldCollide(bodyA) == false)
                    {
                        Contact cNuke = c;
                        c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check default filtering
                    if (ShouldCollide(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = c;
                        c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Check user filtering.
                    if (ContactFilter != null && ContactFilter(fixtureA, fixtureB) == false)
                    {
                        Contact cNuke = c;
                        c = c.Next;
                        Destroy(cNuke);
                        continue;
                    }

                    // Clear the filtering flag.
                    c.FilterFlag = false;
                }

                bool activeA = bodyA.Awake && bodyA.BodyType != BodyType.Static;
                bool activeB = bodyB.Awake && bodyB.BodyType != BodyType.Static;

                // At least one body must be awake and it must be dynamic or kinematic.
                if (activeA == false && activeB == false)
                {
					ActiveContacts.Remove(c);
                    c = c.Next;
                    continue;
                }

                int proxyIdA = fixtureA.Proxies[indexA].ProxyId;
                int proxyIdB = fixtureB.Proxies[indexB].ProxyId;

                bool overlap = BroadPhase.TestOverlap(proxyIdA, proxyIdB);

                // Here we destroy contacts that cease to overlap in the broad-phase.
                if (overlap == false)
                {
                    Contact cNuke = c;
                    c = c.Next;
                    Destroy(cNuke);
                    continue;
                }

                // The contact persists.
                updateList.Add(c);
                // Assign a unique id for lock order
                bodyA._lockOrder = lockOrder++;
                bodyB._lockOrder = lockOrder++;


                c = c.Next;
            }

			ActiveList.Clear();

            // update contacts
            System.Threading.Tasks.Parallel.ForEach<Contact>(updateList, (c) =>
            {
                // find lower order item
                Fixture fixtureA = c.FixtureA;
                Fixture fixtureB = c.FixtureB;

                // find lower order item
                Body orderedBodyA = fixtureA.Body;
                Body orderedBodyB = fixtureB.Body;
                int idA = orderedBodyA._lockOrder;
                int idB = orderedBodyB._lockOrder;
                if (idA == idB)
                    throw new System.Exception();

                if (idA > idB)
                {
                    orderedBodyA = fixtureB.Body;
                    orderedBodyB = fixtureA.Body;
                }

                // obtain lock
                for (; ; )
                {
                    if (System.Threading.Interlocked.CompareExchange(ref orderedBodyA._lock, 1, 0) == 0)
                    {
                        if (System.Threading.Interlocked.CompareExchange(ref orderedBodyB._lock, 1, 0) == 0)
                            break;
                        System.Threading.Interlocked.Exchange(ref orderedBodyA._lock, 0);
                    }
#if NET40 || NET45 || NETSTANDARD2_0
                    System.Threading.Thread.Sleep(0);
#endif
                }

                c.Update(this);

                System.Threading.Interlocked.Exchange(ref orderedBodyB._lock, 0);
                System.Threading.Interlocked.Exchange(ref orderedBodyA._lock, 0);
            });

            updateList.Clear();
        }
#endif
*/

        private static bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
        {
            return (fixtureB.CollisionMask & fixtureA.CollisionLayer) != 0 ||
                   (fixtureA.CollisionMask & fixtureB.CollisionLayer) != 0;
        }

        internal void UpdateActiveContacts(ContactEdge ContactList, bool value)
        {
            if (value)
            {
                for (var contactEdge = ContactList; contactEdge != null; contactEdge = contactEdge.Next)
                {
                    if (contactEdge.Contact != null && !ActiveContacts[contactEdge.Contact.GridId].Contains(contactEdge.Contact))
                        ActiveContacts[contactEdge.Contact.GridId].Add(contactEdge.Contact);
                }
            }
            else
            {
                for (var contactEdge = ContactList; contactEdge != null; contactEdge = contactEdge.Next)
                {
                    if (contactEdge.Contact != null && contactEdge.Other?.Awake == false)
                    {
                        if (ActiveContacts[contactEdge.Contact.GridId].Contains(contactEdge.Contact))
                            ActiveContacts[contactEdge.Contact.GridId].Remove(contactEdge.Contact);
                    }
                }
            }
        }
    }
}
