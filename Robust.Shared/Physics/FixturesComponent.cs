using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    [RegisterComponent]
    [NetworkedComponent]
    [Friend(typeof(FixtureSystem))]
    public sealed class FixturesComponent : Component, ISerializationHooks
    {
        // This is a snowflake component whose main job is making physics states smaller for massive bodies
        // (e.g. grids)
        // Content generally shouldn't care about its existence.

        public override string Name => "Fixtures";

        [ViewVariables]
        public int FixtureCount => Fixtures.Count;

        [ViewVariables]
        public Dictionary<string, Fixture> Fixtures = new();

        [DataField("fixtures")]
        [NeverPushInheritance]
        private List<Fixture> _serializedFixtures = new();

        void ISerializationHooks.BeforeSerialization()
        {
            DebugTools.Assert(_serializedFixtures.Count == 0);

            foreach (var (_, fixture) in Fixtures)
            {
                _serializedFixtures.Add(fixture);
            }
        }

        void ISerializationHooks.AfterDeserialization()
        {
            var fixtureSystem = EntitySystem.Get<FixtureSystem>();
            var physics = Owner.EntityManager.GetComponent<PhysicsComponent>(OwnerUid);

            foreach (var fixture in _serializedFixtures)
            {
                fixture.Body = physics;
                fixture.ComputeProperties();
                fixture.ID = fixtureSystem.GetFixtureName(this, fixture);
            }

            _serializedFixtures.Clear();
        }
    }
}
