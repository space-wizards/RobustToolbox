using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Shared.Spawning;

[TestFixture]
public sealed class SpawnNextToOrDropTest : EntitySpawnHelpersTest
{
    [Test]
    public async Task Test()
    {
        await Setup();

        // Spawning next to an entity in a container will insert the entity into the container.
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildA);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(GrandChildA));
            Assert.That(Container.IsEntityInContainer(uid));
            Assert.That(Container.GetContainer(GrandChildA, "greatGrandChildA").Contains(uid));
        });

        // The container is now full, spawning will insert into the outer container.
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildA);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(ChildA));
            Assert.That(Container.IsEntityInContainer(uid));
            Assert.That(Container.GetContainer(ChildA, "grandChildA").Contains(uid));
        });

        // If outer two containers are full, will insert into outermost container.
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildA);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(Parent));
            Assert.That(Container.IsEntityInContainer(uid));
            Assert.That(Container.GetContainer(Parent, "childA").Contains(uid));
        });

        // Finally, this will drop the item on the map.
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildA);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(Map));
            Assert.That(Container.IsEntityInContainer(uid), Is.False);
            Assert.That(EntMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(ParentPos));
        });

        // Repeating this will just drop it on the map again.
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildA);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(Map));
            Assert.That(Container.IsEntityInContainer(uid), Is.False);
            Assert.That(EntMan.GetComponent<TransformComponent>(uid).Coordinates, Is.EqualTo(ParentPos));
        });

        // Repeat the above but with the B-children.

        // First insert works fine
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildB);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(GrandChildB));
            Assert.That(Container.IsEntityInContainer(uid));
            Assert.That(Container.GetContainer(GrandChildB, "greatGrandChildB").Contains(uid));
        });

        // AS grandChildB is not in a container, but its parent still is, the next insert will insert the entity into
        // the same container as childB
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildB);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(Parent));
            Assert.That(Container.IsEntityInContainer(uid), Is.True);
            Assert.That(Container.GetContainer(Parent, "childB").Contains(uid));
        });

        // Repeating this will attach the entity to the map
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, GreatGrandChildB);
            Assert.That(EntMan.EntityExists(uid));
            Assert.That(Xforms.GetParentUid(uid), Is.EqualTo(Map));
            Assert.That(Container.IsEntityInContainer(uid), Is.False);
        });

        // Spawning "next to" a map just drops the entity in nullspace
        await Server.WaitPost(() =>
        {
            var uid = EntMan.SpawnNextToOrDrop(null, Map);
            Assert.That(EntMan.EntityExists(uid));
            var xform = EntMan.GetComponent<TransformComponent>(uid);
            Assert.That(xform.ParentUid, Is.EqualTo(EntityUid.Invalid));
            Assert.That(xform.MapID, Is.EqualTo(MapId.Nullspace));
            Assert.That(xform.MapUid, Is.Null);
            Assert.That(xform.GridUid, Is.Null);
        });

        // Spawning next to an entity on a pre-init map does not initialize the entity.
        // Previously the intermediate step of spawning the entity into nullspace would cause it to get initialized.
        await Server.WaitPost(() =>
        {
            var preInitMap = EntMan.System<SharedMapSystem>().CreateMap(out var mapId, runMapInit: false);
            var ent = EntMan.Spawn(null, new MapCoordinates(default, mapId));

            Assert.That(EntMan.GetComponent<MetaDataComponent>(preInitMap).EntityLifeStage, Is.LessThan(EntityLifeStage.MapInitialized));
            Assert.That(EntMan.GetComponent<MetaDataComponent>(ent).EntityLifeStage, Is.LessThan(EntityLifeStage.MapInitialized));

            var uid = EntMan.SpawnNextToOrDrop(null, ent);
            Assert.That(EntMan.GetComponent<MetaDataComponent>(uid).EntityLifeStage, Is.LessThan(EntityLifeStage.MapInitialized));
        });

        await Server.WaitPost(() => MapSys.DeleteMap(MapId));
    }
}
