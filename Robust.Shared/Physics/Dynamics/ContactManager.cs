using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        internal MapId MapId { get; set; }

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
        private void AddPair(GridId gridId, in FixtureProxy proxyA, in FixtureProxy proxyB)
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
            if (!bodyB.ShouldCollide(bodyA))
                return;

            //Check default filter
            if (!ShouldCollide(fixtureA, fixtureB))
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
            // TODO: Should we only be checking one side's mask? I think maybe fixtureB? IDK
            return !((fixtureA.CollisionMask & fixtureB.CollisionLayer) == 0x0 &&
                     (fixtureB.CollisionMask & fixtureA.CollisionLayer) == 0x0);
        }

        private void Destroy(Contact contact)
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

            // Remove from the world.
            ContactList.Remove(contact);

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

            /*
#if USE_ACTIVE_CONTACT_SET
			if (ActiveContacts.Contains(contact))
			{
				ActiveContacts.Remove(contact);
			}
#endif
*/
            contact.Destroy();
        }

        internal void Collide()
        {
            // Can be changed while enumerating
            for (var i = 0; i < ContactList.Count; i++)
            {
                var contact = ContactList[i];
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
        }

        public void PreSolve()
        {
            // We'll do pre and post-solve around all islands rather than each specific island as it seems cleaner with race conditions.
            var collisionsWith = new Dictionary<ICollideBehavior, int>();

            foreach (var contact in ContactList)
            {
                var bodyA = contact.FixtureA!.Body.Owner;
                var bodyB = contact.FixtureB!.Body.Owner;

                // Apply onCollide behavior
                // TODO: CollideWith should be called with the body as a minor optimisation.
                // Also these ToArrays are hilariously expensive.
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

        }
    }

    public delegate void BroadPhaseDelegate(GridId gridId, in FixtureProxy proxyA, in FixtureProxy proxyB);
}
