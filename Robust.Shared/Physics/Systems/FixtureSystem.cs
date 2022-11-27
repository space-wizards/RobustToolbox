using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems
{
    /// <summary>
    /// Manages physics fixtures.
    /// </summary>
    public sealed partial class FixtureSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FixturesComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<FixturesComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<FixturesComponent, ComponentHandleState>(OnHandleState);

            SubscribeLocalEvent<PhysicsComponent, ComponentShutdown>(OnPhysicsShutdown);
        }

        private void OnShutdown(EntityUid uid, FixturesComponent component, ComponentShutdown args)
        {
            var xform = Transform(uid);
            if (xform.MapID == Map.MapId.Nullspace)
                return;

            // TODO: Need a better solution to this because the only reason I don't throw is that allcomponents test
            // Yes it is actively making the game buggier but I would essentially double the size of this PR trying to fix it
            // my best solution rn is move the broadphase property onto FixturesComponent and then refactor
            // SharedBroadphaseSystem a LOT.
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                return;
            }

            // Can't just get physicscomp on shutdown as it may be touched completely independently.
            _physics.DestroyContacts(body, xform.MapID, xform);

            // TODO im 99% sure  _broadphaseSystem.RemoveBody(body, component) gets triggered by this as well, so is this even needed?
            _physics.SetCanCollide(body, false);
        }
        #region Public

        /// <summary>
        ///     Attempts to add a new fixture. Will do nothing if a fixture with the requested ID already exists.
        /// </summary>
        public bool TryCreateFixture(PhysicsComponent body, Fixture fixture, bool updates = true, FixturesComponent? manager = null, TransformComponent? xform = null)
        {
            if (!Resolve(body.Owner, ref manager, ref xform, false))
                return false;

            if (!string.IsNullOrEmpty(fixture.ID) && manager.Fixtures.ContainsKey(fixture.ID))
                return false;

            CreateFixture(body, fixture, updates, manager, xform);
            return true;
        }

        public void CreateFixture(PhysicsComponent body, Fixture fixture, bool updates = true, FixturesComponent? manager = null, TransformComponent? xform = null)
        {
            DebugTools.Assert(MetaData(body.Owner).EntityLifeStage < EntityLifeStage.Terminating);

            if (!Resolve(body.Owner, ref manager, ref xform))
            {
                DebugTools.Assert(false);
                return;
            }

            EnsureFixtureId(manager, fixture);
            manager.Fixtures.Add(fixture.ID, fixture);
            fixture.Body = body;

            if (body.CanCollide)
            {
                _lookup.CreateProxies(xform, fixture);
            }

            // Supposed to be wrapped in density but eh
            if (updates)
            {
                FixtureUpdate(manager, body);
                _physics.ResetMassData(manager, body);
                Dirty(manager);
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
        public void CreateFixture(PhysicsComponent body, IPhysShape shape, float density)
        {
            // TODO: Make it take in density instead?
            var fixture = new Fixture(body, shape) {Density = density};
            CreateFixture(body, fixture);
        }

        /// <summary>
        /// Creates a <see cref="Fixture"/> from this shape and adds it to the specified <see cref="PhysicsComponent"/> with mass.
        /// </summary>
        public void CreateFixture(PhysicsComponent body, IPhysShape shape, float density, int collisionLayer, int collisionMask)
        {
            var fixture = new Fixture(body, shape)
            {
                Density = density
            };
            FixturesComponent? manager = null;

            _physics.SetCollisionLayer(fixture, collisionLayer, manager);
            _physics.SetCollisionMask(fixture, collisionMask, manager);
            CreateFixture(body, fixture);
        }

        /// <summary>
        /// Attempts to get the <see cref="Fixture"/> with the specified ID for this body.
        /// </summary>
        public Fixture? GetFixtureOrNull(PhysicsComponent body, string id, FixturesComponent? manager = null)
        {
            if (!Resolve(body.Owner, ref manager, false))
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

            var xform = Transform(body.Owner);
            var map = xform.MapUid;

            if (TryComp<SharedPhysicsMapComponent>(map, out var physicsMap))
            {
                foreach (var (_, contact) in fixture.Contacts.ToArray())
                {
                    physicsMap.ContactManager.Destroy(contact);
                }

                if (body.CanCollide && xform.GridUid != xform.Owner)
                {
                    _lookup.DestroyProxies(fixture, xform, physicsMap);
                }
            }

            if (updates)
                FixtureUpdate(manager, body);
        }

        #endregion

        private void OnPhysicsShutdown(EntityUid uid, PhysicsComponent component, ComponentShutdown args)
        {
            if (MetaData(uid).EntityLifeStage > EntityLifeStage.MapInitialized) return;
            EntityManager.RemoveComponent<FixturesComponent>(uid);
        }

        internal void OnPhysicsInit(EntityUid uid, FixturesComponent component)
        {
            // Convert the serialized list to the dictionary format as it may not necessarily have an ID in YAML
            // (probably change this someday for perf reasons?)
            foreach (var fixture in component.SerializedFixtures)
            {
                EnsureFixtureId(component, fixture);

                if (component.Fixtures.TryAdd(fixture.ID, fixture)) continue;

                // This can happen on stuff like grids that save their fixtures to the map file.
                // Logger.DebugS("physics", $"Tried to deserialize fixture {fixture.ID} on {uid} which already exists.");
            }

            component.SerializedFixtureData = null;

            // Can't ACTUALLY add it to the broadphase here because transform is still in a transient dimension on the 5th plane
            // hence we'll just make sure its body is set and SharedBroadphaseSystem will deal with it later.
            if (EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                foreach (var fixture in component.Fixtures.Values)
                {
                    fixture.Body = body;
                }

                // Make sure all the right stuff is set on the body
                FixtureUpdate(component, body, false);
            }
        }

        private void OnGetState(EntityUid uid, FixturesComponent component, ref ComponentGetState args)
        {
            args.State = new FixtureManagerComponentState
            {
                Fixtures = component.Fixtures.Values.ToArray(),
            };
        }

        private void OnHandleState(EntityUid uid, FixturesComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not FixtureManagerComponentState state) return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physics))
            {
                Logger.ErrorS("physics", $"Tried to apply fixture state for an entity without physics: {ToPrettyString(uid)}");
                return;
            }

            component.SerializedFixtureData = null;
            var toAddFixtures = new ValueList<Fixture>();
            var toRemoveFixtures = new ValueList<Fixture>();
            var computeProperties = false;

            // Given a bunch of data isn't serialized need to sort of re-initialise it
            var newFixtures = new Dictionary<string, Fixture>(state.Fixtures.Length);

            for (var i = 0; i < state.Fixtures.Length; i++)
            {
                var fixture = state.Fixtures[i];
                var newFixture = new Fixture();
                fixture.CopyTo(newFixture);
                newFixture.Body = physics;
                newFixtures.Add(newFixture.ID, newFixture);
            }

            TransformComponent? xform = null;

            // Add / update new fixtures
            foreach (var (id, fixture) in newFixtures)
            {
                if (component.Fixtures.TryGetValue(id, out var existing))
                {
                    if (!existing.Equivalent(fixture))
                    {
                        fixture.CopyTo(existing);
                        computeProperties = true;
                        _broadphase.Refilter(existing, xform);
                    }
                }
                else
                {
                    toAddFixtures.Add(fixture);
                }
            }

            // Remove old fixtures
            foreach (var (existingId, existing) in component.Fixtures)
            {
                if (!newFixtures.ContainsKey(existingId))
                {
                    toRemoveFixtures.Add(existing);
                }
            }

            foreach (var fixture in toRemoveFixtures)
            {
                computeProperties = true;
                DestroyFixture(physics, fixture, false);
            }

            // TODO: We also still need event listeners for shapes (Probably need C# events)
            // Or we could just make it so shapes can only be updated via fixturesystem which handles it
            // automagically (friends or something?)
            foreach (var fixture in toAddFixtures)
            {
                computeProperties = true;
                CreateFixture(physics, fixture, false);
            }

            if (computeProperties)
            {
                FixtureUpdate(component, physics);
            }
        }

        /// <summary>
        ///     Return the fixture's id if it has one. Otherwise, this automatically generates a fixture id and stores it.
        /// </summary>
        private string EnsureFixtureId(FixturesComponent component, Fixture fixture)
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
                    fixture.AutoGeneratedId = name;
                    return name;
                }
            }
        }

        #region Mass

        [Obsolete("Use Density")]
        public void SetMass(Fixture fixture, float value, FixturesComponent? manager = null)
        {
            var area = fixture.Area;
            var density = area / value;
            _physics.SetDensity(fixture, density, manager);
        }

        #endregion

        #region Restitution

        public void SetRestitution(Fixture fixture, float value, FixturesComponent? manager = null, bool update = true)
        {
            fixture._restitution = value;
            if (update && Resolve(fixture.Body.Owner, ref manager))
                FixtureUpdate(manager);
        }

        #endregion

        public void FixtureUpdate(Fixture fixture, FixturesComponent? fixturesComponent = null)
        {
            if (!Resolve(fixture.Body.Owner, ref fixturesComponent))
                return;

            FixtureUpdate(fixturesComponent, fixture.Body);
        }

        /// <summary>
        /// Updates all of the cached physics information on the body derived from fixtures.
        /// </summary>
        public void FixtureUpdate(FixturesComponent component, PhysicsComponent? body = null, bool dirty = true)
        {
            if (!Resolve(component.Owner, ref body))
                return;

            var mask = 0;
            var layer = 0;
            var hard = false;

            foreach (var (_, fixture) in component.Fixtures)
            {
                mask |= fixture.CollisionMask;
                layer |= fixture.CollisionLayer;
                hard |= fixture.Hard;
            }

            _physics.ResetMassData(component, body);

            // Normally this method is called when fixtures need to be dirtied anyway so no point in returning early I think
            body.CollisionMask = mask;
            body.CollisionLayer = layer;
            body.Hard = hard;
            if (component.FixtureCount == 0)
                _physics.SetCanCollide(body, false);

            if (dirty)
                Dirty(component);
        }

        public int GetFixtureCount(EntityUid uid, FixturesComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                return 0;
            }

            return component.FixtureCount;
        }

        [Serializable, NetSerializable]
        private sealed class FixtureManagerComponentState : ComponentState
        {
            public Fixture[] Fixtures = default!;
        }
    }
}
