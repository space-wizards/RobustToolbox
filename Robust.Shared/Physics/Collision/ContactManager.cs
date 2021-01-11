using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision
{
    internal sealed class ContactManager
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        // Large parts of this will be deprecated as more stuff gets ported.
        // For now it's more or less a straight port of the existing code.

        private List<Manifold> _collisionCache = new();

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
        }

        public void FindNewContacts(PhysicsMap map)
        {
            var bodies = map.AwakeBodies;

            _collisionCache.Clear();
            var combinations = new HashSet<(EntityUid, EntityUid)>();
            foreach (var bodyA in bodies)
            {
                foreach (var bodyB in _physicsManager.GetCollidingEntities(bodyA, Vector2.Zero, false))
                {
                    var aUid = bodyA.Entity.Uid;
                    var bUid = bodyB.Uid;

                    if (bUid.CompareTo(aUid) > 0)
                    {
                        var tmpUid = bUid;
                        bUid = aUid;
                        aUid = tmpUid;
                    }

                    if (!combinations.Add((aUid, bUid)))
                    {
                        continue;
                    }

                    var bPhysics = bodyB.GetComponent<IPhysicsComponent>();
                    _collisionCache.Add(new Manifold(bodyA, bPhysics, bPhysics.Hard && bPhysics.Hard));
                }
            }

            var counter = 0;

            if (_collisionCache.Count > 0)
            {
                while(GetNextCollision(_collisionCache, counter, out var collision))
                {
                    collision.A.WakeBody();
                    collision.B.WakeBody();

                    counter++;
                    var impulse = _physicsManager.SolveCollisionImpulse(collision);
                    if (collision.A.CanMove())
                    {
                        collision.A.ApplyImpulse(-impulse);
                    }

                    if (collision.B.CanMove())
                    {
                        collision.B.ApplyImpulse(impulse);
                    }
                }
            }

            var collisionsWith = new Dictionary<ICollideBehavior, int>();
            foreach (var collision in _collisionCache)
            {
                // Apply onCollide behavior
                foreach (var behavior in collision.A.Entity.GetAllComponents<ICollideBehavior>().ToArray())
                {
                    var entity = collision.B.Entity;
                    if (entity.Deleted) break;
                    behavior.CollideWith(entity);
                    if (collisionsWith.ContainsKey(behavior))
                    {
                        collisionsWith[behavior] += 1;
                    }
                    else
                    {
                        collisionsWith[behavior] = 1;
                    }
                }

                foreach (var behavior in collision.B.Entity.GetAllComponents<ICollideBehavior>().ToArray())
                {
                    var entity = collision.A.Entity;
                    if (entity.Deleted) break;
                    behavior.CollideWith(entity);
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

        private bool GetNextCollision(IReadOnlyList<Manifold> collisions, int counter, out Manifold collision)
        {
            // The *4 is completely arbitrary
            if (counter > collisions.Count * 4)
            {
                collision = default;
                return false;
            }

            var offset = _random.Next(collisions.Count - 1);
            for (var i = 0; i < collisions.Count; i++)
            {
                var index = (i + offset) % collisions.Count;
                if (collisions[index].Unresolved)
                {
                    collision = collisions[index];
                    return true;
                }

            }

            collision = default;
            return false;
        }
    }
}
