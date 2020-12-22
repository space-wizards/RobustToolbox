using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Solver;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Represents a contact between 2 shapes. Even if this exists it doesn't mean that their is an overlap as
    ///     it only represents the AABB overlapping.
    /// </summary>
    public class Contact
    {
        private ContactType _type;

        private static EdgeShape _edge = new EdgeShape();

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

        public ContactEdge _nodeA = new ContactEdge();
        public ContactEdge _nodeB = new ContactEdge();

        /// <summary>
        ///     TimeOfImpact count
        /// </summary>
        public int _toiCount;

        /// <summary>
        ///     TimeOfImpact TODO: Rename
        /// </summary>
        public float _toi;

        public Fixture? FixtureA { get; set; }
        public Fixture? FixtureB { get; set; }

        /// <summary>
        ///     Friction of the contact.
        /// </summary>
        public float Friction { get; set; }

        /// <summary>
        ///     Restitution (AKA bounciness) of the contact.
        /// </summary>
        public float Restitution { get; set; }

        public Manifold Manifold { get; set; }

        /// <summary>
        ///     Get / set desired tangent speed for conveyor belt behavior in m/s.
        /// </summary>
        public float TangentSpeed { get; set; }

        // TODO: That long-ass comment
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Get the child primitive index for FixtureA.
        /// </summary>
        public int ChildIndexA { get; set; }

        /// <summary>
        ///     Get the child primitive index for FixtureB.
        /// </summary>
        public int ChildIndexB { get; set; }

        /// <summary>
        ///     Get the next contact in the map's contact list.
        /// </summary>
        public Contact? Next { get; set; }

        /// <summary>
        ///     Get the previous contact in the map's contact list.
        /// </summary>
        public Contact? Prev { get; set; }

        /// <summary>
        ///     Is this Contact currently touching.
        /// </summary>
        public bool IsTouching { get; set; }

        /// <summary>
        ///     Has this contact already been added to a physics island?
        /// </summary>
        public bool IslandFlag { get; set; }

        public bool TOIFlag { get; set; }

        public bool FilterFlag { get; set; }

        public void ResetRestitution()
        {
            Debug.Assert(FixtureA != null && FixtureB != null);
            Restitution = PhysicsSettings.MixRestitution(FixtureA.Restitution, FixtureB.Restitution);
        }

        public void ResetFriction()
        {
            Debug.Assert(FixtureA != null && FixtureB != null);
            Friction = PhysicsSettings.MixFriction(FixtureA.Friction, FixtureB.Friction);
        }

        protected Contact(Fixture? fixtureA, int indexA, Fixture? fixtureB, int indexB)
        {
            Reset(fixtureA, indexA, fixtureB, indexB);
        }

        // TODO: Probs delete coz never used
        public void GetMapManifold(out Vector2 normal, out Vector2[] points)
        {
            Debug.Assert(FixtureA != null && FixtureB != null);
            PhysicsComponent bodyA = FixtureA.Body;
            PhysicsComponent bodyB = FixtureB.Body;
            Shape shapeA = FixtureA.Shape;
            Shape shapeB = FixtureB.Shape;

            var aTransform = bodyA.GetTransform();
            var bTransform = bodyB.GetTransform();
            ContactSolver.WorldManifold.Initialize(Manifold, ref aTransform, shapeA.Radius, ref bTransform, shapeB.Radius, out normal, out points);
        }

        private void Reset(Fixture? fixtureA, int indexA, Fixture? fixtureB, int indexB)
        {
            Enabled = true;
            IsTouching = false;
            IslandFlag = false;
            FilterFlag = false;
            TOIFlag = false;

            FixtureA = fixtureA;
            FixtureB = fixtureB;

            ChildIndexA = indexA;
            ChildIndexB = indexB;

            Manifold = new Manifold
            {
                Points = Manifold.Points,
                Type = Manifold.Type,
                LocalNormal = Manifold.LocalNormal,
                LocalPoint = Manifold.LocalPoint,
                PointCount = 0,
            };

            Next = null;
            Prev = null;

            _nodeA.Contact = null;
            _nodeA.Other = null;
            _nodeA.Next = null;
            _nodeA.Prev = null;

            _nodeB.Contact = null;
            _nodeB.Other = null;
            _nodeB.Next = null;
            _nodeB.Prev = null;

            _toiCount = 0;

            //FPE: We only set the friction and restitution if we are not destroying the contact
            if (FixtureA != null && FixtureB != null)
            {
                Friction = PhysicsSettings.MixFriction(FixtureA.Friction, FixtureB.Friction);
                Restitution = PhysicsSettings.MixRestitution(FixtureA.Restitution, FixtureB.Restitution);
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
            Debug.Assert(FixtureA != null && FixtureB != null);
            PhysicsComponent bodyA = FixtureA.Body;
            PhysicsComponent bodyB = FixtureB.Body;

            if (FixtureA == null || FixtureB == null)
                return;

            Manifold oldManifold = Manifold;

            // Re-enable this contact.
            Enabled = true;

            bool touching;
            bool wasTouching = IsTouching;

            bool sensor = FixtureA.IsSensor || FixtureB.IsSensor;

            // Is this contact a sensor?
            if (sensor)
            {
                Shape shapeA = FixtureA.Shape;
                Shape shapeB = FixtureB.Shape;
                var transformA = bodyA.GetTransform();
                var transformB = bodyB.GetTransform();
                touching = Shapes.Collision.TestOverlap(shapeA, ChildIndexA, shapeB, ChildIndexB, ref transformA, ref transformB);

                // Sensors don't generate manifolds.
                var manifold = Manifold;
                manifold.PointCount = 0;
                Manifold = manifold;
            }
            else
            {
                var aTransform = bodyA.GetTransform();
                var bTransform = bodyB.GetTransform();

                Evaluate(Manifold, ref aTransform, ref bTransform);
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

                    // TODO: Need to suss this struct bullshit out
                    var manifold = Manifold;
                    manifold.Points[i] = mp2;
                    Manifold = manifold;
                }

                if (touching != wasTouching)
                {
                    bodyA.Awake = true;
                    bodyB.Awake = true;
                }
            }

            IsTouching = touching;

            if (wasTouching == false)
            {
                if (touching)
                {
                    bool enabledA = true, enabledB = true;

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

                    // Report the collision to both bodies:
                    if (FixtureA.Body != null && FixtureA.Body.onCollisionEventHandler != null)
                        foreach (OnCollisionEventHandler handler in FixtureA.Body.onCollisionEventHandler.GetInvocationList())
                            enabledA = handler(FixtureA, FixtureB, this) && enabledA;

                    // Reverse the order of the reported fixtures. The first fixture is always the one that the
                    // user subscribed to.
                    if (FixtureB.Body != null && FixtureB.Body.onCollisionEventHandler != null)
                        foreach (OnCollisionEventHandler handler in FixtureB.Body.onCollisionEventHandler.GetInvocationList())
                            enabledB = handler(FixtureB, FixtureA, this) && enabledB;


                    Enabled = enabledA && enabledB;

                    // BeginContact can also return false and disable the contact
                    if (enabledA && enabledB && contactManager.BeginContact != null)
                        Enabled = contactManager.BeginContact(this);

                    // If the user disabled the contact (needed to exclude it in TOI solver) at any point by
                    // any of the callbacks, we need to mark it as not touching and call any separation
                    // callbacks for fixtures that didn't explicitly disable the collision.
                    if (!Enabled)
                        IsTouching = false;
                }
            }
            else
            {
                if (touching == false)
                {
                    //Report the separation to both participants:
                    if (FixtureA != null && FixtureA.OnSeparation != null)
                        FixtureA.OnSeparation(FixtureA, FixtureB, this);

                    //Reverse the order of the reported fixtures. The first fixture is always the one that the
                    //user subscribed to.
                    if (FixtureB != null && FixtureB.OnSeparation != null)
                        FixtureB.OnSeparation(FixtureB, FixtureA, this);

                    //Report the separation to both bodies:
                    if (FixtureA != null && FixtureA.Body != null && FixtureA.Body.onSeparationEventHandler != null)
                        FixtureA.Body.onSeparationEventHandler(FixtureA, FixtureB, this);

                    //Reverse the order of the reported fixtures. The first fixture is always the one that the
                    //user subscribed to.
                    if (FixtureB != null && FixtureB.Body != null && FixtureB.Body.onSeparationEventHandler != null)
                        FixtureB.Body.onSeparationEventHandler(FixtureB, FixtureA, this);

                    contactManager.EndContact?.Invoke(this);
                }
            }

            if (sensor)
                return;

            contactManager.PreSolve?.Invoke(this, ref oldManifold);
        }

        /// <summary>
        /// Evaluate this contact with your own manifold and transforms.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="transformA">The first transform.</param>
        /// <param name="transformB">The second transform.</param>
        private void Evaluate(Manifold manifold, ref PhysicsTransform transformA, ref PhysicsTransform transformB)
        {
            Debug.Assert(FixtureA != null && FixtureB != null);

            switch (_type)
            {
                case ContactType.Polygon:
                    Shapes.Collision.CollidePolygons(ref manifold, (PolygonShape) FixtureA.Shape, ref transformA, (PolygonShape) FixtureB.Shape, ref transformB);
                    break;
                case ContactType.PolygonAndCircle:
                    Shapes.Collision.CollidePolygonAndCircle(ref manifold, (PolygonShape) FixtureA.Shape, ref transformA, (CircleShape) FixtureB.Shape, ref transformB);
                    break;
                case ContactType.EdgeAndCircle:
                    Shapes.Collision.CollideEdgeAndCircle(ref manifold, (EdgeShape) FixtureA.Shape, ref transformA, (CircleShape) FixtureB.Shape, ref transformB);
                    break;
                case ContactType.EdgeAndPolygon:
                    Shapes.Collision.CollideEdgeAndPolygon(ref manifold, (EdgeShape) FixtureA.Shape, ref transformA, (PolygonShape) FixtureB.Shape, ref transformB);
                    break;
                case ContactType.ChainAndCircle:
                    ChainShape chain = (ChainShape) FixtureA.Shape;
                    chain.GetChildEdge(_edge, ChildIndexA);
                    Shapes.Collision.CollideEdgeAndCircle(ref manifold, _edge, ref transformA, (CircleShape) FixtureB.Shape, ref transformB);
                    break;
                case ContactType.ChainAndPolygon:
                    ChainShape loop2 = (ChainShape) FixtureA.Shape;
                    loop2.GetChildEdge(_edge, ChildIndexA);
                    Shapes.Collision.CollideEdgeAndPolygon(ref manifold, _edge, ref transformA, (PolygonShape) FixtureB.Shape, ref transformB);
                    break;
                case ContactType.Circle:
                    Shapes.Collision.CollideCircles(ref manifold, (CircleShape) FixtureA.Shape, ref transformA, (CircleShape) FixtureB.Shape, ref transformB);
                    break;
            }
        }

        internal static Contact? Create(ContactManager contactManager, Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            ShapeType type1 = fixtureA.Shape.ShapeType;
            ShapeType type2 = fixtureB.Shape.ShapeType;

            DebugTools.Assert(ShapeType.Unknown < type1 && type1 < ShapeType.TypeCount);
            DebugTools.Assert(ShapeType.Unknown < type2 && type2 < ShapeType.TypeCount);

            Contact? c = null;
            var contactPoolList = contactManager._contactPoolList;
            if (contactPoolList.Next != contactPoolList)
            {
                // get first item in the pool.
                c = contactPoolList.Next;
                Debug.Assert(c != null);
                // Remove from the pool.
                contactPoolList.Next = c.Next;
                c.Next = null;
            }
            // Edge+Polygon is non-symetrical due to the way Erin handles collision type registration.
            if ((type1 >= type2 || (type1 == ShapeType.Edge && type2 == ShapeType.Polygon)) && !(type2 == ShapeType.Edge && type1 == ShapeType.Polygon))
            {
                if (c == null)
                    c = new Contact(fixtureA, indexA, fixtureB, indexB);
                else
                    c.Reset(fixtureA, indexA, fixtureB, indexB);
            }
            else
            {
                if (c == null)
                    c = new Contact(fixtureB, indexB, fixtureA, indexA);
                else
                    c.Reset(fixtureB, indexB, fixtureA, indexA);
            }


            c._type = _registers[(int)type1, (int)type2];

            return c;
        }

        internal void Destroy()
        {
            if (Manifold.PointCount > 0 && FixtureA?.IsSensor == false && FixtureB?.IsSensor == false)
            {
                FixtureA.Body.Awake = true;
                FixtureB.Body.Awake = true;
            }

            Reset(null, 0, null, 0);
        }
    }

    public sealed class ContactEdge
    {
        /// <summary>
        ///     Parent contact for this edge.
        /// </summary>
        public Contact? Contact { get; set; } = default!;

        /// <summary>
        ///     Other body we're connected to.
        /// </summary>
        public PhysicsComponent? Other { get; set; } = default!;

        /// <summary>
        ///     Next edge in our body's contact list.
        /// </summary>
        public ContactEdge? Next { get; set; } = default!;

        /// <summary>
        ///     Previous edge in our body's contact list.
        /// </summary>
        public ContactEdge? Prev { get; set; } = default!;

        public ContactEdge() {}

        public ContactEdge(Contact contact, PhysicsComponent other, ContactEdge next, ContactEdge previous)
        {
            Contact = contact;
            Other = other;
            Next = next;
            Prev = previous;
        }
    }

    public enum ContactType
    {
        NotSupported = 0,
        Polygon,
        PolygonAndCircle,
        Circle,
        EdgeAndPolygon,
        EdgeAndCircle,
        ChainAndPolygon,
        ChainAndCircle,
    }
}
