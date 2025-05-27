using System.Threading.Tasks;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Spawning;

[TestFixture]
public sealed class TrySpawnInContainerTest : EntitySpawnHelpersTest
{
    [Test]
    public async Task Test()
    {
        await Setup();

        // Spawning into a non-existent container does nothing.
        await Server.WaitPost(() =>
        {
            int count = EntMan.EntityCount;
            Assert.That(EntMan.TrySpawnInContainer(null, ChildA, "foo", out var uid), Is.False);
            Assert.That(EntMan.EntityCount, Is.EqualTo(count));
            Assert.That(EntMan.EntityExists(uid), Is.False);
            Assert.That(EntMan.TrySpawnInContainer(null, GrandChildB, "foo", out uid), Is.False);
            Assert.That(EntMan.EntityCount, Is.EqualTo(count));
            Assert.That(EntMan.EntityExists(uid), Is.False);
        });

        // Spawning into a container works as expected.
        await Server.WaitPost(() =>
        {
            Assert.That(EntMan.TrySpawnInContainer(null, ChildA, "grandChildA", out var uid));
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid!.Value), Is.EqualTo(ChildA));
            Assert.That(Container.IsEntityInContainer(uid.Value));
            Assert.That(Container.GetContainer(ChildA, "grandChildA").Contains(uid.Value));
        });

        // Spawning another entity will fail as the container is now full
        await Server.WaitPost(() =>
        {
            int count = EntMan.EntityCount;
            Assert.That(EntMan.TrySpawnInContainer(null, ChildA, "grandChildA", out var uid), Is.False);
            Assert.That(EntMan.EntityCount, Is.EqualTo(count));
            Assert.That(EntMan.EntityExists(uid), Is.False);
        });

        await Server.WaitPost(() => MapSys.DeleteMap(MapId));
    }
}
