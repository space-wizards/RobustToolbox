using System.Diagnostics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     A wrapper for shapes that's used to attach them to bodies with additional data such as mass or friction.
    /// </summary>
    public sealed class Fixture : IExposeData
    {
        /// <summary>
        ///     Parent body of this fixture.
        /// </summary>
        public PhysicsComponent Body { get; private set; } = default!;

        /// <summary>
        ///     Our child shape.
        /// </summary>
        public Shape Shape { get; set; }

        /// <summary>
        ///     Are we hard-collidable or just used for collision events.
        /// </summary>
        public bool IsSensor
        {
            get => _isSensor;
            private set
            {
                if (Body != null)
                    Body.Awake = true;

                _isSensor = value;
            }
        }

        private bool _isSensor;

        public float Friction { get; private set; }

        /// <summary>
        ///     How much bounce is there on collision.
        /// </summary>
        public float Restitution { get; set; }

        // TODO: Proxies?

        // Collision event handlers but we could probably use eventbus.

        public int CollisionLayer
        {
            get => _collisionLayer;
            set
            {
                if (_collisionLayer == value)
                    return;

                _collisionLayer = value;
                Refilter();
            }
        }

        private int _collisionLayer;

        public int CollisionMask
        {
            get => _collisionMask;
            set
            {
                if (_collisionMask == value)
                    return;

                _collisionMask = value;
                Refilter();
            }
        }

        private int _collisionMask;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction("restitution", 0f, value => Restitution = value, () => Restitution);
            serializer.DataReadWriteFunction("friction", 0.2f, value => Friction = value, () => Friction);
        }

        private void Refilter()
        {
            var edge = Body.ContactList;
            while (edge != null)
            {
                var contact = edge.Contact;
                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                if (fixtureA == this || fixtureB == this)
                    contact.FilterFlag = true;

                edge = edge.Next;
            }

            // Touch each proxy to create new pairs
            var map = Body.PhysicsMap;

            if (map == null)
                return;

            var broadPhase = map.ContactManager.Broadphase;
            TouchProxies(broadPhase);
        }
    }
}
