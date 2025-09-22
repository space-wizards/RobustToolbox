using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Shared.Spawning;

[TestFixture]
public sealed class TrySpawnNextToTest : EntitySpawnHelpersTest
{
    [Test]
    public async Task Test()
    {
        await Setup();

        // Spawning next to an entity in a container will insert the entity into the container.
        await Server.WaitPost(() =>
        {
            Assert.That(EntMan.TrySpawnNextTo(null, ChildA, out var uid));
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid!.Value), Is.EqualTo(Parent));
            Assert.That(Container.IsEntityInContainer(uid.Value));
            Assert.That(Container.GetContainer(Parent, "childA").Contains(uid.Value));
        });

        // The container is now full, spawning will fail.
        await Server.WaitPost(() =>
        {
            int count = EntMan.EntityCount;
            Assert.That(EntMan.TrySpawnNextTo(null, ChildA, out var uid), Is.False);
            Assert.That(EntMan.EntityCount, Is.EqualTo(count));
            Assert.That(EntMan.EntityExists(uid), Is.False);
        });

        // Spawning next to an entity that is not in a container will drop it
        await Server.WaitPost(() =>
        {
            Assert.That(EntMan.TrySpawnNextTo(null, GrandChildB, out var uid));
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid!.Value), Is.EqualTo(Parent));
            Assert.That(Container.IsEntityInContainer(uid.Value));
            Assert.That(Container.IsEntityOrParentInContainer(uid.Value));
        });

        // Spawning "next to" a nullspace entity will fail.
        await Server.WaitPost(() =>
        {
            int count = EntMan.EntityCount;
            Assert.That(EntMan.TrySpawnNextTo(null, Map, out var uid), Is.False);
            Assert.That(EntMan.EntityCount, Is.EqualTo(count));
            Assert.That(EntMan.EntityExists(uid), Is.False);
        });

        await Server.WaitPost(() => MapSys.DeleteMap(MapId));
    }
}
