using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;

    public void SetDensity(Fixture fixture, float value, FixturesComponent? fixtures = null, bool update = true)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture.Density.Equals(value))
            return;

        if (!Resolve(fixture.Body.Owner, ref fixtures))
            return;

        fixture.Density = value;

        if (update)
            _fixtures.FixtureUpdate(fixtures, fixture.Body);
    }

    public void SetFriction(Fixture fixture, float value, FixturesComponent? fixtures = null, bool update = true)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture._friction.Equals(value))
            return;

        if (!Resolve(fixture.Body.Owner, ref fixtures))
            return;

        fixture._friction = value;

        if (update)
            _fixtures.FixtureUpdate(fixtures, fixture.Body);
    }

    public void SetHard(Fixture fixture, bool value, FixturesComponent? fixtures = null)
    {
        if (fixture.Hard.Equals(value))
            return;

        if (!Resolve(fixture.Body.Owner, ref fixtures))
            return;

        fixture.Hard = value;
        _fixtures.FixtureUpdate(fixtures, fixture.Body);
        WakeBody(fixture.Body);
    }

    public void SetRestitution(Fixture fixture, float value, FixturesComponent? fixtures = null, bool update = true)
    {
        DebugTools.Assert(value >= 0f);

        if (fixture._restitution.Equals(value))
            return;

        if (!Resolve(fixture.Body.Owner, ref fixtures))
            return;

        fixture._restitution = value;

        if (update)
            _fixtures.FixtureUpdate(fixtures, fixture.Body);
    }

    #region Collision Masks & Layers

    public void AddCollisionMask(FixturesComponent component, Fixture fixture, int mask)
    {
        if ((fixture.CollisionMask & mask) == mask) return;

        DebugTools.Assert(component.Fixtures.ContainsKey(fixture.ID));
        fixture._collisionMask |= mask;

        if (TryComp<PhysicsComponent>(component.Owner, out var body))
        {
            _fixtures.FixtureUpdate(component, body);
        }

        _broadphase.Refilter(fixture);
    }

    public void SetCollisionMask(FixturesComponent component, Fixture fixture, int mask)
    {
        if (fixture.CollisionMask == mask) return;

        DebugTools.Assert(component.Fixtures.ContainsKey(fixture.ID));
        fixture._collisionMask = mask;

        if (TryComp<PhysicsComponent>(component.Owner, out var body))
        {
            _fixtures.FixtureUpdate(component, body);
        }

        _broadphase.Refilter(fixture);
    }

    public void SetCollisionMask(Fixture fixture, int mask, FixturesComponent? fixturesComponent = null)
    {
        if (fixture._collisionMask.Equals(mask))
            return;

        if (!Resolve(fixture.Body.Owner, ref fixturesComponent))
            return;

        fixture._collisionMask = mask;
        _fixtures.FixtureUpdate(fixturesComponent, fixture.Body);
        _broadphase.Refilter(fixture);
    }

    public void RemoveCollisionMask(FixturesComponent component, Fixture fixture, int mask)
    {
        if ((fixture.CollisionMask & mask) == 0x0) return;

        DebugTools.Assert(component.Fixtures.ContainsKey(fixture.ID));
        fixture._collisionMask &= ~mask;

        if (TryComp<PhysicsComponent>(component.Owner, out var body))
        {
            _fixtures.FixtureUpdate(component, body);
        }

        _broadphase.Refilter(fixture);
    }

    public void AddCollisionLayer(FixturesComponent component, Fixture fixture, int layer)
    {
        if ((fixture.CollisionLayer & layer) == layer) return;

        DebugTools.Assert(component.Fixtures.ContainsKey(fixture.ID));
        fixture._collisionLayer |= layer;

        if (TryComp<PhysicsComponent>(component.Owner, out var body))
        {
            _fixtures.FixtureUpdate(component, body);
        }

        _broadphase.Refilter(fixture);
    }

    public void SetCollisionLayer(FixturesComponent component, Fixture fixture, int layer)
    {
        if (fixture.CollisionLayer == layer) return;

        DebugTools.Assert(component.Fixtures.ContainsKey(fixture.ID));
        fixture._collisionLayer = layer;

        if (TryComp<PhysicsComponent>(component.Owner, out var body))
        {
            _fixtures.FixtureUpdate(component, body);
        }

        _broadphase.Refilter(fixture);
    }

    public void SetCollisionLayer(Fixture fixture, int layer, FixturesComponent? fixturesComponent = null)
    {
        if (fixture._collisionLayer.Equals(layer))
            return;

        if (!Resolve(fixture.Body.Owner, ref fixturesComponent))
            return;

        fixture._collisionLayer = layer;
        _fixtures.FixtureUpdate(fixturesComponent, fixture.Body);
        _broadphase.Refilter(fixture);
    }

    public void RemoveCollisionLayer(FixturesComponent component, Fixture fixture, int layer)
    {
        if ((fixture.CollisionLayer & layer) == 0x0) return;

        DebugTools.Assert(component.Fixtures.ContainsKey(fixture.ID));
        fixture._collisionLayer &= ~layer;

        if (TryComp<PhysicsComponent>(component.Owner, out var body))
        {
            _fixtures.FixtureUpdate(component, body);
        }

        _broadphase.Refilter(fixture);
    }

    #endregion
}
