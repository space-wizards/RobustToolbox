using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Storage for physics fixtures
    /// </summary>
    /// <remarks>
    /// In its own component to decrease physics comp state size significantly.
    /// </remarks>
    [RegisterComponent]
    [NetworkedComponent]
    [ComponentReference(typeof(ILookupWorldBox2Component))]
    public sealed class FixturesComponent : Component, ILookupWorldBox2Component
    {
        // This is a snowflake component whose main job is making physics states smaller for massive bodies
        // (e.g. grids)
        // Content generally shouldn't care about its existence.

        [ViewVariables]
        public int FixtureCount => Fixtures.Count;

        [ViewVariables]
        [Access(typeof(FixtureSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public readonly Dictionary<string, Fixture> Fixtures = new();

        [DataField("fixtures", customTypeSerializer:typeof(FixtureSerializer))]
        [NeverPushInheritance]
        [Access(typeof(FixtureSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        internal List<Fixture> SerializedFixtures
        {
            get => SerializedFixtureData ?? Fixtures.Values.ToList();
            set => SerializedFixtureData = value;
        }

        internal List<Fixture>? SerializedFixtureData;

        public Box2 GetAABB(Transform transform)
        {
            var bounds = new Box2(transform.Position, transform.Position);
            // TODO cache this to speed up entity lookups & tree updating
            foreach (var fixture in Fixtures.Values)
            {
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    // TODO don't transform each fixture, just transform the final AABB
                    var boundy = fixture.Shape.ComputeAABB(transform, i);
                    bounds = bounds.Union(boundy);
                }
            }

            return bounds;
        }
    }
}
