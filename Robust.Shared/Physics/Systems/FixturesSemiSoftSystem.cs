using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Robust.Shared.Physics.Systems;

/// <summary>
/// Prevents collision if the colliding fixture layers are completely contained within the
/// <see cref="FixturesSemiSoftComponent"/> mask.
/// </summary>
public sealed class FixturesSemiSoftSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FixturesSemiSoftComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<FixturesSemiSoftComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !args.OurFixture.Hard || !args.OtherFixture.Hard)
            return;

        // Get the mask of the collision itself.
        var collisionMask = args.OurFixture.CollisionLayer & args.OtherFixture.CollisionMask;

        if ((ent.Comp.Mask | collisionMask) == ent.Comp.Mask)
            args.Cancelled = true;

    }
}
