using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    public void SetDensity(EntityUid uid, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Density.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Density = value;

        if (update)
            _fixtures.FixtureUpdate(uid, manager: manager, body: fixture.Body);
    }

    public void SetFriction(EntityUid uid, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Friction.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Friction = value;

        if (update)
            _fixtures.FixtureUpdate(uid, manager: manager, body: fixture.Body);
    }

    public void SetHard(EntityUid uid, Fixture fixture, bool value, FixturesComponent? manager = null)
    {
        if (fixture.Hard.Equals(value))
            return;

        if (!Resolve(uid, ref manager))
            return;

        fixture.Hard = value;
        _fixtures.FixtureUpdate(uid, manager: manager, body:fixture.Body);
        WakeBody(uid, body: fixture.Body);
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
            _fixtures.FixtureUpdate(uid, manager: manager, body: fixture.Body);
    }

    #region Collision Masks & Layers

    public void AddCollisionMask(EntityUid uid, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == mask) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixture.ID));
        fixture.CollisionMask |= mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(fixture);
    }

    public void SetCollisionMask(EntityUid uid, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if (fixture.CollisionMask == mask) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixture.ID));
        fixture.CollisionMask = mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(fixture);
    }

    public void RemoveCollisionMask(EntityUid uid, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionMask & mask) == 0x0) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixture.ID));
        fixture.CollisionMask &= ~mask;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(fixture);
    }

    public void AddCollisionLayer(EntityUid uid, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == layer) return;

        if (!Resolve(uid, ref manager))
            return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixture.ID));
        fixture.CollisionLayer |= layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(fixture);
    }

    public void SetCollisionLayer(EntityUid uid, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
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

        _broadphase.Refilter(fixture);
    }

    public void RemoveCollisionLayer(EntityUid uid, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)
    {
        if ((fixture.CollisionLayer & layer) == 0x0 || !Resolve(uid, ref manager)) return;

        DebugTools.Assert(manager.Fixtures.ContainsKey(fixture.ID));
        fixture.CollisionLayer &= ~layer;

        if (body != null || TryComp(uid, out body))
        {
            _fixtures.FixtureUpdate(uid, manager: manager, body: body);
        }

        _broadphase.Refilter(fixture);
    }

    #endregion
}
