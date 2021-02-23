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
using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class Contact
    {
        [Dependency] private readonly ICollisionManager _collisionManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public ContactEdge NodeA = new();
        public ContactEdge NodeB = new();

        public Fixture? FixtureA;
        public Fixture? FixtureB;

        public Manifold Manifold = new();

        private ContactType _type;

        /// <summary>
        ///     Ordering is under <see cref="ShapeType"/>
        ///     uses enum to work out which collision evaluation to use.
        /// </summary>
        private static ContactType[,] _registers = {
                                                           {
                                                               // Circle register
                                                               ContactType.Circle,
                                                               ContactType.EdgeAndCircle,
                                                               ContactType.PolygonAndCircle,
                                                               ContactType.ChainAndCircle,
                                                               ContactType.AabbAndCircle,
                                                               ContactType.RectAndCircle,
                                                           },
                                                           {
                                                               // Edge register
                                                               ContactType.EdgeAndCircle,
                                                               ContactType.NotSupported, // Edge
                                                               ContactType.EdgeAndPolygon,
                                                               ContactType.NotSupported, // Chain
                                                               ContactType.NotSupported, // Aabb
                                                               ContactType.NotSupported, // Rect
                                                           },
                                                           {
                                                               // Polygon register
                                                               ContactType.PolygonAndCircle,
                                                               ContactType.EdgeAndPolygon,
                                                               ContactType.Polygon,
                                                               ContactType.ChainAndPolygon,
                                                               ContactType.AabbAndPolygon,
                                                               ContactType.RectAndPolygon,
                                                           },
                                                           {
                                                               // Chain register
                                                               ContactType.ChainAndCircle,
                                                               ContactType.NotSupported, // Edge
                                                               ContactType.ChainAndPolygon,
                                                               ContactType.NotSupported, // Chain
                                                               ContactType.NotSupported, // Aabb - TODO Just cast to poly
                                                               ContactType.NotSupported, // Rect - TODO Just cast to poly
                                                           },
                                                           {
                                                               // Aabb register
                                                               ContactType.AabbAndCircle,
                                                               ContactType.NotSupported, // Edge - TODO Just cast to poly
                                                               ContactType.AabbAndPolygon,
                                                               ContactType.NotSupported, // Chain - TODO Just cast to poly
                                                               ContactType.Aabb,
                                                               ContactType.AabbAndRect,
                                                           },
                                                           {
                                                               // Rectangle register
                                                               ContactType.RectAndCircle,
                                                               ContactType.NotSupported, // Edge - TODO Just cast to poly
                                                               ContactType.RectAndPolygon,
                                                               ContactType.NotSupported, // Chain - TODO Just cast to poly
                                                               ContactType.AabbAndRect,
                                                               ContactType.Rect,
                                                           }
                                                       };

        /// <summary>
        ///     Has this contact already been added to an island?
        /// </summary>
        public bool IslandFlag { get; set; }

        public bool FilterFlag { get; set; }

        /// <summary>
        ///     Determines whether the contact is touching.
        /// </summary>
        public bool IsTouching { get; set; }

        // Some day we'll refactor it to be more like EntityCoordinates
        public GridId GridId { get; set; } = GridId.Invalid;

        /// Enable/disable this contact. This can be used inside the pre-solve
        /// contact listener. The contact is only disabled for the current
        /// time step (or sub-step in continuous collisions).
        /// NOTE: If you are setting Enabled to a constant true or false,
        /// use the explicit Enable() or Disable() functions instead to
        /// save the CPU from doing a branch operation.
        public bool Enabled { get; set; }

        /// <summary>
        ///     Get the child primitive index for fixture A.
        /// </summary>
        public int ChildIndexA { get; internal set; }

        /// <summary>
        ///     Get the child primitive index for fixture B.
        /// </summary>
        public int ChildIndexB { get; internal set; }

        /// <summary>
        ///     The mixed friction of the 2 fixtures.
        /// </summary>
        public float Friction { get; set; }

        /// <summary>
        ///     The mixed restitution of the 2 fixtures.
        /// </summary>
        public float Restitution { get; set; }

        /// <summary>
        ///     Used for conveyor belt behavior in m/s.
        /// </summary>
        public float TangentSpeed { get; set; }

        public Contact(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            IoCManager.InjectDependencies(this);
            Reset(fixtureA, indexA, fixtureB, indexB);
        }

        /// <summary>
        ///     Gets a new contact to use, using the contact pool if relevant.
        /// </summary>
        /// <param name="fixtureA"></param>
        /// <param name="indexA"></param>
        /// <param name="fixtureB"></param>
        /// <param name="indexB"></param>
        /// <returns></returns>
        internal static Contact Create(GridId gridId, Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            var type1 = fixtureA.Shape.ShapeType;
            var type2 = fixtureB.Shape.ShapeType;

            DebugTools.Assert(ShapeType.Unknown < type1 && type1 < ShapeType.TypeCount);
            DebugTools.Assert(ShapeType.Unknown < type2 && type2 < ShapeType.TypeCount);

            Queue<Contact> pool = fixtureA.Body.PhysicsMap.ContactPool;
            if (pool.TryDequeue(out var contact))
            {
                if ((type1 >= type2 || (type1 == ShapeType.Edge && type2 == ShapeType.Polygon)) && !(type2 == ShapeType.Edge && type1 == ShapeType.Polygon))
                {
                    contact.Reset(fixtureA, indexA, fixtureB, indexB);
                }
                else
                {
                    contact.Reset(fixtureB, indexB, fixtureA, indexA);
                }
            }
            else
            {
                // Edge+Polygon is non-symmetrical due to the way Erin handles collision type registration.
                if ((type1 >= type2 || (type1 == ShapeType.Edge && type2 == ShapeType.Polygon)) && !(type2 == ShapeType.Edge && type1 == ShapeType.Polygon))
                {
                    contact = new Contact(fixtureA, indexA, fixtureB, indexB);
                }
                else
                {
                    contact = new Contact(fixtureB, indexB, fixtureA, indexA);
                }
            }

            contact.GridId = gridId;
            contact._type = _registers[(int) type1, (int) type2];

            return contact;
        }

        public void ResetRestitution()
        {
            Restitution = MathF.Max(FixtureA?.Restitution ?? 0.0f, FixtureB?.Restitution ?? 0.0f);
        }

        public void ResetFriction()
        {
            Friction = MathF.Sqrt(FixtureA?.Friction ?? 0.0f * FixtureB?.Friction ?? 0.0f);
        }

        private void Reset(Fixture? fixtureA, int indexA, Fixture? fixtureB, int indexB)
        {
            Enabled = true;
            IsTouching = false;
            IslandFlag = false;
            FilterFlag = false;
            // TOIFlag = false;

            FixtureA = fixtureA;
            FixtureB = fixtureB;

            ChildIndexA = indexA;
            ChildIndexB = indexB;

            Manifold.PointCount = 0;

            NodeA.Contact = null;
            NodeA.Previous = null;
            NodeA.Next = null;
            NodeA.Other = null;

            NodeB.Contact = null;
            NodeB.Previous = null;
            NodeB.Next = null;
            NodeB.Other = null;

            // _toiCount = 0;

            //FPE: We only set the friction and restitution if we are not destroying the contact
            if (FixtureA != null && FixtureB != null)
            {
                Friction = MathF.Sqrt(FixtureA.Friction * FixtureB.Friction);
                Restitution = MathF.Max(FixtureA.Restitution, FixtureB.Restitution);
            }

            TangentSpeed = 0;
        }

        /// <summary>
        /// Gets the world manifold.
        /// </summary>
        public void GetWorldManifold(out Vector2 normal, out Vector2[] points)
        {
            PhysicsComponent bodyA = FixtureA?.Body!;
            PhysicsComponent bodyB = FixtureB?.Body!;
            IPhysShape shapeA = FixtureA?.Shape!;
            IPhysShape shapeB = FixtureB?.Shape!;

            ContactSolver.InitializeManifold(Manifold, bodyA.GetTransform(), bodyB.GetTransform(), shapeA.Radius, shapeB.Radius, out normal, out points);
        }

        /// <summary>
        /// Update the contact manifold and touching status.
        /// Note: do not assume the fixture AABBs are overlapping or are valid.
        /// </summary>
        /// <param name="contactManager">The contact manager.</param>
        internal void Update(ContactManager contactManager)
        {
            PhysicsComponent bodyA = FixtureA!.Body;
            PhysicsComponent bodyB = FixtureB!.Body;

            if (FixtureA == null || FixtureB == null)
                return;

            Manifold oldManifold = Manifold.Clone();

            // Re-enable this contact.
            Enabled = true;

            bool touching;
            bool wasTouching = IsTouching;

            bool sensor = !FixtureA.Hard || !FixtureB.Hard;

            // Is this contact a sensor?
            if (sensor)
            {
                IPhysShape shapeA = FixtureA.Shape;
                IPhysShape shapeB = FixtureB.Shape;
                touching = _collisionManager.TestOverlap(shapeA, ChildIndexA, shapeB, ChildIndexB, bodyA.GetTransform(), bodyB.GetTransform());

                // Sensors don't generate manifolds.
                Manifold.PointCount = 0;
            }
            else
            {
                Evaluate(Manifold, bodyA.GetTransform(), bodyB.GetTransform());
                touching = Manifold.PointCount > 0;

                // Match old contact ids to new contact ids and copy the
                // stored impulses to warm start the solver.
                for (int i = 0; i < Manifold.PointCount; ++i)
                {
                    ManifoldPoint mp2 = Manifold.Points[i];
                    mp2.NormalImpulse = 0.0f;
                    mp2.TangentImpulse = 0.0f;
                    ContactID id2 = mp2.Id;

                    for (int j = 0; j < oldManifold.PointCount; ++j)
                    {
                        ManifoldPoint mp1 = oldManifold.Points[j];

                        if (mp1.Id.Key == id2.Key)
                        {
                            mp2.NormalImpulse = mp1.NormalImpulse;
                            mp2.TangentImpulse = mp1.TangentImpulse;
                            break;
                        }
                    }

                    Manifold.Points[i] = mp2;
                }

                if (touching != wasTouching)
                {
                    bodyA.Awake = true;
                    bodyB.Awake = true;
                }
            }

            IsTouching = touching;
            // TODO: Need to do collision behaviors around here.

            if (!wasTouching)
            {
                if (touching)
                {
                    var enabledA = true;
                    var enabledB = true;

                    /*
                    // Report the collision to both participants. Track which ones returned true so we can
                    // later call OnSeparation if the contact is disabled for a different reason.
                    if (FixtureA.OnCollision != null)
                        foreach (OnCollisionEventHandler handler in FixtureA.OnCollision.GetInvocationList())
                            enabledA = handler(FixtureA, FixtureB, this) && enabledA;

                    // Reverse the order of the reported fixtures. The first fixture is always the one that the
                    // user subscribed to.
                    if (FixtureB.OnCollision != null)
                        foreach (OnCollisionEventHandler handler in FixtureB.OnCollision.GetInvocationList())
                            enabledB = handler(FixtureB, FixtureA, this) && enabledB;
                    */

                    Enabled = enabledA && enabledB;

                    // BeginContact can also return false and disable the contact
                    /*
                    if (enabledA && enabledB && contactManager.BeginContact != null)
                        Enabled = contactManager.BeginContact(this);
                    */
                }
            }
            else
            {
                if (!touching)
                {
                    /*
                    //Report the separation to both participants:
                    if (FixtureA != null && FixtureA.OnSeparation != null)
                        FixtureA.OnSeparation(FixtureA, FixtureB);

                    //Reverse the order of the reported fixtures. The first fixture is always the one that the
                    //user subscribed to.
                    if (FixtureB != null && FixtureB.OnSeparation != null)
                        FixtureB.OnSeparation(FixtureB, FixtureA);

                    if (contactManager.EndContact != null)
                        contactManager.EndContact(this);
                    */
                }
            }

            if (sensor)
                return;

            _entityManager.EventBus.RaiseEvent(EventSource.Local, new PreSolveMessage(this, oldManifold));
        }

        /// <summary>
        ///     Evaluate this contact with your own manifold and transforms.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="transformA">The first transform.</param>
        /// <param name="transformB">The second transform.</param>
        private void Evaluate(Manifold manifold, in Transform transformA, in Transform transformB)
        {
            switch (_type)
            {
                // TODO: Need a unit test for these.
                case ContactType.Polygon:
                    _collisionManager.CollidePolygons(manifold, (PolygonShape) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
                    break;
                case ContactType.PolygonAndCircle:
                    _collisionManager.CollidePolygonAndCircle(manifold, (PolygonShape) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.EdgeAndCircle:
                    _collisionManager.CollideEdgeAndCircle(manifold, (EdgeShape) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.EdgeAndPolygon:
                    _collisionManager.CollideEdgeAndPolygon(manifold, (EdgeShape) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
                    break;
                case ContactType.ChainAndCircle:
                    throw new NotImplementedException();
                    /*
                    ChainShape chain = (ChainShape)FixtureA.Shape;
                    chain.GetChildEdge(_edge, ChildIndexA);
                    Collision.CollisionManager.CollideEdgeAndCircle(ref manifold, _edge, ref transformA, (CircleShape)FixtureB.Shape, ref transformB);
                    */
                    break;
                case ContactType.ChainAndPolygon:
                    throw new NotImplementedException();
                    /*
                    ChainShape loop2 = (ChainShape)FixtureA.Shape;
                    loop2.GetChildEdge(_edge, ChildIndexA);
                    Collision.CollisionManager.CollideEdgeAndPolygon(ref manifold, _edge, ref transformA, (PolygonShape)FixtureB.Shape, ref transformB);
                    */
                    break;
                case ContactType.Circle:
                    _collisionManager.CollideCircles(manifold, (PhysShapeCircle) FixtureA!.Shape, in transformA, (PhysShapeCircle) FixtureB!.Shape, in transformB);
                    break;
                // Custom ones
                // TODO: Need to remove the ctors and just use PhysShapeAABB + PhysShapeRect directly in their own functions (Acruid's didn't seem to fill out all of the Box2D data needed).
                // When I profiled 25% of 200ms ping was physics. 15% was UpdateEntityTree and the next highest was this at 1.50%
                // (After was the MoveEvent listeners which you can't really do much about besides caching better?)
                case ContactType.Aabb:
                    _collisionManager.CollideAabbs(manifold, (PhysShapeAabb) FixtureA!.Shape, transformA, (PhysShapeAabb) FixtureB!.Shape, transformB);
                    break;
                case ContactType.AabbAndCircle:
                    _collisionManager.CollideAabbAndCircle(manifold, (PhysShapeAabb) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.AabbAndPolygon:
                    _collisionManager.CollideAabbAndPolygon(manifold, (PhysShapeAabb) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
                    break;
                case ContactType.AabbAndRect:
                    _collisionManager.CollideAabbAndRect(manifold, (PhysShapeAabb) FixtureA!.Shape, transformA, (PhysShapeRect) FixtureB!.Shape, transformB);
                    break;
                case ContactType.Rect:
                    _collisionManager.CollideRects(manifold, (PhysShapeRect) FixtureA!.Shape, transformA, (PhysShapeRect) FixtureB!.Shape, transformB);
                    break;
                case ContactType.RectAndCircle:
                    _collisionManager.CollideRectAndCircle(manifold, (PhysShapeRect) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.RectAndPolygon:
                    _collisionManager.CollideRectAndPolygon(manifold, (PhysShapeRect) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
                    break;
            }
        }

        internal void Destroy()
        {
            // Seems like active contacts were never used in farseer anyway
            // FixtureA?.Body.PhysicsMap.ContactManager.RemoveActiveContact(this);
            FixtureA?.Body.PhysicsMap.ContactPool.Enqueue(this);

            if (Manifold.PointCount > 0 && FixtureA?.Hard == true && FixtureB?.Hard == true)
            {
                FixtureA.Body.Awake = true;
                FixtureB.Body.Awake = true;
            }

            Reset(null, 0, null, 0);
        }

        private enum ContactType : byte
        {
            NotSupported,
            Polygon,
            PolygonAndCircle,
            Circle,
            EdgeAndPolygon,
            EdgeAndCircle,
            ChainAndPolygon,
            ChainAndCircle,
            // Custom
            Aabb,
            AabbAndPolygon,
            AabbAndCircle,
            AabbAndRect,
            Rect,
            RectAndCircle,
            RectAndPolygon,
        }
    }
}
