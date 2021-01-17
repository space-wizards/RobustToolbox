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
        private IPhysShape Shape = default!;

        public static FixtureData From(Fixture fixture)
        {
            var data = new FixtureData
            {
                Shape = fixture.Shape
            };
            return data;
        }

        public static Fixture To(FixtureData data)
        {
            var fixture = new Fixture(data.Shape);
            return fixture;
        }
    }
}
