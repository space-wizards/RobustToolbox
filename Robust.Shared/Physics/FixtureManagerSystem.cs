using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    public sealed class FixtureManagerSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FixtureManagerComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<FixtureManagerComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<PhysicsInitializedEvent>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsShutdownEvent>(OnPhysicsShutdown);
            // TODO: On physics init add comp
            // TODO: On physics remove remove comp
        }

        #region Public

        public void CreateFixture(PhysicsComponent body, Fixture fixture)
        {
            fixture.ID = body.GetFixtureName(fixture);
            body.Fixtures.Add(fixture);
            body.FixtureCount += 1;
            fixture.Body = body;

            // TODO: Assert world locked
            // Broadphase should be set in the future TM
            // Should only happen for nullspace / initializing entities
            if (body.Broadphase != null)
            {
                UpdateBroadphaseCache(body.Broadphase);
                CreateProxies(fixture, body.Owner.Transform.WorldPosition, false);
            }

            // Supposed to be wrapped in density but eh
            body.ResetMassData();
            body.Dirty();
            // TODO: Set newcontacts to true.
        }

        public Fixture CreateFixture(PhysicsComponent body, IPhysShape shape)
        {
            var fixture = new Fixture(body, shape);
            CreateFixture(body, fixture);
            return fixture;
        }

        public void CreateFixture(PhysicsComponent body, IPhysShape shape, float mass)
        {
            // TODO: Make it take in density instead
            var fixture = new Fixture(body, shape) {Mass = mass};
            CreateFixture(body, fixture);
        }

        public void DestroyFixture(Fixture fixture)
        {
            DestroyFixture(fixture.Body, fixture);
        }

        public void DestroyFixture(PhysicsComponent body, Fixture fixture)
        {
            // TODO: Move this to fixturemanagersystem instead

            // TODO: Assert world locked
            DebugTools.Assert(fixture.Body == body);
            DebugTools.Assert(body.FixtureCount > 0);

            if (!body.Fixtures.Remove(fixture))
            {
                Logger.ErrorS("physics", $"Tried to remove fixture from {body.Owner} that was already removed.");
                return;
            }

            var edge = body.ContactEdges;

            while (edge != null)
            {
                var contact = edge.Contact!;
                edge = edge.Next;

                var fixtureA = contact.FixtureA;
                var fixtureB = contact.FixtureB;

                if (fixture == fixtureA || fixture == fixtureB)
                {
                    body.PhysicsMap?.ContactManager.Destroy(contact);
                }
            }

            var broadphase = GetBroadphase(fixture.Body);

            if (broadphase != null)
            {
                DestroyProxies(broadphase, fixture);
            }

            body.ResetMassData();
            body.Dirty();
        }

        #endregion

        private void OnPhysicsShutdown(PhysicsShutdownEvent ev)
        {
            // TODO: Remove physics on fixturemanager shutdown
            if (EntityManager.GetEntity(ev.Uid).LifeStage > EntityLifeStage.MapInitialized) return;
            EntityManager.RemoveComponent<FixtureManagerComponent>(ev.Uid);
        }

        private void OnPhysicsInit(PhysicsInitializedEvent ev)
        {
            EntityManager.EnsureComponent<FixtureManagerComponent>(ev.Uid);
        }

        private void OnGetState(EntityUid uid, FixtureManagerComponent component, ref ComponentGetState args)
        {
            args.State = new FixtureManagerComponentState
            {
                Fixtures = component.Fixtures.Values.ToList(),
            };
        }

        private void OnHandleState(EntityUid uid, FixtureManagerComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not FixtureManagerComponentState state) return;

            var physics = EntityManager.GetComponent<PhysicsComponent>(uid);

            var toAddFixtures = new List<Fixture>();
            var toRemoveFixtures = new List<Fixture>();
            var computeProperties = false;

            // Given a bunch of data isn't serialized need to sort of re-initialise it
            var newFixtures = new List<Fixture>(state.Fixtures.Count);
            foreach (var fixture in state.Fixtures)
            {
                var newFixture = new Fixture();
                fixture.CopyTo(newFixture);
                newFixture.Body = physics;
                newFixtures.Add(newFixture);
            }

            // Add / update new fixtures
            foreach (var fixture in newFixtures)
            {
                var found = false;

                foreach (var (_, existing) in component.Fixtures)
                {
                    if (!fixture.ID.Equals(existing.ID)) continue;

                    if (!fixture.Equals(existing))
                    {
                        toAddFixtures.Add(fixture);
                        toRemoveFixtures.Add(existing);
                    }

                    found = true;
                    break;
                }

                if (!found)
                {
                    toAddFixtures.Add(fixture);
                }
            }

            // Remove old fixtures
            foreach (var (_, existing) in component.Fixtures)
            {
                var found = false;

                foreach (var fixture in newFixtures)
                {
                    if (fixture.ID.Equals(existing.ID))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toRemoveFixtures.Add(existing);
                }
            }

            foreach (var fixture in toRemoveFixtures)
            {
                computeProperties = true;
                _broadphaseSystem.DestroyFixture(physics, fixture);
            }

            // TODO: We also still need event listeners for shapes (Probably need C# events)
            foreach (var fixture in toAddFixtures)
            {
                computeProperties = true;
                _broadphaseSystem.CreateFixture(physics, fixture);
                fixture.Shape.ApplyState();
            }

            if (computeProperties)
            {
                physics.ResetMassData();
            }
        }

        [Serializable, NetSerializable]
        private sealed class FixtureManagerComponentState : ComponentState
        {
            public List<Fixture> Fixtures = default!;
        }

        public string SetFixtureID(Fixture fixture)
        {
            if (string.IsNullOrEmpty(fixture.ID))
            {

            }

            return fixture.ID;
        }
    }
}
