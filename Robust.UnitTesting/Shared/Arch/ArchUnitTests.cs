using Arch.Core;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Shared.Arch;

[TestFixture]
public sealed class ArchUnitTests
{
    private World _world = World.Create();

    [Test]
    public void EntityTest()
    {
        var ent = _world.Create();
        var uid = EntityUid.FromArch(_world, ent);
        Assert.Multiple(() =>
        {
            Assert.That(uid.GetArchId(), Is.EqualTo(ent.Id));
            Assert.That(_world.IsAlive(new Entity(uid.GetArchId())));
        });
    }
}
