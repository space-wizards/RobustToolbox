using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
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
        [Dependency] private readonly IGameTiming _timing = default!;
        private EntityQuery<PhysicsMapComponent> _mapQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<FixturesComponent> _fixtureQuery;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FixturesComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<FixturesComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<FixturesComponent, ComponentHandleState>(OnHandleState);
            _mapQuery = GetEntityQuery<PhysicsMapComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();
            _fixtureQuery = GetEntityQuery<FixturesComponent>();
        }

        private void OnShutdown(EntityUid uid, FixturesComponent component, ComponentShutdown args)
        {
            // TODO: Need a better solution to this because the only reason I don't throw is that allcomponents test
            // Yes it is actively making the game buggier but I would essentially double the size of this PR trying to fix it
            // my best solution rn is move the broadphase property onto FixturesComponent and then refactor
            // SharedBroadphaseSystem a LOT.
            if (!_physicsQuery.TryGetComponent(uid, out var body))
                return;

            // Can't just get physicscomp on shutdown as it may be touched completely independently.
            _physics.DestroyContacts(body);
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
            if (!_physicsQuery.Resolve(uid, ref body) || !_fixtureQuery.Resolve(uid, ref manager))
                return false;

            if (manager.Fixtures.ContainsKey(id))
                return false;

            var fixture = new Fixture(shape, collisionLayer, collisionMask, hard, density, friction, restitution);
            CreateFixture(uid, id, fixture, updates, manager, body, xform);
            return true;
        }

        internal void CreateFixture(
            EntityUid uid,
            string fixtureId,
            Fixture fixture,
            bool updates = true,
            FixturesComponent? manager = null,
            PhysicsComponent? body = null,
            TransformComponent? xform = null)
        {
            DebugTools.Assert(MetaData(uid).EntityLifeStage < EntityLifeStage.Terminating);

            if (!_physicsQuery.Resolve(uid, ref body) || !_fixtureQuery.Resolve(uid, ref manager))
            {
                DebugTools.Assert(false);
                return;
            }

            if (string.IsNullOrEmpty(fixtureId))
            {
                throw new InvalidOperationException($"Tried to create a fixture without an ID!");
            }

            manager.Fixtures.Add(fixtureId, fixture);
            fixture.Owner = uid;

            if (body.CanCollide && Resolve(uid, ref xform))
            {
                _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
            }

            // Supposed to be wrapped in density but eh
            if (updates)
            {
                // Don't need to dirty here as we'll just manually call it after (we 100% need to call it).
                FixtureUpdate(uid, false, manager: manager, body: body);
                // Don't need to ResetMassData as FixtureUpdate already does it.
                Dirty(uid, manager);
            }

            // TODO: Set newcontacts to true.
        }

        /// <summary>
        /// Attempts to get the <see cref="Fixture"/> with the specified ID for this body.
        /// </summary>
        public Fixture? GetFixtureOrNull(EntityUid uid, string id, FixturesComponent? manager = null)
        {
            if (!_fixtureQuery.Resolve(uid, ref manager))
                return null;

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
            if (!_fixtureQuery.Resolve(uid, ref manager))
                return;

            var fixture = GetFixtureOrNull(uid, id, manager);
            if (fixture != null)
                DestroyFixture(uid, id, fixture, updates, body, manager, xform);
        }

        /// <summary>
        /// Destroys the specified <see cref="Fixture"/>
        /// </summary>
        /// <param name="updates">Whether to update mass etc. Set false if you're doing a bulk operation</param>
        public void DestroyFixture(
            EntityUid uid,
            string fixtureId,
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
            DebugTools.Assert(manager.FixtureCount > 0);

            if (!manager.Fixtures.Remove(fixtureId))
            {
                Log.Error($"Tried to remove fixture from {ToPrettyString(uid)} that was already removed.");
                return;
            }

            // Temporary debug block for trying to help catch a bug where grid fixtures disappear without the chunk's
            // fixture set being updated
#if DEBUG
            if (TryComp(uid, out MapGridComponent? grid) && !_timing.ApplyingState)
            {
                foreach (var chunk in grid.Chunks.Values)
                {
                    DebugTools.Assert(!chunk.Fixtures.Contains(fixtureId), $"A grid fixture is being deleted without first removing it from the chunk. Please report this bug.");
                }
            }
#endif

            foreach (var contact in fixture.Contacts.Values.ToArray())
            {
                _physics.DestroyContact(contact);
            }

            if (_lookup.TryGetCurrentBroadphase(xform, out var broadphase))
            {
                DebugTools.Assert(xform.MapUid == Transform(broadphase.Owner).MapUid);
                _mapQuery.TryGetComponent(xform.MapUid, out var physicsMap);
                _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            }

            if (updates)
            {
                var resetMass = fixture.Density > 0f;
                FixtureUpdate(uid, resetMass: resetMass, manager: manager, body: body);
            }
        }

        #endregion

        internal void OnPhysicsInit(EntityUid uid, FixturesComponent component, PhysicsComponent? body = null)
        {
            // Can't ACTUALLY add it to the broadphase here because transform is still in a transient dimension on the 5th plane
            // hence we'll just make sure its body is set and SharedBroadphaseSystem will deal with it later.
            if (Resolve(uid, ref body, false))
            {
                foreach (var (id, fixture) in component.Fixtures)
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        throw new InvalidOperationException($"Tried to setup fixture on init for {ToPrettyString(uid)} with no ID!");
                    }

                    fixture.Owner = uid;
                }

                // Make sure all the right stuff is set on the body
                FixtureUpdate(uid, dirty: false, manager: component, body: body);
            }
        }

        private void OnGetState(EntityUid uid, FixturesComponent component, ref ComponentGetState args)
        {
            var copied = new Dictionary<string, Fixture>(component.Fixtures.Count);

            foreach (var (id, fixture) in component.Fixtures)
            {
                var copy = new Fixture();
                fixture.CopyTo(copy);
                copied[id] = copy;
            }

            args.State = new FixtureManagerComponentState()
            {
                Fixtures = copied,
            };
        }

        private void OnHandleState(EntityUid uid, FixturesComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not FixtureManagerComponentState state)
                return;

            if (!EntityManager.TryGetComponent(uid, out PhysicsComponent? physics))
            {
                Log.Error($"Tried to apply fixture state for an entity without physics: {ToPrettyString(uid)}");
                return;
            }

            var toAddFixtures = new ValueList<(string Id, Fixture Fixture)>();
            var toRemoveFixtures = new ValueList<(string Id, Fixture Fixture)>();
            var computeProperties = false;

            // Given a bunch of data isn't serialized need to sort of re-initialise it
            var newFixtures = new Dictionary<string, Fixture>(state.Fixtures.Count);

            foreach (var (id, fixture) in state.Fixtures)
            {
                var newFixture = new Fixture();
                fixture.CopyTo(newFixture);
                newFixtures.Add(id, newFixture);
                newFixture.Owner = uid;
            }

            TransformComponent? xform = null;
            var regenerate = false;

            // Add / update new fixtures
            // FUTURE SLOTH
            // Do not touch this or I WILL GLASS YOU.
            // Updating fixtures in place causes prediction issues with contacts.
            // See PR #3431 for when this started.
            foreach (var (id, fixture) in newFixtures)
            {
                if (!component.Fixtures.TryGetValue(id, out var existing))
                {
                    toAddFixtures.Add((id, fixture));
                }
                // Retained fixture but new data
                else if (!existing.Equivalent(fixture))
                {
                    fixture.CopyTo(existing);
                    computeProperties = true;
                    regenerate = true;
                }
            }

            // Remove old fixtures
            foreach (var (existingId, existing) in component.Fixtures)
            {
                if (!newFixtures.ContainsKey(existingId))
                {
                    toRemoveFixtures.Add((existingId, existing));
                }
            }

            // TODO add a DestroyFixture() override that takes in a list.
            // reduced broadphase lookups
            foreach (var (id, fixture) in toRemoveFixtures.Span)
            {
                computeProperties = true;
                DestroyFixture(uid, id, fixture, false, physics, component);
            }

            // TODO: We also still need event listeners for shapes (Probably need C# events)
            // Or we could just make it so shapes can only be updated via fixturesystem which handles it
            // automagically (friends or something?)
            foreach (var (id, fixture) in toAddFixtures.Span)
            {
                computeProperties = true;
                CreateFixture(uid, id, fixture, false, component, physics, xform);
            }

            if (computeProperties)
            {
                FixtureUpdate(uid, manager: component, body: physics);
            }

            if (regenerate)
            {
                _broadphase.RegenerateContacts((uid, physics, component, xform));
            }
        }

        #region Restitution

        public void SetRestitution(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
        {
            fixture.Restitution = value;
            if (update && Resolve(uid, ref manager))
                FixtureUpdate(uid, manager: manager);
        }

        #endregion

        /// <summary>
        /// Updates all of the cached physics information on the body derived from fixtures.
        /// </summary>
        public void FixtureUpdate(EntityUid uid, bool dirty = true, bool resetMass = true, FixturesComponent? manager = null, PhysicsComponent? body = null)
        {
            if (!_physicsQuery.Resolve(uid, ref body) || !_fixtureQuery.Resolve(uid, ref manager))
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

            if (resetMass)
                _physics.ResetMassData(uid, manager, body);

            // Save the old layer to see if an event should be raised later.
            var oldLayer = body.CollisionLayer;

            // Normally this method is called when fixtures need to be dirtied anyway so no point in returning early I think
            body.CollisionMask = mask;
            body.CollisionLayer = layer;
            body.Hard = hard;

            if (manager.FixtureCount == 0)
                _physics.SetCanCollide(uid, false, manager: manager, body: body);

            if (oldLayer != layer)
            {
                var ev = new CollisionLayerChangeEvent((uid, body));
                RaiseLocalEvent(ref ev);
            }

            if (dirty)
                Dirty(uid, manager);
        }

        public int GetFixtureCount(EntityUid uid, FixturesComponent? manager = null)
        {
            if (!_fixtureQuery.Resolve(uid, ref manager))
            {
                return 0;
            }

            return manager.FixtureCount;
        }

        [Serializable, NetSerializable]
        private sealed class FixtureManagerComponentState : ComponentState
        {
            public Dictionary<string, Fixture> Fixtures = default!;
        }
    }
}
