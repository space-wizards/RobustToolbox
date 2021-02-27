/*
Microsoft Permissive License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license.
If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution,
prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or
derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by
including a complete copy of this license with your distribution.
If you distribute any portion of the software in compiled or object code form, you may only do so under a license that
complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
You may have additional consumer rights under your local laws which this license cannot change.
To the extent permitted under your local laws, the contributors exclude the implied warranties of
merchantability, fitness for a particular purpose and non-infringement.
*/

using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        // TODO: When a static body has no contacts left need to set it to sleep as otherwise it'll just show as awake
        // for debug drawing (map never adds static bodies as awake so should be no problem there).

        [Dependency] private readonly IConfigurationManager _configManager = default!;

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

        internal void RemoveActiveContact(Contact contact)
        {
            if (!ActiveContacts.Contains(contact))
            {
                ActiveContacts.Remove(contact);
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

            //Check default filter
            if (!ShouldCollide(fixtureA, fixtureB))
                return;

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

        public void PreSolve()
        {
            // We'll do pre and post-solve around all islands rather than each specific island as it seems cleaner with race conditions.
            foreach (var contact in ActiveContacts)
            {
                if (!contact.IsTouching) continue;

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
                        behavior.CollideWith(bodyA, bodyB);
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
                        behavior.CollideWith(bodyB, bodyA);
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
        }

        public void PostSolve()
        {

        }
    }

    public delegate void BroadPhaseDelegate(GridId gridId, in FixtureProxy proxyA, in FixtureProxy proxyB);
}
