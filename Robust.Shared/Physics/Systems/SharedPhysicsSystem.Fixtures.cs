using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
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
            PhysicsComponent? body = null,
            TransformComponent? xform = null)
        {
            if (!PhysicsQuery.Resolve(uid, ref body))
                return false;

            if (body.Fixtures.ContainsKey(id))
                return false;

            var fixture = new Fixture(shape, collisionLayer, collisionMask, hard, density, friction, restitution);
            CreateFixture(uid, id, fixture, updates, body, xform);
            return true;
        }

        internal void CreateFixture(
            EntityUid uid,
            string fixtureId,
            Fixture fixture,
            bool updates = true,
            PhysicsComponent? body = null,
            TransformComponent? xform = null)
        {
            DebugTools.Assert(MetaData(uid).EntityLifeStage < EntityLifeStage.Terminating);

            if (!PhysicsQuery.Resolve(uid, ref body))
            {
                DebugTools.Assert(false);
                return;
            }

            if (string.IsNullOrEmpty(fixtureId))
            {
                throw new InvalidOperationException($"Tried to create a fixture without an ID!");
            }

            body.Fixtures.Add(fixtureId, fixture);
            fixture.Owner = uid;

            if (body.CanCollide && Resolve(uid, ref xform))
            {
                _lookup.CreateProxies(uid, fixtureId, fixture, xform, body);
            }

            // Supposed to be wrapped in density but eh
            if (updates)
            {
                // Don't need to dirty here as we'll just manually call it after (we 100% need to call it).
                FixtureUpdate(uid, false, body: body);
                // Don't need to ResetMassData as FixtureUpdate already does it.
                Dirty(uid, body);
            }

            // TODO: Set newcontacts to true.
        }

        /// <summary>
        /// Attempts to get the <see cref="Fixture"/> with the specified ID for this body.
        /// </summary>
        public Fixture? GetFixtureOrNull(EntityUid uid, string id, PhysicsComponent? body = null)
        {
            if (!PhysicsQuery.Resolve(uid, ref body))
                return null;

            return body.Fixtures.GetValueOrDefault(id);
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
            TransformComponent? xform = null)
        {
            if (!PhysicsQuery.Resolve(uid, ref body))
                return;

            var fixture = GetFixtureOrNull(uid, id);
            if (fixture != null)
                DestroyFixture(uid, id, fixture, updates, body, xform);
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
            TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref body, ref xform))
            {
                return;
            }

            // TODO: Assert world locked
            DebugTools.Assert(body.Fixtures.Count > 0);

            if (!body.Fixtures.Remove(fixtureId))
            {
                Log.Error($"Tried to remove fixture from {ToPrettyString(uid)} that was already removed.");
                return;
            }

            foreach (var contact in fixture.Contacts.Values.ToArray())
            {
                DestroyContact(contact);
            }

            if (_lookup.TryGetCurrentBroadphase(xform, out var broadphase))
            {
                DebugTools.Assert(xform.MapUid == Transform(broadphase.Owner).MapUid);
                PhysMapQuery.TryGetComponent(xform.MapUid, out var physicsMap);
                _lookup.DestroyProxies(uid, fixtureId, fixture, xform, broadphase, physicsMap);
            }

            if (updates)
            {
                var resetMass = fixture.Density > 0f;
                FixtureUpdate(uid, resetMass: resetMass, body: body);
            }
        }

    /// <summary>
    /// Updates all of the cached physics information on the body derived from fixtures.
    /// </summary>
    public void FixtureUpdate(EntityUid uid, bool dirty = true, bool resetMass = true, PhysicsComponent? body = null)
    {
        if (!PhysicsQuery.Resolve(uid, ref body))
            return;

        var mask = 0;
        var layer = 0;
        var hard = false;

        foreach (var fixture in body.Fixtures.Values)
        {
            mask |= fixture.CollisionMask;
            layer |= fixture.CollisionLayer;
            hard |= fixture.Hard;
        }

        if (resetMass)
            ResetMassData(uid, body);

        // Save the old layer to see if an event should be raised later.
        var oldLayer = body.CollisionLayer;

        // Normally this method is called when fixtures need to be dirtied anyway so no point in returning early I think
        body.CollisionMask = mask;
        body.CollisionLayer = layer;
        body.Hard = hard;

        if (body.Fixtures.Count == 0)
            SetCanCollide(uid, false, body: body);

        if (oldLayer != layer)
        {
            var ev = new CollisionLayerChangeEvent((uid, body));
            RaiseLocalEvent(ref ev);
        }

        if (dirty)
            Dirty(uid, body);
    }

    public void SetDensity(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, PhysicsComponent? body = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Density.Equals(value))
            return;

        fixture.Density = value;

        if (update)
            FixtureUpdate(uid, body: body);
    }

    public void SetFriction(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, PhysicsComponent? body = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Friction.Equals(value))
            return;

        fixture.Friction = value;

        if (update)
            FixtureUpdate(uid, body: body);
    }

    public void SetHard(EntityUid uid, Fixture fixture, bool value, PhysicsComponent? body = null)
    {
        if (fixture.Hard.Equals(value))
            return;

        fixture.Hard = value;
        FixtureUpdate(uid, body: body);
        WakeBody(uid, body: body);
    }

    public void SetRestitution(EntityUid uid, Fixture fixture, float value, bool update = true, PhysicsComponent? body = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Restitution.Equals(value))
            return;

        fixture.Restitution = value;

        if (update)
            FixtureUpdate(uid, body: body);
    }

    #region Collision Masks & Layers

    /// <summary>
    /// Similar to IsHardCollidable but also checks whether both entities are set to CanCollide
    /// </summary>
    public bool IsCurrentlyHardCollidable(Entity<PhysicsComponent?> bodyA, Entity<PhysicsComponent?> bodyB)
    {
        if (!PhysicsQuery.Resolve(bodyA, ref bodyA.Comp, false) ||
            !PhysicsQuery.Resolve(bodyB, ref bodyB.Comp, false))
        {
            return false;
        }

        if (!bodyA.Comp.CanCollide ||
            !bodyB.Comp.CanCollide)
        {
            return false;
        }

        return IsHardCollidable(bodyA, bodyB);
    }

    /// <summary>
    /// Returns true if both entities are hard-collidable with each other.
    /// </summary>
    public bool IsHardCollidable(Entity<PhysicsComponent?> bodyA, Entity<PhysicsComponent?> bodyB)
    {
        if (!PhysicsQuery.Resolve(bodyA, ref bodyA.Comp, false) ||
            !PhysicsQuery.Resolve(bodyB, ref bodyB.Comp, false))
        {
            return false;
        }

        // Fast check
        if (!bodyA.Comp.Hard ||
            !bodyB.Comp.Hard ||
            ((bodyA.Comp.CollisionLayer & bodyB.Comp.CollisionMask) == 0x0 &&
            (bodyA.Comp.CollisionMask & bodyB.Comp.CollisionLayer) == 0x0))
        {
            return false;
        }

        // Slow check
        foreach (var fix in bodyA.Comp.Fixtures.Values)
        {
            if (!fix.Hard)
                continue;

            foreach (var other in bodyB.Comp.Fixtures.Values)
            {
                if (!other.Hard)
                    continue;

                if ((fix.CollisionLayer & other.CollisionMask) == 0x0 &&
                    (fix.CollisionMask & other.CollisionLayer) == 0x0)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    public void AddCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == mask) return;

        if (!PhysicsQuery.Resolve(uid, ref body))
            return;

        DebugTools.Assert(body.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask |= mask;

        FixtureUpdate(uid, body: body);
        _broadphase.Refilter(uid, fixture);
    }

    public void SetCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, PhysicsComponent? body = null)
    {
        if (fixture.CollisionMask == mask) return;

        if (!PhysicsQuery.Resolve(uid, ref body))
            return;

        DebugTools.Assert(body.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask = mask;

        FixtureUpdate(uid, body: body);
        _broadphase.Refilter(uid, fixture);
    }

    public void RemoveCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == 0x0) return;

        if (!PhysicsQuery.Resolve(uid, ref body))
            return;

        DebugTools.Assert(body.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask &= ~mask;

        FixtureUpdate(uid, body: body);
        _broadphase.Refilter(uid, fixture);
    }

    public void AddCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == layer) return;

        if (!PhysicsQuery.Resolve(uid, ref body))
            return;

        DebugTools.Assert(body.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionLayer |= layer;

        FixtureUpdate(uid, body: body);
        _broadphase.Refilter(uid, fixture);
    }

    public void SetCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, PhysicsComponent? body = null)
    {
        if (fixture.CollisionLayer.Equals(layer))
            return;

        if (!PhysicsQuery.Resolve(uid, ref body))
            return;

        fixture.CollisionLayer = layer;

        FixtureUpdate(uid, body: body);
        _broadphase.Refilter(uid, fixture);
    }

    public void RemoveCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == 0x0 || !PhysicsQuery.Resolve(uid, ref body))
            return;

        DebugTools.Assert(body.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionLayer &= ~layer;

        FixtureUpdate(uid, body: body);
        _broadphase.Refilter(uid, fixture);
    }

    #endregion
}
