using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedPhysicsSystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;

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

        _broadphaseSystem.Refilter(fixture);
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

        _broadphaseSystem.Refilter(fixture);
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

        _broadphaseSystem.Refilter(fixture);
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

        _broadphaseSystem.Refilter(fixture);
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

        _broadphaseSystem.Refilter(fixture);
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

        _broadphaseSystem.Refilter(fixture);
    }

    #endregion
}
