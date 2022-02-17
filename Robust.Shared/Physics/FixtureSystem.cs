using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
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
    /// Manages physics fixtures.
    /// </summary>
    public sealed partial class FixtureSystem : EntitySystem
    {
        [Dependency] private readonly SharedBroadphaseSystem _broadphaseSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FixturesComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FixturesComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<FixturesComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<FixturesComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<PhysicsInitializedEvent>(OnPhysicsInit);
            SubscribeLocalEvent<PhysicsComponent, ComponentShutdown>(OnPhysicsShutdown);
        }

        private void OnShutdown(EntityUid uid, FixturesComponent component, ComponentShutdown args)
        {
            // TODO: Need a better solution to this because the only reason I don't throw is that allcomponents test
            // Yes it is actively making the game buggier but I would essentially double the size of this PR trying to fix it
            // my best solution rn is move the broadphase property onto FixturesComponent and then refactor
            // SharedBroadphaseSystem a LOT.
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                return;
            }

            // Can't just get physicscomp on shutdown as it may be touched completely independently.
            body.DestroyContacts();
            _broadphaseSystem.RemoveBody(body, component);
            body.CanCollide = false;
        }

        private void OnInit(EntityUid uid, FixturesComponent component, ComponentInit args)
        {
            // Convert the serialized list to the dictionary format as it may not necessarily have an ID in YAML
            // (probably change this someday for perf reasons?)
            foreach (var fixture in component.SerializedFixtures)
            {
                fixture.ID = GetFixtureName(component, fixture);

                if (component.Fixtures.TryAdd(fixture.ID, fixture)) continue;

                // This can happen on stuff like grids that save their fixtures to the map file.
                // Logger.DebugS("physics", $"Tried to deserialize fixture {fixture.ID} on {uid} which already exists.");
            }

            component.SerializedFixtures.Clear();

            // Can't ACTUALLY add it to the broadphase here because transform is still in a transient dimension on the 5th plane
            // hence we'll just make sure its body is set and SharedBroadphaseSystem will deal with it later.
            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                foreach (var (_, fixture) in component.Fixtures)
                {
                    fixture.Body = body;
                }

                // Make sure all the right stuff is set on the body
                FixtureUpdate(component);

                if (body.CanCollide)
                {
                    DebugTools.Assert(!Get<SharedContainerSystem>().IsEntityInContainer(uid));
                    _broadphaseSystem.AddBody(body, component);
                }
            }
            /* TODO: Literally only AllComponentsOneToOneDeleteTest fails on this so fuck it this is what we get.
            else
            {
                Logger.ErrorS("physics", $"Didn't find a {nameof(PhysicsComponuid)}"
            }
            */
        }

        #region Public

        public void CreateFixture(PhysicsComponent body, Fixture fixture, bool updates = true, FixturesComponent? manager = null, TransformComponent? xform = null)
        {
            if (!Resolve(body.Owner, ref manager, ref xform))
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
                var (worldPos, worldRot) = xform.GetWorldPositionRotation();

                _broadphaseSystem.CreateProxies(fixture, worldPos, worldRot);
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

        /// <summary>
        /// Creates a <see cref="Fixture"/> from this shape and adds it to the specified <see cref="PhysicsComponent"/>
        /// </summary>
        public Fixture CreateFixture(PhysicsComponent body, IPhysShape shape)
        {
            var fixture = new Fixture(body, shape);
            CreateFixture(body, fixture);
            return fixture;
        }

        /// <summary>
        /// Creates a <see cref="Fixture"/> from this shape and adds it to the specified <see cref="PhysicsComponent"/> with mass.
        /// </summary>
        public void CreateFixture(PhysicsComponent body, IPhysShape shape, float mass)
        {
            // TODO: Make it take in density instead?
            var fixture = new Fixture(body, shape) {Mass = mass};
            CreateFixture(body, fixture);
        }

        /// <summary>
        /// Creates a <see cref="Fixture"/> from this shape and adds it to the specified <see cref="PhysicsComponent"/> with mass.
        /// </summary>
        public void CreateFixture(PhysicsComponent body, IPhysShape shape, float mass, int collisionLayer, int collisionMask)
        {
            // TODO: Make it take in density instead?
            var fixture = new Fixture(body, shape) {
                Mass = mass,
                CollisionLayer = collisionLayer,
                CollisionMask = collisionMask
            };
            CreateFixture(body, fixture);
        }

        /// <summary>
        /// Attempts to get the <see cref="Fixture"/> with the specified ID for this body.
        /// </summary>
        public Fixture? GetFixtureOrNull(PhysicsComponent body, string id, FixturesComponent? manager = null)
        {
            if (!Resolve(body.Owner, ref manager))
            {
                return null;
            }

            return manager.Fixtures.TryGetValue(id, out var fixture) ? fixture : null;
        }

        /// <summary>
        /// Destroys the specified <see cref="Fixture"/> attached to the body.
        /// </summary>
        /// <param name="body">The specified body</param>
        /// <param name="id">The fixture ID</param>
        /// <param name="updates">Whether to update mass etc. Set false if you're doing a bulk operation</param>
        public void DestroyFixture(PhysicsComponent body, string id, bool updates = true)
        {
            var fixture = GetFixtureOrNull(body, id);

            if (fixture == null) return;

            DestroyFixture(body, fixture, updates);
        }

        /// <summary>
        /// Destroys the specified <see cref="Fixture"/>
        /// </summary>
        /// <param name="fixture">The specified fixture</param>
        /// <param name="updates">Whether to update mass etc. Set false if you're doing a bulk operation</param>
        public void DestroyFixture(Fixture fixture, bool updates = true, FixturesComponent? manager = null)
        {
            DestroyFixture(fixture.Body, fixture, updates, manager);
        }

        /// <summary>
        /// Destroys the specified <see cref="Fixture"/>
        /// </summary>
        /// <param name="updates">Whether to update mass etc. Set false if you're doing a bulk operation</param>
        public void DestroyFixture(PhysicsComponent body, Fixture fixture, bool updates = true, FixturesComponent? manager = null)
        {
            if (!Resolve(body.Owner, ref manager))
            {
                return;
            }

            // TODO: Assert world locked
            DebugTools.Assert(fixture.Body == body);
            DebugTools.Assert(manager.FixtureCount > 0);

            if (!manager.Fixtures.Remove(fixture.ID))
            {
                Logger.ErrorS("fixtures", $"Tried to remove fixture from {body.Owner} that was already removed.");
                return;
            }

            foreach (var (_, contact) in fixture.Contacts.ToArray())
            {
                body.PhysicsMap?.ContactManager.Destroy(contact);
            }

            var broadphase = body.Broadphase;

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
            if (MetaData(uid).EntityLifeStage > EntityLifeStage.MapInitialized) return;
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

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physics))
            {
                DebugTools.Assert(false);
                Logger.ErrorS("physics", $"Tried to apply fixture state for {uid} which has name {nameof(PhysicsComponent)}");
                return;
            }

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
            }

            if (computeProperties)
            {
                physics.ResetMassData();
            }
        }

        private string GetFixtureName(FixturesComponent component, Fixture fixture)
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

        /// <summary>
        /// Updates all of the cached physics information on the body derived from fixtures.
        /// </summary>
        public void FixtureUpdate(FixturesComponent component, PhysicsComponent? body = null)
        {
            if (!Resolve(component.Owner, ref body))
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

            body.ResetMassData(component);

            // Normally this method is called when fixtures need to be dirtied anyway so no point in returning early I think
            body.CollisionMask = mask;
            body.CollisionLayer = layer;
            body.Hard = hard;
            component.Dirty();
        }

        [Serializable, NetSerializable]
        private sealed class FixtureManagerComponentState : ComponentState
        {
            public List<Fixture> Fixtures = default!;
        }
    }
}
