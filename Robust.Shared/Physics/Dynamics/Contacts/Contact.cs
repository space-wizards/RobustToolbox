using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class Contact
    {
        public ContactEdge NodeA = new();
        public ContactEdge NodeB = new();

        public Fixture? FixtureA;
        public Fixture? FixtureB;

        public AetherManifold Manifold = new();

        private ContactType _type;

        private static ContactType[,] _registers = {
                                                           {
                                                               ContactType.Circle,
                                                               ContactType.EdgeAndCircle,
                                                               ContactType.PolygonAndCircle,
                                                               ContactType.ChainAndCircle,
                                                           },
                                                           {
                                                               ContactType.EdgeAndCircle,
                                                               ContactType.NotSupported,
                                                               // 1,1 is invalid (no ContactType.Edge)
                                                               ContactType.EdgeAndPolygon,
                                                               ContactType.NotSupported,
                                                               // 1,3 is invalid (no ContactType.EdgeAndLoop)
                                                           },
                                                           {
                                                               ContactType.PolygonAndCircle,
                                                               ContactType.EdgeAndPolygon,
                                                               ContactType.Polygon,
                                                               ContactType.ChainAndPolygon,
                                                           },
                                                           {
                                                               ContactType.ChainAndCircle,
                                                               ContactType.NotSupported,
                                                               // 3,1 is invalid (no ContactType.EdgeAndLoop)
                                                               ContactType.ChainAndPolygon,
                                                               ContactType.NotSupported,
                                                               // 3,3 is invalid (no ContactType.Loop)
                                                           },
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

            AetherManifold oldManifold = Manifold;

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
                touching = IoCManager.Resolve<ICollisionManager>().TestOverlap(shapeA, ChildIndexA, shapeB, ChildIndexB, bodyA.GetTransform(), bodyB.GetTransform());

                // Sensors don't generate manifolds.
                Manifold.PointCount = 0;
            }
            else
            {
                Evaluate(ref Manifold, bodyA.GetTransform(), bodyB.GetTransform());
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

            /*
            if (contactManager.PreSolve != null)
                contactManager.PreSolve(this, ref oldManifold);
            */
        }

        /// <summary>
        /// Evaluate this contact with your own manifold and transforms.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="transformA">The first transform.</param>
        /// <param name="transformB">The second transform.</param>
        private void Evaluate(ref AetherManifold manifold, in Transform transformA, in Transform transformB)
        {
            var collisionManager = IoCManager.Resolve<ICollisionManager>();

            switch (_type)
            {
                case ContactType.Polygon:
                    collisionManager.CollidePolygons(ref manifold, new PolygonShape(FixtureA!.Shape), transformA, new PolygonShape(FixtureB!.Shape), transformB);
                    break;
                case ContactType.PolygonAndCircle:
                    collisionManager.CollidePolygonAndCircle(ref manifold, new PolygonShape(FixtureA!.Shape), transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.EdgeAndCircle:
                    collisionManager.CollideEdgeAndCircle(ref manifold, (EdgeShape) FixtureA!.Shape, transformA, (PhysShapeCircle) FixtureB!.Shape, transformB);
                    break;
                case ContactType.EdgeAndPolygon:
                    collisionManager.CollideEdgeAndPolygon(ref manifold, (EdgeShape) FixtureA!.Shape, transformA, (PolygonShape) FixtureB!.Shape, transformB);
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
                    collisionManager.CollideCircles(ref manifold, (PhysShapeCircle) FixtureA!.Shape, in transformA, (PhysShapeCircle) FixtureB!.Shape, in transformB);
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
        }
    }
}
