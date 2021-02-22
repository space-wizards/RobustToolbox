/*
80% of this class is custom but just in case.

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
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics
{
    public interface IFixture
    {
        // TODO
    }

    [Serializable, NetSerializable]
    public sealed class Fixture : IFixture, IExposeData, IEquatable<Fixture>
    {
        public IReadOnlyDictionary<GridId, FixtureProxy[]> Proxies => _proxies;

        [NonSerialized]
        private readonly Dictionary<GridId, FixtureProxy[]> _proxies = new();

        [ViewVariables]
        [NonSerialized]
        public int ProxyCount = 0;

        [ViewVariables] public IPhysShape Shape { get; private set; } = default!;

        [ViewVariables]
        [field:NonSerialized]
        public PhysicsComponent Body { get; internal set; } = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Friction
        {
            get => _friction;
            set
            {
                if (MathHelper.CloseTo(value, _friction)) return;

                _friction = value;
                Body.FixtureChanged(this);
            }
        }

        private float _friction;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Restitution
        {
            get => _restitution;
            set
            {
                if (MathHelper.CloseTo(value, _restitution)) return;

                _restitution = value;
                Body.FixtureChanged(this);
            }
        }

        private float _restitution;

        /// <summary>
        ///     Non-hard <see cref="IPhysicsComponent"/>s will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Hard
        {
            get => _hard;
            set
            {
                if (_hard == value)
                    return;

                Body.RegenerateContacts();
                _hard = value;
                Body.FixtureChanged(this);
            }
        }

        private bool _hard;

        /// <summary>
        /// Bitmask of the collision layers the component is a part of.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get => _collisionLayer;
            set
            {
                if (_collisionLayer == value)
                    return;

                Body.RegenerateContacts();
                _collisionLayer = value;
                Body.FixtureChanged(this);
            }
        }

        private int _collisionLayer;

        /// <summary>
        ///  Bitmask of the layers this component collides with.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get => _collisionMask;
            set
            {
                if (_collisionMask == value)
                    return;

                Body.RegenerateContacts();
                _collisionMask = value;
                Body.FixtureChanged(this);
            }
        }

        private int _collisionMask;

        public Fixture(PhysicsComponent body, IPhysShape shape)
        {
            Body = body;
            Shape = shape;
        }

        public Fixture(IPhysShape shape, int collisionLayer, int collisionMask, bool hard)
        {
            Shape = shape;
            _collisionLayer = collisionLayer;
            _collisionMask = collisionMask;
            _hard = hard;
        }

        public Fixture() {}

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Shape, "shape", new PhysShapeAabb());
            serializer.DataField(ref _friction, "friction", 0.4f);
            serializer.DataField(ref _restitution, "restitution", 0f);
            serializer.DataField(ref _hard, "hard", true);
            serializer.DataField(ref _collisionLayer, "layer", 0, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _collisionMask, "mask", 0, WithFormat.Flags<CollisionMask>());
        }

        /// <summary>
        ///     As a bunch of things aren't serialized we need to instantiate Fixture from an empty ctor and then copy values across.
        /// </summary>
        /// <param name="fixture"></param>
        internal void CopyTo(Fixture fixture)
        {
            fixture.Shape = Shape;
            fixture._friction = _friction;
            fixture._restitution = _restitution;
            fixture._hard = _hard;
            fixture._collisionLayer = _collisionLayer;
            fixture._collisionMask = _collisionMask;
        }

        internal void SetProxies(GridId gridId, FixtureProxy[] proxies)
        {
            DebugTools.Assert(!_proxies.ContainsKey(gridId));
            _proxies[gridId] = proxies;
        }

        /// <summary>
        ///     Clear this fixture's proxies from the broadphase.
        ///     If doing this for every fixture at once consider using the method on PhysicsComponent instead.
        /// </summary>
        /// <remarks>
        ///     Broadphase system will also need cleaning up for the cached broadphases for the body.
        /// </remarks>
        /// <param name="mapId"></param>
        /// <param name="broadPhaseSystem"></param>
        public void ClearProxies(MapId? mapId = null, SharedBroadPhaseSystem? broadPhaseSystem = null)
        {
            mapId ??= Body.Owner.Transform.MapID;
            broadPhaseSystem ??= EntitySystem.Get<SharedBroadPhaseSystem>();

            foreach (var (gridId, proxies) in _proxies)
            {
                var broadPhase = broadPhaseSystem.GetBroadPhase(mapId.Value, gridId);
                if (broadPhase == null) continue;

                foreach (var proxy in proxies)
                {
                    broadPhase.RemoveProxy(proxy.ProxyId);
                }
            }

            _proxies.Clear();
        }

        /// <summary>
        ///     Clears the particular grid's proxies for this fixture.
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="broadPhaseSystem"></param>
        /// <param name="gridId"></param>
        public void ClearProxies(MapId mapId, SharedBroadPhaseSystem broadPhaseSystem, GridId gridId)
        {
            if (!Proxies.TryGetValue(gridId, out var proxies)) return;

            var broadPhase = broadPhaseSystem.GetBroadPhase(mapId, gridId);

            if (broadPhase != null)
            {
                foreach (var proxy in proxies)
                {
                    broadPhase.RemoveProxy(proxy.ProxyId);
                }
            }

            _proxies.Remove(gridId);
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
            DebugTools.Assert(_proxies.Count == 0);
            ProxyCount = Shape.ChildCount;

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

                var proxies = new FixtureProxy[Shape.ChildCount];
                _proxies[gridId] = proxies;

                for (var i = 0; i < ProxyCount; i++)
                {
                    // TODO: Will need to pass in childIndex to this as well
                    var aabb = Shape.CalculateLocalBounds(gridRotation).Translated(offset);

                    var proxy = new FixtureProxy(aabb, this, i);

                    proxy.ProxyId = broadPhase.AddProxy(ref proxy);
                    proxies[i] = proxy;
                    DebugTools.Assert(proxies[i].ProxyId != DynamicTree.Proxy.Free);
                }
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

            var proxies = new FixtureProxy[Shape.ChildCount];
            _proxies[gridId] = proxies;

            for (var i = 0; i < ProxyCount; i++)
            {
                // TODO: Will need to pass in childIndex to this as well
                var aabb = Shape.CalculateLocalBounds(gridRotation).Translated(offset);

                var proxy = new FixtureProxy(aabb, this, i);

                proxy.ProxyId = broadPhase.AddProxy(ref proxy);
                proxies[i] = proxy;
                DebugTools.Assert(proxies[i].ProxyId != DynamicTree.Proxy.Free);
            }

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
