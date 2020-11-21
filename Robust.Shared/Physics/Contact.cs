using System;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Represents a contact between 2 shapes. Even if this exists it doesn't mean that their is an overlap as
    ///     it only represents the AABB overlapping.
    /// </summary>
    internal sealed class Contact
    {
        private ContactType _contactType;

        private static EdgeShape _edge = new EdgeShape();

        // _registers?

        internal ContactEdge _nodeA = new ContactEdge();
        internal ContactEdge _nodeB = new ContactEdge();

        /// <summary>
        ///     TimeOfImpact count
        /// </summary>
        internal int _toiCount;

        /// <summary>
        ///     TimeOfImpact TODO: Rename
        /// </summary>
        internal float _toi;

        internal Fixture FixtureA { get; set; }
        internal Fixture FixtureB { get; set; }

        /// <summary>
        ///     Friction of the contact.
        /// </summary>
        internal float Friction { get; set; }

        /// <summary>
        ///     Restitution (AKA bounciness) of the contact.
        /// </summary>
        internal float Restitution { get; set; }

        // TODO
        internal Manifold Manifold { get; } = default!;

        /// <summary>
        ///     Get / set desired tangent speed for conveyor belt behavior in m/s.
        /// </summary>
        internal float TangentSpeed { get; set; }

        // TODO: That long-ass comment
        internal bool Enabled { get; set; } = true;

        /// <summary>
        ///     Get the child primitive index for FixtureA.
        /// </summary>
        internal int ChildIndexA { get; set; }

        /// <summary>
        ///     Get the child primitive index for FixtureB.
        /// </summary>
        internal int ChildIndexB { get; set; }

        /// <summary>
        ///     Get the next contact in the map's contact list.
        /// </summary>
        internal Contact? Next { get; set; }

        /// <summary>
        ///     Get the previous contact in the map's contact list.
        /// </summary>
        internal Contact? Previous { get; set; }

        /// <summary>
        ///     Is this Contact currently touching.
        /// </summary>
        internal bool IsTouching { get; set; }

        /// <summary>
        ///     Has this contact already been added to a physics island?
        /// </summary>
        internal bool IslandFlag { get; set; }

        internal bool TOIFlag { get; set; }

        internal bool FilterFlag { get; set; }

        internal void ResetRestitution()
        {
            throw new NotImplementedException();
        }

        internal void ResetFriction()
        {
            throw new NotImplementedException();
        }

        protected Contact(Fixture fixtureA, int indexA, Fixture fixtureB, int indexB)
        {
            Reset(fixtureA, indexA, fixtureB, indexB);
        }

        internal void GetMapManifold(out Vector2 normal, out Vector2[] points)
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

    internal sealed class ContactEdge
    {
        /// <summary>
        ///     Parent contact for this edge.
        /// </summary>
        internal Contact Contact { get; set; } = default!;

        /// <summary>
        ///     Other body we're connected to.
        /// </summary>
        internal IPhysBody Other { get; set; } = default!;

        /// <summary>
        ///     Next edge in our body's contact list.
        /// </summary>
        internal ContactEdge Next { get; set; } = default!;

        /// <summary>
        ///     Previous edge in our body's contact list.
        /// </summary>
        internal ContactEdge Previous { get; set; } = default!;

        internal ContactEdge() {}

        internal ContactEdge(Contact contact, IPhysBody other, ContactEdge next, ContactEdge previous)
        {
            Contact = contact;
            Other = other;
            Next = next;
            Previous = previous;
        }
    }

    internal enum ContactType
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
