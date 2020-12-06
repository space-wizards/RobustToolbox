using System;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Represents a contact between 2 shapes. Even if this exists it doesn't mean that their is an overlap as
    ///     it only represents the AABB overlapping.
    /// </summary>
    public sealed class Contact
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

        public Fixture FixtureA { get; set; }
        public Fixture FixtureB { get; set; }

        /// <summary>
        ///     Friction of the contact.
        /// </summary>
        public float Friction { get; set; }

        /// <summary>
        ///     Restitution (AKA bounciness) of the contact.
        /// </summary>
        public float Restitution { get; set; }

        // TODO
        public Manifold Manifold { get; } = default!;

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
            throw new NotImplementedException();
        }

        public void ResetFriction()
        {
            throw new NotImplementedException();
        }

        protected Contact(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            Reset(fixtureA, indexA, fixtureB, indexB);
        }

        public void GetMapManifold(out Vector2 normal, out Vector2[] points)
        {
            throw new NotImplementedException();
        }

        private void Reset(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            Enabled = true;
            IsTouching = false;
            // TODO: Flags

            FixtureA = fixtureA;
            FixtureB = fixtureB;

            ChildIndexA = indexA;
            ChildIndexB = indexB;

            Manifold.PointCount = 0;

            Next = null;
            Previous = null;

            // TODO: Node stuff

            throw new NotImplementedException();

            if (FixtureA != null && FixtureB != null)
            {

            }
        }
    }

    public sealed class ContactEdge
    {
        /// <summary>
        ///     Parent contact for this edge.
        /// </summary>
        public Contact Contact { get; set; } = default!;

        /// <summary>
        ///     Other body we're connected to.
        /// </summary>
        public IPhysBody Other { get; set; } = default!;

        /// <summary>
        ///     Next edge in our body's contact list.
        /// </summary>
        public ContactEdge Next { get; set; } = default!;

        /// <summary>
        ///     Previous edge in our body's contact list.
        /// </summary>
        public ContactEdge Previous { get; set; } = default!;

        public ContactEdge() {}

        public ContactEdge(Contact contact, IPhysBody other, ContactEdge next, ContactEdge previous)
        {
            Contact = contact;
            Other = other;
            Next = next;
            Previous = previous;
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
