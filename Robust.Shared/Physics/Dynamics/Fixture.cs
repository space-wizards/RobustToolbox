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
        public Dictionary<GridId, FixtureProxy[]> Proxies;

        public IPhysShape Shape { get; private set; } = default!;

        public PhysicsComponent Body { get; private set; } = default!;

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

        public Fixture(IPhysShape shape)
        {
            Shape = shape;
            Proxies = new Dictionary<GridId, FixtureProxy[]>();
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
            Body.FixtureChanged(this);
        }
    }
}
