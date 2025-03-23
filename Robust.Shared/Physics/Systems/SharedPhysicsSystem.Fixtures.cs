using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    public void SetDensity(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Density.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Density = value;

        if (update)
            _fixtures.FixtureUpdate(uid, manager: manager);
    }

    public void SetFriction(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Friction.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Friction = value;

        if (update)
            _fixtures.FixtureUpdate(uid, manager: manager);
    }

    public void SetHard(EntityUid uid, Fixture fixture, bool value, FixturesComponent? manager = null)
    {
        if (fixture.Hard.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Hard = value;
        _fixtures.FixtureUpdate(uid, manager: manager);
        WakeBody(uid);
    }

    public void SetRestitution(EntityUid uid, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Restitution.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Restitution = value;

        if (update)
            _fixtures.FixtureUpdate(uid, manager: manager);
    }

    #region Collision Masks & Layers

    /// <summary>
    /// Similar to IsHardCollidable but also checks whether both entities are set to CanCollide
    /// </summary>
    public bool IsCurrentlyHardCollidable(Entity<FixturesComponent?, PhysicsComponent?> bodyA, Entity<FixturesComponent?, PhysicsComponent?> bodyB)
    {
        if (!_fixturesQuery.Resolve(bodyA, ref bodyA.Comp1, false) ||
            !_fixturesQuery.Resolve(bodyB, ref bodyB.Comp1, false) ||
            !PhysicsQuery.Resolve(bodyA, ref bodyA.Comp2, false) ||
            !PhysicsQuery.Resolve(bodyB, ref bodyB.Comp2, false))
        {
            return false;
        }

        if (!bodyA.Comp2.CanCollide ||
            !bodyB.Comp2.CanCollide)
        {
            return false;
        }

        return IsHardCollidable(bodyA, bodyB);
    }

    /// <summary>
    /// Returns true if both entities are hard-collidable with each other.
    /// </summary>
    public bool IsHardCollidable(Entity<FixturesComponent?, PhysicsComponent?> bodyA, Entity<FixturesComponent?, PhysicsComponent?> bodyB)
    {
        if (!_fixturesQuery.Resolve(bodyA, ref bodyA.Comp1, false) ||
            !_fixturesQuery.Resolve(bodyB, ref bodyB.Comp1, false) ||
            !PhysicsQuery.Resolve(bodyA, ref bodyA.Comp2, false) ||
            !PhysicsQuery.Resolve(bodyB, ref bodyB.Comp2, false))
        {
            return false;
        }

        // Fast check
        if (!bodyA.Comp2.Hard ||
            !bodyB.Comp2.Hard ||
            ((bodyA.Comp2.CollisionLayer & bodyB.Comp2.CollisionMask) == 0x0 &&
            (bodyA.Comp2.CollisionMask & bodyB.Comp2.CollisionLayer) == 0x0))
        {
            return false;
        }

        // Slow check
        foreach (var fix in bodyA.Comp1.Fixtures.Values)
        {
            if (!fix.Hard)
                continue;

            foreach (var other in bodyB.Comp1.Fixtures.Values)
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

    public void AddCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == mask) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask |= mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void SetCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (fixture.CollisionMask == mask) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask = mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void RemoveCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == 0x0) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionMask &= ~mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void AddCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == layer) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionLayer |= layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void SetCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (fixture.CollisionLayer.Equals(layer))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.CollisionLayer = layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    public void RemoveCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == 0x0 || !Resolve(uid, ref manager)) return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixtureId));
        fixture.CollisionLayer &= ~layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(uid, fixture);
    }

    #endregion
}
