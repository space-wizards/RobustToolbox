using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    private void InitializeFixturesChange()
    {
        SubscribeLocalEvent<FixturesChangeComponent, ComponentStartup>(OnChangeStartup);
        SubscribeLocalEvent<FixturesChangeComponent, ComponentShutdown>(OnChangeShutdown);
    }

    private void OnChangeStartup(Entity<FixturesChangeComponent> ent, ref ComponentStartup args)
    {
        foreach (var (id, fixture) in ent.Comp.Fixtures)
        {
            _fixtures.TryCreateFixture(ent.Owner,
                fixture.Shape,
                id,
                fixture.Density,
                fixture.Hard,
                fixture.CollisionLayer,
                fixture.CollisionMask,
                fixture.Friction,
                fixture.Restitution);
        }

        // TODO: Fixture creation should be handling this.
        WakeBody(ent.Owner);
    }

    private void OnChangeShutdown(Entity<FixturesChangeComponent> ent, ref ComponentShutdown args)
    {
        foreach (var id in ent.Comp.Fixtures.Keys)
        {
            _fixtures.DestroyFixture(ent.Owner, id);
        }
    }
}
