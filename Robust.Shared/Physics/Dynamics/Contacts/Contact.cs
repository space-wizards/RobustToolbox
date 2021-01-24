using System;
using System.Collections.Generic;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    internal sealed class Contact
    {
        public ContactEdge NodeA = new();
        public ContactEdge NodeB = new();

        public Fixture? FixtureA;
        public Fixture? FixtureB;

        public AetherManifold Manifold = default!;

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

        /// <summary>
        ///     Determines whether the contact is touching.
        /// </summary>
        public bool IsTouching { get; set; }

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
        internal static Contact Create(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            var type1 = fixtureA.Shape.ShapeType;
            var type2 = fixtureB.Shape.ShapeType;

            DebugTools.Assert(ShapeType.Unknown < type1 && type1 < ShapeType.TypeCount);
            DebugTools.Assert(ShapeType.Unknown < type2 && type2 < ShapeType.TypeCount);

            Queue<Contact> pool = fixtureA.Body.PhysicsMap._contactPool;
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
            // FilterFlag = false;
            // TOIFlag = false;

            FixtureA = fixtureA;
            FixtureB = fixtureB;

            // ChildIndexA = indexA;
            // ChildIndexB = indexB;

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

        internal void Destroy()
        {
            // Seems like active contacts were never used in farseer anyway
            // FixtureA?.Body.PhysicsMap.ContactManager.RemoveActiveContact(this);
            FixtureA?.Body.PhysicsMap._contactPool.Enqueue(this);

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
