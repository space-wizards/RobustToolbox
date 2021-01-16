using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    public interface IFixture
    {
        // TODO
    }

    [Serializable, NetSerializable]
    public class Fixture : IFixture, IExposeData
    {
        // TODO: For now we'll just do 1 proxy until we get multiple shapes
        public Dictionary<GridId, FixtureProxy[]> Proxies = new();

        public IPhysShape Shape { get; private set; } = default!;

        public IPhysicsComponent Body { get; private set; } = default!;

        /// <summary>
        ///     Non-hard <see cref="IPhysicsComponent"/>s will not cause action collision (e.g. blocking of movement)
        ///     while still raising collision events.
        /// </summary>
        /// <remarks>
        ///     This is useful for triggers or such to detect collision without actually causing a blockage.
        /// </remarks>
        public bool Hard { get; set; }

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
                if (value == _collisionMask)
                    return;

                _collisionMask = value;
            }
        }

        private int _collisionMask;

        public Fixture(IPhysicsComponent body)
        {
            Body = body;
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _collisionLayer, "layer", 0, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _collisionMask, "mask", 0, WithFormat.Flags<CollisionMask>());
        }

        public void Startup()
        {
            // TODO: Compute proxies and add to broadphases.
        }

        public void Shutdown()
        {
            foreach (var (gridId, proxies) in Proxies)
            {

            }

            // TODO: Remove from broadphase
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Test whether a point is contained in this fixture in world-space.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool TestPoint(Vector2 point)
        {
            throw new NotImplementedException();
        }

        public bool RayCast()
        {
            throw new NotImplementedException();
        }
    }
}
