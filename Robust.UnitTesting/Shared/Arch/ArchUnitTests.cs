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
        var uid = EntityUid.FromArch(ent);
        Assert.That(uid.GetHashCode(), Is.EqualTo(ent.Id));

        var andBackAgain = uid.ToArch();
        Assert.That(andBackAgain, Is.EqualTo(ent));
    }
}
