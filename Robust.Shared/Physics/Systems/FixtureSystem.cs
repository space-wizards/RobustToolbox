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
            // TODO: Need a better solution to this because the only reason I don't throw is that allcomponents test
            // Yes it is actively making the game buggier but I would essentially double the size of this PR trying to fix it
            // my best solution rn is move the broadphase property onto FixturesComponent and then refactor
            // SharedBroadphaseSystem a LOT.
            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? body))
            {
                return;
            }

            // Can't just get physicscomp on shutdown as it may be touched completely independently.
            _physics.DestroyContacts(body);

            // TODO im 99% sure  _broadphaseSystem.RemoveBody(body, component) gets triggered by this as well, so is this even needed?
            _physics.SetCanCollide(uid, false, false, manager: component, body: body);
        }

        #region Public

        public bool TryCreateFixture(
            EntityUid uid,
            IPhysShape shape,
            string id,
            float density = PhysicsConstants.DefaultDensity,
            bool hard = true,
            int collisionLayer = 0,
            int collisionMask = 0,
            float friction = PhysicsConstants.DefaultContactFriction,
            float restitution = PhysicsConstants.DefaultRestitution,
            bool updates = true,
            FixturesComponent? manager = null,
            PhysicsComponent? body = null,
            TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref body, ref manager))
                return false;

            if (manager.Fixtures.ContainsKey(id))
                return false;

            var fixture = new Fixture(shape, collisionLayer, collisionMask, hard, density, friction, restitution);
            fixture.ID = id;
            CreateFixture(uid, fixture, updates, manager, body, xform);
            return true;
        }

        internal void CreateFixture(
            EntityUid uid,
            Fixture fixture,
            bool updates = true,
            FixturesComponent? manager = null,
            PhysicsComponent? body = null,
            TransformComponent? xform = null)
        {
            DebugTools.Assert(MetaData(uid).EntityLifeStage < EntityLifeStage.Terminating);

            if (!Resolve(uid, ref manager, ref body))
            {
                DebugTools.Assert(false);
                return;
            }

            EnsureFixtureId(manager, fixture);
            manager.Fixtures.Add(fixture.ID, fixture);
            fixture.Body = body;

            if (body.CanCollide && Resolve(uid, ref xform))
            {
                _lookup.CreateProxies(xform, fixture);
            }

            // Supposed to be wrapped in density but eh
            if (updates)
            {
                // Don't need to dirty here as we'll just manually call it after (we 100% need to call it).
                FixtureUpdate(uid, false, manager: manager, body: body);
                // Don't need to ResetMassData as FixtureUpdate already does it.
                Dirty(manager);
            }
            // TODO: Set newcontacts to true.
        }

        /// <summary>
        /// Attempts to get the <see cref="Fixture"/> with the specified ID for this body.
        /// </summary>
        public Fixture? GetFixtureOrNull(EntityUid uid, string id, FixturesComponent? manager = null)
        {
            if (!Resolve(uid, ref manager, false))
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
        public void DestroyFixture(
            EntityUid uid,
            string id,
            bool updates = true,
            PhysicsComponent? body = null,
            FixturesComponent? manager = null,
            TransformComponent? xform = null)
        {
            var fixture = GetFixtureOrNull(uid, id, manager);

            if (fixture == null) return;

            DestroyFixture(uid, fixture, updates, body, manager, xform);
        }

        /// <summary>
        /// Destroys the specified <see cref="Fixture"/>
        /// </summary>
        /// <param name="updates">Whether to update mass etc. Set false if you're doing a bulk operation</param>
        public void DestroyFixture(
            EntityUid uid,
            Fixture fixture,
            bool updates = true,
            PhysicsComponent? body = null,
            FixturesComponent? manager = null,
            TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref body, ref manager, ref xform))
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

            foreach (var contact in fixture.Contacts.Values.ToArray())
            {
                _physics.DestroyContact(contact);
            }

            if (_lookup.TryGetCurrentBroadphase(xform, out var broadphase))
            {
                var map = Transform(broadphase.Owner).MapUid;
                TryComp<PhysicsMapComponent>(map, out var physicsMap);
                _lookup.DestroyProxies(fixture, xform, broadphase, physicsMap);
            }

            if (updates)
                FixtureUpdate(uid, manager: manager, body: body);
        }

        #endregion

        private void OnPhysicsShutdown(EntityUid uid, PhysicsComponent component, ComponentShutdown args)
        {
            if (MetaData(uid).EntityLifeStage > EntityLifeStage.MapInitialized) return;
            EntityManager.RemoveComponent<FixturesComponent>(uid);
        }

        internal void OnPhysicsInit(EntityUid uid, FixturesComponent component, PhysicsComponent? body = null)
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
            if (Resolve(uid, ref body, false))
            {
                foreach (var fixture in component.Fixtures.Values)
                {
                    fixture.Body = body;
                }

                // Make sure all the right stuff is set on the body
                FixtureUpdate(uid, false, component, body);
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
            // FUTURE SLOTH
            // Do not touch this or I WILL GLASS YOU.
            // Updating fixtures in place causes prediction issues with contacts.
            // See PR #3431 for when this started.
            foreach (var (id, fixture) in newFixtures)
            {
                if (!component.Fixtures.TryGetValue(id, out var existing))
                {
                    toAddFixtures.Add(fixture);
                }
                else if (!existing.Equivalent(fixture))
                {
                    toRemoveFixtures.Add(existing);
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

            // TODO add a DestroyFixture() override that takes in a list.
            // reduced broadphase lookups
            foreach (var fixture in toRemoveFixtures)
            {
                computeProperties = true;
                DestroyFixture(uid, fixture, false, physics, component);
            }

            // TODO: We also still need event listeners for shapes (Probably need C# events)
            // Or we could just make it so shapes can only be updated via fixturesystem which handles it
            // automagically (friends or something?)
            foreach (var fixture in toAddFixtures)
            {
                computeProperties = true;
                CreateFixture(uid, fixture, false, component, physics, xform);
            }

            if (computeProperties)
            {
                FixtureUpdate(uid, manager: component, body: physics);
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

        #region Restitution

        public void SetRestitution(EntityUid uid, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
        {
            fixture.Restitution = value;
            if (update && Resolve(uid, ref manager))
                FixtureUpdate(uid, manager: manager);
        }

        #endregion

        /// <summary>
        /// Updates all of the cached physics information on the body derived from fixtures.
        /// </summary>
        public void FixtureUpdate(EntityUid uid, bool dirty = true, FixturesComponent? manager = null, PhysicsComponent? body = null)
        {
            if (!Resolve(uid, ref body, ref manager))
                return;

            var mask = 0;
            var layer = 0;
            var hard = false;

            foreach (var fixture in manager.Fixtures.Values)
            {
                mask |= fixture.CollisionMask;
                layer |= fixture.CollisionLayer;
                hard |= fixture.Hard;
            }

            _physics.ResetMassData(uid, manager, body);

            // Normally this method is called when fixtures need to be dirtied anyway so no point in returning early I think
            body.CollisionMask = mask;
            body.CollisionLayer = layer;
            body.Hard = hard;

            if (manager.FixtureCount == 0)
                _physics.SetCanCollide(uid, false, manager: manager, body: body);

            if (dirty)
                Dirty(manager);
        }

        public int GetFixtureCount(EntityUid uid, FixturesComponent? manager = null)
        {
            if (!Resolve(uid, ref manager, false))
            {
                return 0;
            }

            return manager.FixtureCount;
        }

        [Serializable, NetSerializable]
        private sealed class FixtureManagerComponentState : ComponentState
        {
            public Fixture[] Fixtures = default!;
        }
    }
}
