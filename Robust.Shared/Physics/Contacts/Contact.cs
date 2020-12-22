using System;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Represents a contact between 2 shapes. Even if this exists it doesn't mean that their is an overlap as
    ///     it only represents the AABB overlapping.
    /// </summary>
    public class Contact
    {
        private ContactType _contactType;

        private static EdgeShape _edge = new EdgeShape();

        // _registers?

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
        public Contact? Previous { get; set; }

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
            Restitution = PhysicsSettings.MixRestitution(FixtureA.Restitution, FixtureB.Restitution);
        }

        public void ResetFriction()
        {
            Friction = PhysicsSettings.MixFriction(FixtureA.Friction, FixtureB.Friction);
        }

        protected Contact(Fixture? fixtureA, int indexA, Fixture? fixtureB, int indexB)
        {
            Reset(fixtureA, indexA, fixtureB, indexB);
        }

        // TODO: Probs delete coz never used
        public void GetMapManifold(out Vector2 normal, out FixedArray2<Vector2> points)
        {
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
            Previous = null;

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
