using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Robust.Shared.IntegrationTests.GameObjects;

[TestFixture]
public sealed class EntitySystemManagerUninitializedTests
{
    private sealed class DummySystem : EntitySystem;

    [Test]
    public void TryDoesNotThrow()
    {
        var systems = new EntitySystemManager();

        // These should not throw even with an uninitalized entity system manager
        Assert.DoesNotThrow(() => systems.TryGetEntitySystem(out DummySystem? _));
        Assert.DoesNotThrow(() => systems.TryGetEntitySystem(typeof(DummySystem), out _));
        Assert.DoesNotThrow(() => systems.GetEntitySystemOrNull<DummySystem>());

        Assert.That(systems.TryGetEntitySystem(out DummySystem? _), Is.False);
        Assert.That(systems.TryGetEntitySystem(typeof(DummySystem), out _), Is.False);
        Assert.That(systems.GetEntitySystemOrNull<DummySystem>(), Is.Null);
    }
}
