using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
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
    [Friend(typeof(FixtureSystem))]
    [ComponentProtoName("Fixtures")]
    public sealed class FixturesComponent : Component, ISerializationHooks
    {
        // This is a snowflake component whose main job is making physics states smaller for massive bodies
        // (e.g. grids)
        // Content generally shouldn't care about its existence.

        [ViewVariables]
        public int FixtureCount => Fixtures.Count;

        [ViewVariables]
        public readonly Dictionary<string, Fixture> Fixtures = new();

        [DataField("fixtures")]
        [NeverPushInheritance]
        internal List<Fixture> SerializedFixtures = new();

        void ISerializationHooks.BeforeSerialization()
        {
            // Can't assert the count is 0 because it's possible it gets re-serialized before init!
            if (SerializedFixtures.Count > 0) return;

            // At some stage we'll serialize non-default grids (gridfixturesystem does support it)
            // but for now just for smaller map file + reading speed we'll just globally cull them.
            if (IoCManager.Resolve<IEntityManager>().HasComponent<MapGridComponent>(Owner)) return;

            foreach (var (_, fixture) in Fixtures)
            {
                SerializedFixtures.Add(fixture);
            }
        }
    }
}
