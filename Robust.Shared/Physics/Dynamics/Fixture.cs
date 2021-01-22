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
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
    public interface IFixture
    {
        // TODO
    }

    public class Fixture : IFixture, IExposeData, IEquatable<Fixture>
    {
        // TODO: For now we'll just do 1 proxy until we get multiple shapes
        public Dictionary<GridId, FixtureProxy[]> Proxies;

        public IPhysShape Shape { get; private set; } = default!;

        public PhysicsComponent Body { get; internal set; } = default!;

        /// <summary>
        ///     Non-hard <see cref="IPhysicsComponent"/>s will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        public bool Hard
        {
            get => _hard;
            set
            {
                if (_hard == value)
                    return;

                _hard = value;
                Body.FixtureChanged(this);
            }
        }

        private bool _hard;

        /// <summary>
        /// Bitmask of the collision layers the component is a part of.
        /// </summary>
        public int CollisionLayer
        {
            get => _collisionLayer;
            set
            {
                if (_collisionLayer == value)
                    return;

                _collisionLayer = value;
                Body.FixtureChanged(this);
            }
        }

        private int _collisionLayer;

        /// <summary>
        ///  Bitmask of the layers this component collides with.
        /// </summary>
        public int CollisionMask
        {
            get => _collisionMask;
            set
            {
                if (_collisionMask == value)
                    return;

                _collisionMask = value;
                Body.FixtureChanged(this);
            }
        }

        private int _collisionMask;

        public Fixture(PhysicsComponent body, IPhysShape shape)
        {
            Body = body;
            Shape = shape;
            Proxies = new Dictionary<GridId, FixtureProxy[]>();
        }

        public Fixture(IPhysShape shape, int collisionLayer, int collisionMask, bool hard)
        {
            Shape = shape;
            Proxies = new Dictionary<GridId, FixtureProxy[]>();
            _collisionLayer = collisionLayer;
            _collisionMask = collisionMask;
            _hard = hard;
        }

        public Fixture()
        {
            Proxies = new Dictionary<GridId, FixtureProxy[]>();
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            Proxies = new Dictionary<GridId, FixtureProxy[]>();
            serializer.DataField(this, x => x.Shape, "shape", new PhysShapeAabb());
            serializer.DataField(ref _hard, "hard", true);
            serializer.DataField(ref _collisionLayer, "layer", 0, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _collisionMask, "mask", 0, WithFormat.Flags<CollisionMask>());
        }

        /// <summary>
        ///     Clear this fixture's proxies from the broadphase.
        ///     If doing this for every fixture at once consider using the method on PhysicsComponent instead.
        /// </summary>
        /// <remarks>
        ///     Broadphase system will also need cleaning up for the cached broadphases for the body.
        /// </remarks>
        /// <param name="broadPhaseSystem"></param>
        public void ClearProxies(SharedBroadPhaseSystem? broadPhaseSystem = null)
        {
            var mapId = Body.Owner.Transform.MapID;
            broadPhaseSystem ??= EntitySystem.Get<SharedBroadPhaseSystem>();

            foreach (var (gridId, proxies) in Proxies)
            {
                var broadPhase = broadPhaseSystem.GetBroadPhase(mapId, gridId);
                if (broadPhase == null) continue;

                foreach (var proxy in proxies)
                {
                    broadPhase.RemoveProxy(proxy.ProxyId);
                }
            }

            Proxies.Clear();
        }

        /// <summary>
        ///     Creates FixtureProxies on the relevant broadphases.
        ///     If doing this for every fixture at once consider using the method on PhysicsComponent instead.
        /// </summary>
        /// <remarks>
        ///     You will need to manually add this to the body's broadphases.
        /// </remarks>
        public void CreateProxies(IMapManager? mapManager = null, SharedBroadPhaseSystem? broadPhaseSystem = null)
        {
            var mapId = Body.Owner.Transform.MapID;
            mapManager ??= IoCManager.Resolve<IMapManager>();
            broadPhaseSystem ??= EntitySystem.Get<SharedBroadPhaseSystem>();

            var worldAABB = Body.GetWorldAABB(mapManager);
            var worldPosition = Body.Owner.Transform.WorldPosition;
            var worldRotation = Body.Owner.Transform.WorldRotation;

            foreach (var gridId in mapManager.FindGridIdsIntersecting(mapId, worldAABB, true))
            {
                var broadPhase = broadPhaseSystem.GetBroadPhase(mapId, gridId);
                if (broadPhase == null) continue;

                Vector2 offset = worldPosition;
                double gridRotation = worldRotation;

                if (gridId != GridId.Invalid)
                {
                    var grid = mapManager.GetGrid(gridId);
                    offset -= grid.WorldPosition;
                    // TODO: Should probably have a helper for this
                    gridRotation = worldRotation - Body.Owner.EntityManager.GetEntity(grid.GridEntityId).Transform.WorldRotation;
                }

                var proxies = new FixtureProxy[1];
                // TODO: Will need update number with ProxyCount
                Proxies[gridId] = proxies;
                Box2 aabb = Shape.CalculateLocalBounds(gridRotation).Translated(offset);

                var proxy = new FixtureProxy(aabb, this);

                proxy.ProxyId = broadPhase.AddProxy(ref proxy);
                proxies[0] = proxy;
                DebugTools.Assert(proxies[0].ProxyId != DynamicTree.Proxy.Free);
            }
        }

        /// <summary>
        ///     Creates FixtureProxies on the relevant broadphase.
        ///     If doing this for every fixture at once consider using the method on PhysicsComponent instead.
        /// </summary>
        public void CreateProxies(IBroadPhase broadPhase, IMapManager? mapManager = null, SharedBroadPhaseSystem? broadPhaseSystem = null)
        {
            // TODO: Combine with the above method to be less DRY.
            mapManager ??= IoCManager.Resolve<IMapManager>();
            broadPhaseSystem ??= EntitySystem.Get<SharedBroadPhaseSystem>();

            var gridId = broadPhaseSystem.GetGridId(broadPhase);

            Vector2 offset = Body.Owner.Transform.WorldPosition;
            var worldRotation = Body.Owner.Transform.WorldRotation;
            double gridRotation = worldRotation;

            if (gridId != GridId.Invalid)
            {
                var grid = mapManager.GetGrid(gridId);
                offset -= grid.WorldPosition;
                // TODO: Should probably have a helper for this
                gridRotation = worldRotation - Body.Owner.EntityManager.GetEntity(grid.GridEntityId).Transform.WorldRotation;
            }

            var proxies = new FixtureProxy[1];
            // TODO: Will need update number with ProxyCount
            Proxies[gridId] = proxies;
            Box2 aabb = Shape.CalculateLocalBounds(gridRotation).Translated(offset);

            var proxy = new FixtureProxy(aabb, this);

            proxy.ProxyId = broadPhase.AddProxy(ref proxy);
            proxies[0] = proxy;
            DebugTools.Assert(proxies[0].ProxyId != DynamicTree.Proxy.Free);

            broadPhaseSystem.AddBroadPhase(Body, broadPhase);
        }

        // This is a crude equals mainly to avoid having to re-create the fixtures every time a state comes in.
        public bool Equals(Fixture? other)
        {
            if (other == null) return false;

            return _hard == other.Hard &&
                   _collisionLayer == other.CollisionLayer &&
                   _collisionMask == other.CollisionMask &&
                   Shape.Equals(other.Shape) &&
                   Body == other.Body;
        }
    }
}
