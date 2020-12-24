using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     A wrapper for shapes that's used to attach them to bodies with additional data such as mass or friction.
    /// </summary>
    public sealed class Fixture : IExposeData
    {
        // TODO: Need to call Dirty on shit I guess

        /// <summary>
        ///     Proxies are essentially a wrapper around shape children.
        ///     Currently it's only used for chain shapes as all the other types have 1 child only.
        /// </summary>
        /// <remarks>
        ///     Originally Aether2D had this as an array and I wasn't entirely sure how to handle spanning multiple grids
        ///     so for now I've just made it so proxies are stored per-grid.
        /// </remarks>
        public Dictionary<GridId, FixtureProxy[]> Proxies { get; private set; } = default!;

        public int ProxyCount { get; private set; }

        // todo: this should be nullable at some stage ahhhh
        /// <summary>
        ///     Parent body of this fixture.
        /// </summary>
        public PhysicsComponent Body { get; internal set; } = default!;

        /// <summary>
        ///     Our child shape.
        /// </summary>
        public Shape Shape { get; set; } = default!;

        /// <summary>
        /// Fires after two shapes have collided and are solved. This gives you a chance to get the impact force.
        /// </summary>
        public AfterCollisionEventHandler? AfterCollision;

        /// <summary>
        /// Fires when two fixtures are close to each other.
        /// Due to how the broadphase works, this can be quite inaccurate as shapes are approximated using AABBs.
        /// </summary>
        public BeforeCollisionEventHandler? BeforeCollision;

        /// <summary>
        /// Fires when two shapes collide and a contact is created between them.
        /// Note that the first fixture argument is always the fixture that the delegate is subscribed to.
        /// </summary>
        public OnCollisionEventHandler? OnCollision;

        /// <summary>
        /// Fires when two shapes separate and a contact is removed between them.
        /// Note: This can in some cases be called multiple times, as a fixture can have multiple contacts.
        /// Note The first fixture argument is always the fixture that the delegate is subscribed to.
        /// </summary>
        public OnSeparationEventHandler? OnSeparation;

        /// <summary>
        ///     Are we hard-collidable or just used for collision events.
        /// </summary>
        public bool IsSensor
        {
            get => _isSensor;
            private set
            {
                if (_isSensor == value)
                    return;

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

        // TODO: Collision event handlers but we could probably use eventbus.

        /// <summary>
        ///     What layers do we collide with (for external entities).
        /// </summary>
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

        /// <summary>
        ///     What layers do we collide with that affects us.
        /// </summary>
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

        public Fixture(Shape shape)
        {
            Shape = shape.Clone();

            Proxies = new Dictionary<GridId, FixtureProxy[]>();
            ProxyCount = 0;
        }

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
                var fixtureA = contact?.FixtureA;
                var fixtureB = contact?.FixtureB;

                if ((fixtureA == this || fixtureB == this) && contact != null)
                    contact.FilterFlag = true;

                edge = edge.Next;
            }

            // Touch each proxy to create new pairs

            TouchProxies(IoCManager.Resolve<IBroadPhaseManager>());
        }

        /// <summary>
        ///     Touch each proxy to create new pairs AKA did we move.
        /// </summary>
        /// <param name="broadPhase"></param>
        internal void TouchProxies(IBroadPhaseManager broadPhase)
        {
            foreach (var (_, proxies) in Proxies)
            {
                for (var i = 0; i < ProxyCount; i++)
                    broadPhase.TouchProxy(proxies[i]);
            }
        }

        /// <summary>
        ///     Test if a point is contained in this fixture.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool TestPoint(ref Vector2 point)
        {
            var transform = Body.GetTransform();
            return Shape.TestPoint(ref transform, ref point);
        }

        public bool RayCast(out RayCastOutput output, ref RayCastInput input, int childIndex)
        {
            var transform = Body.GetTransform();
            return Shape.RayCast(out output, ref input, transform, childIndex);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="gridId"></param>
        /// <param name="transform">In grid-space</param>
        /// <returns></returns>
        internal FixtureProxy[] CreateProxies(GridId gridId, in PhysicsTransform transform, IMapManager? mapManager = null)
        {
            // The original created them plus added them to broadphase which was clunky and not a good
            // separation of responsibilities so this solely creates them and we'll handle broadphase
            // elsewhere
            DebugTools.Assert(ProxyCount == 0);
            mapManager ??= IoCManager.Resolve<IMapManager>();

            PhysicsTransform gridTransform;
            if (gridId != GridId.Invalid)
            {
                var grid = mapManager.GetGrid(gridId);
                // TODO: Need to get grids rotation
                gridTransform = new PhysicsTransform(grid.WorldToLocal(transform.Position), transform.Quaternion);
            }
            else
            {
                gridTransform = transform;
            }

            ProxyCount = Shape.ChildCount;

            Proxies[gridId] = new FixtureProxy[ProxyCount];

            for (var i = 0; i < ProxyCount; i++)
            {
                var proxy = new FixtureProxy
                {
                    Fixture = this,
                    ChildIndex = i,
                    AABB = Shape.ComputeAABB(gridTransform, i),
                    ProxyId = new DynamicTree.Proxy(i),
                };

                Proxies[gridId][i] = proxy;
            }

            return Proxies[gridId];
        }

        internal void DestroyProxies()
        {
            foreach (var (gridId, proxies) in Proxies)
            {
                var broadPhase = EntitySystem.Get<SharedBroadPhaseSystem>().GetBroadPhase(gridId);
                for (var i = 0; i < ProxyCount; i++)
                    broadPhase.RemoveProxy(proxies[i].ProxyId);
            }

            ProxyCount = 0;
        }

        /// <summary>
        ///     Clones the fixture onto the specified body.
        /// </summary>
        /// <param name="body">The body you wish to clone the fixture onto.</param>
        /// <returns>The cloned fixture.</returns>
        public Fixture CloneOnto(PhysicsComponent body)
        {
            return CloneOnto(body, Shape);
        }

        /// <summary>
        /// Clones the fixture and attached shape onto the specified body.
        /// Note: This is used only by Deserialization.
        /// </summary>
        /// <param name="body">The body you wish to clone the fixture onto.</param>
        /// <returns>The cloned fixture.</returns>
        internal Fixture CloneOnto(PhysicsComponent body, Shape shape)
        {
            Fixture fixture = new Fixture(shape.Clone())
            {
                Restitution = Restitution,
                Friction = Friction,
                IsSensor = IsSensor,
                CollisionLayer = CollisionLayer,
                CollisionMask = CollisionMask
            };

            body.AddFixture(fixture);
            return fixture;
        }
    }
}
