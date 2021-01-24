using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        /// <summary>
        ///     Called when the broadphase finds two fixtures close to each other.
        /// </summary>
        public BroadPhaseDelegate OnBroadPhaseCollision;

        // Large parts of this will be deprecated as more stuff gets ported.
        // For now it's more or less a straight port of the existing code.

        // For now we'll just clear contacts every tick.
        public List<Contact> ContactList = new(128);

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

        /// <summary>
        ///     Go through the cached broadphase movement and update contacts.
        /// </summary>
        /// <param name="proxyA"></param>
        /// <param name="proxyB"></param>
        private void AddPair(in FixtureProxy proxyA, in FixtureProxy proxyB)
        {
            Fixture fixtureA = proxyA.Fixture;
            Fixture fixtureB = proxyB.Fixture;

            int indexA = proxyA.ChildIndex;
            int indexB = proxyB.ChildIndex;

            PhysicsComponent bodyA = fixtureA.Body;
            PhysicsComponent bodyB = fixtureB.Body;

            // Are the fixtures on the same body?
            if (bodyA == bodyB) return;

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
            if (bodyB.ShouldCollide(bodyA) == false)
                return;

            //Check default filter
            if (ShouldCollide(fixtureA, fixtureB) == false)
                return;

            //FPE feature: BeforeCollision delegate
            /*
            if (fixtureA.BeforeCollision != null && fixtureA.BeforeCollision(fixtureA, fixtureB) == false)
                return;

            if (fixtureB.BeforeCollision != null && fixtureB.BeforeCollision(fixtureB, fixtureA) == false)
                return;
            */

            // Call the factory.
            Contact c = Contact.Create(fixtureA, indexA, fixtureB, indexB);

            //if (c == null)
            //    return;

            // Contact creation may swap fixtures.
            fixtureA = c.FixtureA!;
            fixtureB = c.FixtureB!;
            bodyA = fixtureA.Body;
            bodyB = fixtureB.Body;

            // Insert into the world.
            ContactList.Add(c);

			// ActiveContacts.Add(c);

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
            return (fixtureA.CollisionMask & fixtureB.CollisionLayer) == 0x0;
        }

        /// <summary>
        ///     Go through each awake body and find collisions.
        /// </summary>
        /// <param name="map"></param>
        public void Collide(PhysicsMap map)
        {
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            var bodies = map.AwakeBodies;

            foreach (var bodyA in bodies)
            {
                if (bodyA.BodyType == BodyType.Static)
                {
                    continue;
                }

                foreach (var bodyB in _broadPhaseSystem.GetCollidingEntities(bodyA, Vector2.Zero, false))
                {
                    var aUid = bodyA.Entity.Uid;
                    var bUid = bodyB.Owner.Uid;

                    if (bUid.CompareTo(aUid) > 0)
                    {
                        var tmpUid = bUid;
                        bUid = aUid;
                        aUid = tmpUid;
                    }

                    if (!combinations.Add((aUid, bUid))) continue;

                    // TODO: Do we need to add one to each? eh!
                    var contact =
                        new Contact(
                        new Manifold(bodyA, bodyB, bodyA.Hard && bodyB.Hard));

                    bodyA.ContactEdges.Add(new ContactEdge(contact));

                    ContactList.Add(contact);
                }
            }
        }

        public void PreSolve()
        {
            // We'll do pre and post-solve around all islands rather than each specific island as it seems cleaner with race conditions.
            var collisionsWith = new Dictionary<ICollideBehavior, int>();

            foreach (var contact in ContactList)
            {
                var bodyA = contact.Manifold.A.Owner;
                var bodyB = contact.Manifold.B.Owner;

                // Apply onCollide behavior
                foreach (var behavior in bodyA.GetAllComponents<ICollideBehavior>().ToArray())
                {
                    if (bodyB.Deleted) break;
                    behavior.CollideWith(bodyB);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }

                foreach (var behavior in bodyB.GetAllComponents<ICollideBehavior>().ToArray())
                {
                    if (bodyA.Deleted) break;
                    behavior.CollideWith(bodyA);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }
            }

            foreach (var behavior in collisionsWith.Keys)
            {
                behavior.PostCollide(collisionsWith[behavior]);
            }
        }

        public void PostSolve()
        {
            // As above this is temporary as we don't retain contacts over ticks (out of scope HARD).
            foreach (var contact in ContactList)
            {
                var bodyA = contact.Manifold.A;
                var bodyB = contact.Manifold.B;

                if (!bodyA.Deleted)
                {
                    bodyA.ContactEdges.Clear();
                }

                if (bodyB.Deleted)
                {
                    bodyB.ContactEdges.Clear();
                }
            }

            ContactList.Clear();
        }
    }

    public delegate void BroadPhaseDelegate(in FixtureProxy proxyA, in FixtureProxy proxyB);
}
