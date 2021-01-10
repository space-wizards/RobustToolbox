using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    [Serializable, NetSerializable]
    public struct FixtureState
    {
        // public

        public static List<FixtureState> ToStates(List<Fixture> fixtures)
        {
            var states = new List<FixtureState>();
            foreach (var fixture in fixtures)
            {
                states.Add(ToState(fixture));
            }

            return states;
        }

        public static FixtureState ToState(Fixture fixture)
        {
            return new FixtureState()
            {

            };
        }

        public static Fixture ToFixture(FixtureState state, PhysicsComponent body)
        {
            var fixture = new Fixture
            {
                Body = body,
            };



            return fixture;
        }

        public static List<Fixture> ToFixtures(List<FixtureState> states, PhysicsComponent body)
        {
            var results = new List<Fixture>();
            foreach (var state in states)
            {
                results.Add(ToFixture(state, body));
            }

            return results;
        }
    }
}
