using System.Collections.Generic;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Robust.Shared.Physics
{
    internal interface IContactManager
    {
        HashSet<Contact> ActiveContacts { get; }

        void FindNewContacts();
    }

    internal sealed class ContactManager : EntitySystem
    {
        // The Karen of the physics system
        [Dependency] private readonly IBroadPhase _broadPhase = default!;

        internal PhysicsMapCallback.BroadphaseDelegate OnBroadphaseCollision = default!;

        public HashSet<Contact> ActiveContacts { get; } = new HashSet<Contact>();

        public readonly ContactListHead ContactList = new ContactListHead();

        public int ContactCount { get; private set; } = 0;
        internal readonly ContactListHead _contactPoolList = new ContactListHead();

        // TODO: OnBroadphaseCollision = AddPair

        // Broad-phase callback
        private void SS14AddPair(IPhysBody bodyA, IPhysBody bodyB)
        {
            var fixtureA = Bro
        }

        private void AddPair(int proxyIdA, int proxyIdB)
        {
            FixtureProxy proxyA = BroadPhase.GetProxy(proxyIdA);
            FixtureProxy proxyB = BroadPhase.GetProxy(proxyIdB);

            Fixture fixtureA = proxyA.Fixture;
            Fixture fixtureB = proxyB.Fixture;

            int indexA = proxyA.ChildIndex;
            int indexB = proxyB.ChildIndex;

            Body bodyA = fixtureA.Body;
            Body bodyB = fixtureB.Body;

            // Are the fixtures on the same body?
            if (bodyA == bodyB)
            {
                return;
            }

            // Does a contact already exist?
            for (ContactEdge ceB = bodyB.ContactList; ceB != null; ceB = ceB.Next)
            {
                if (ceB.Other == bodyA)
                {
                    Fixture fA = ceB.Contact.FixtureA;
                    Fixture fB = ceB.Contact.FixtureB;
                    int iA = ceB.Contact.ChildIndexA;
                    int iB = ceB.Contact.ChildIndexB;

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
            Contact c = Contact.Create(this, fixtureA, indexA, fixtureB, indexB);

            if (c == null)
                return;

            // Contact creation may swap fixtures.
            fixtureA = c.FixtureA;
            fixtureB = c.FixtureB;
            bodyA = fixtureA.Body;
            bodyB = fixtureB.Body;

            // Insert into the world.
            c.Prev = ContactList;
            c.Next = c.Prev.Next;
            c.Prev.Next = c;
            c.Next.Prev = c;
            ContactCount++;

#if USE_ACTIVE_CONTACT_SET
			ActiveContacts.Add(c);
#endif
            // Connect to island graph.

            // Connect to body A
            c._nodeA.Contact = c;
            c._nodeA.Other = bodyB;

            c._nodeA.Prev = null;
            c._nodeA.Next = bodyA.ContactList;
            if (bodyA.ContactList != null)
            {
                bodyA.ContactList.Prev = c._nodeA;
            }
            bodyA.ContactList = c._nodeA;

            // Connect to body B
            c._nodeB.Contact = c;
            c._nodeB.Other = bodyA;

            c._nodeB.Prev = null;
            c._nodeB.Next = bodyB.ContactList;
            if (bodyB.ContactList != null)
            {
                bodyB.ContactList.Prev = c._nodeB;
            }
            bodyB.ContactList = c._nodeB;

            // Wake up the bodies
            if (fixtureA.IsSensor == false && fixtureB.IsSensor == false)
            {
                bodyA.Awake = true;
                bodyB.Awake = true;
            }
        }

        internal void FindNewContacts()
        {
            _broadPhase.UpdatePairs(OnBroadphaseCollision);
        }
    }
}
