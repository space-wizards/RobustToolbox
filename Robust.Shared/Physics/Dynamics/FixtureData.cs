using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics.Dynamics
{
    /// <summary>
    ///     For serialization
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FixtureData
    {
        /*
         * If you ever feel like a moron then I spent an hour trying to debug why raycasts weren't working on the client
         * before I realised I wasn't sending the layer / mask / hard data :)))))))))))))
         */

        private IPhysShape _shape = default!;

        private int _collisionLayer;

        private int _collisionMask;

        private bool _hard;

        public static FixtureData From(Fixture fixture)
        {
            var data = new FixtureData
            {
                _shape = fixture.Shape,
                _collisionLayer = fixture.CollisionLayer,
                _collisionMask = fixture.CollisionMask,
                _hard = fixture.Hard,
            };
            return data;
        }

        public static Fixture To(FixtureData data)
        {
            var fixture = new Fixture(data._shape, data._collisionLayer, data._collisionMask, data._hard);
            return fixture;
        }
    }
}
