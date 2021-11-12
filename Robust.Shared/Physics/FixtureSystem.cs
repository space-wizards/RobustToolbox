using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Managers physics fixtures.
    /// </summary>
    public sealed class FixtureSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FixturesComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FixturesComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<FixturesComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<PhysicsInitializedEvent>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentShutdown>(OnPhysicsShutdown);
        }

        private void OnInit(EntityUid uid, FixturesComponent component, ComponentInit args)
        {
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                if (component._serializedFixtures.Count > 0)
                {
                    DebugTools.Assert(false);
                    Logger.ErrorS("fixtures", $"Tried to add a {nameof(FixturesComponent)} to something that doesn't have a {nameof(PhysicsComponent)}");
                }

                return;
            }

            // Can't resolve in serialization so here we are.
            foreach (var fixture in component._serializedFixtures)
            {
                fixture.Body = body;
                fixture.ID = GetFixtureName(component, fixture);
            }

            component._serializedFixtures.Clear();

            // Make sure all the right stuff is set on the body
            FixtureUpdate(component);
        }

        #region Public

        public void CreateFixture(PhysicsComponent body, Fixture fixture, bool updates = true, FixturesComponent? manager = null)
        {
            if (!Resolve(body.OwnerUid, ref manager))
            {
                DebugTools.Assert(false);
                return;
            }

            fixture.ID = GetFixtureName(manager, fixture);
            manager.Fixtures.Add(fixture.ID, fixture);
            fixture.Body = body;

            // TODO: Assert world locked
            // Broadphase should be set in the future TM
            // Should only happen for nullspace / initializing entities
            if (body.Broadphase != null)
            {
                _broadphaseSystem.UpdateBroadphaseCache(body.Broadphase);
                _broadphaseSystem.CreateProxies(fixture, body.Owner.Transform.WorldPosition, false);
            }

            // Supposed to be wrapped in density but eh
            if (updates)
            {
                FixtureUpdate(manager, body);
                body.ResetMassData();
                manager.Dirty();
            }
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
            // TODO: Make it take in density instead?
            var fixture = new Fixture(body, shape) {Mass = mass};
            CreateFixture(body, fixture);
        }

        public Fixture? GetFixture(PhysicsComponent body, string id, FixturesComponent? manager = null)
        {
            if (!Resolve(body.OwnerUid, ref manager))
            {
                return null;
            }

            return manager.Fixtures.TryGetValue(id, out var fixture) ? fixture : null;
        }

        public void DestroyFixture(Fixture fixture, bool updates = true, FixturesComponent? manager = null)
        {
            DestroyFixture(fixture.Body, fixture, updates, manager);
        }

        public void DestroyFixture(PhysicsComponent body, Fixture fixture, bool updates = true, FixturesComponent? manager = null)
        {
            if (!Resolve(body.OwnerUid, ref manager))
            {
                return;
            }

            // TODO: Assert world locked
            DebugTools.Assert(fixture.Body == body);
            DebugTools.Assert(body.FixtureCount > 0);

            if (!manager.Fixtures.Remove(fixture.ID))
            {
                Logger.ErrorS("fixtures", $"Tried to remove fixture from {body.Owner} that was already removed.");
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

            var broadphase = _broadphaseSystem.GetBroadphase(fixture.Body);

            if (broadphase != null)
            {
                _broadphaseSystem.DestroyProxies(broadphase, fixture);
            }

            if (updates)
            {
                FixtureUpdate(manager, body);
                body.ResetMassData();
                manager.Dirty();
            }
        }

        #endregion

        private void OnPhysicsShutdown(EntityUid uid, PhysicsComponent component, ComponentShutdown args)
        {
            // TODO: Remove physics on fixturemanager shutdown
            if (EntityManager.GetEntity(uid).LifeStage > EntityLifeStage.MapInitialized) return;
            EntityManager.RemoveComponent<FixturesComponent>(uid);
        }

        private void OnPhysicsInit(ref PhysicsInitializedEvent ev)
        {
            EntityManager.EnsureComponent<FixturesComponent>(ev.Uid);
        }

        private void OnGetState(EntityUid uid, FixturesComponent component, ref ComponentGetState args)
        {
            args.State = new FixtureManagerComponentState
            {
                Fixtures = component.Fixtures.Values.ToList(),
            };
        }

        private void OnHandleState(EntityUid uid, FixturesComponent component, ref ComponentHandleState args)
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
                DestroyFixture(physics, fixture);
            }

            // TODO: We also still need event listeners for shapes (Probably need C# events)
            // Or we could just make it so shapes can only be updated via fixturesystem which handles it
            // automagically (friends or something?)
            foreach (var fixture in toAddFixtures)
            {
                computeProperties = true;
                CreateFixture(physics, fixture);
                fixture.Shape.ApplyState();
            }

            if (computeProperties)
            {
                physics.ResetMassData();
            }
        }

        internal string GetFixtureName(FixturesComponent component, Fixture fixture)
        {
            if (!string.IsNullOrEmpty(fixture.ID)) return fixture.ID;

            var i = 0;

            while (true)
            {
                ++i;
                var name = $"fixture_{i}";
                var found = component.Fixtures.ContainsKey(name);

                if (!found)
                {
                    return name;
                }
            }
        }

        private void ShapeUpdate(Fixture fixture)
        {
            // TODO: Ideally we somehow subscribe to shapes so we know when vertices updates or the likes.
        }

        /// <summary>
        /// Updates all of the cached physics information on the body derived from fixtures.
        /// </summary>
        public void FixtureUpdate(FixturesComponent component, PhysicsComponent? body = null)
        {
            if (!Resolve(component.OwnerUid, ref body))
            {
                return;
            }

            var mask = 0;
            var layer = 0;
            var hard = false;

            foreach (var (_, fixture) in component.Fixtures)
            {
                mask |= fixture.CollisionMask;
                layer |= fixture.CollisionLayer;
                hard |= fixture.Hard;
            }

            body.CollisionMask = mask;
            body.CollisionLayer = layer;
            body.Hard = hard;
        }

        [Serializable, NetSerializable]
        private sealed class FixtureManagerComponentState : ComponentState
        {
            public List<Fixture> Fixtures = default!;
        }
    }
}
