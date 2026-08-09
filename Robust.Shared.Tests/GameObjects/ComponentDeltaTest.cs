using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Shared.Tests.GameObjects;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
[TestOf(typeof(EntityManager))]
internal sealed partial class ComponentDeltaTest
{
    [Test]
    public void FieldDirtyOnlyReturnsFieldAspect()
    {
        var component = new TestDeltaComponent
        {
            LastUnclassifiedDirty = GameTick.Zero,
            LastModifiedFields =
            [
                GameTick.Zero,
                new GameTick(11),
            ],
        };

        Assert.That(EntityManager.GetModifiedAspects(component, new GameTick(10)), Is.EqualTo(1UL << 1));
    }

    [Test]
    public void UnclassifiedDirtyOnSameTickAsFieldDirtyForcesFullState()
    {
        var dirtyTick = new GameTick(11);
        var component = new TestDeltaComponent
        {
            LastUnclassifiedDirty = dirtyTick,
            LastModifiedFields =
            [
                dirtyTick,
                GameTick.Zero,
            ],
        };

        var aspects = EntityManager.GetModifiedAspects(component, new GameTick(10));

        Assert.That(aspects, Is.GreaterThanOrEqualTo(DeltaAspect.Unclassified));
    }

    private sealed partial class TestDeltaComponent : Component, IComponentDelta
    {
        public GameTick LastUnclassifiedDirty { get; set; }
        public GameTick[] LastModifiedFields { get; set; } = [];
    }
}
