using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class ContactManager
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        private SharedBroadPhaseSystem _broadPhase = default!;

        // Large parts of this will be deprecated as more stuff gets ported.
        // For now it's more or less a straight port of the existing code.

        // For now we'll just clear contacts every tick.
        public List<Contact> ContactList = new(128);

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _broadPhase = EntitySystem.Get<SharedBroadPhaseSystem>();
        }

        // At some point the below will be changed to go through contact bodies instead.
        /// <summary>
        ///     Go through each awake body and find collisions.
        /// </summary>
        /// <param name="map"></param>
        public void Collide(PhysicsMap map, bool prediction, float frameTime)
        {
            var timeToSleep = _configManager.GetCVar(CVars.TimeToSleep);

            var combinations = new HashSet<(EntityUid, EntityUid)>();

            var bodies = map.AwakeBodies;

            foreach (var bodyA in bodies)
            {
                if (bodyA.BodyType == BodyType.Static)
                {
                    continue;
                }

                foreach (var bodyB in _broadPhase.GetCollidingEntities(bodyA, Vector2.Zero, false))
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
}
