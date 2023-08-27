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

        // Spawning next to an entity that is not in a container will simply spawn it in the same position
        await Server.WaitPost(() =>
        {
            Assert.That(EntMan.TrySpawnNextTo(null, GrandChildB, out var uid));
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid!.Value), Is.EqualTo(ChildB));
            Assert.That(Container.IsEntityInContainer(uid.Value), Is.False);
            Assert.That(Container.IsEntityOrParentInContainer(uid.Value));
            Assert.That(EntMan.GetComponent<TransformComponent>(uid.Value).Coordinates, Is.EqualTo(GrandChildBPos));
        });

        // Spawning "next to" a nullspace entity will fail.
        await Server.WaitPost(() =>
        {
            int count = EntMan.EntityCount;
            Assert.That(EntMan.TrySpawnNextTo(null, Map, out var uid), Is.False);
            Assert.That(EntMan.EntityCount, Is.EqualTo(count));
            Assert.That(EntMan.EntityExists(uid), Is.False);
        });

        await Server.WaitPost(() =>MapMan.DeleteMap(MapId));
        Server.Dispose();
    }
}
