using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using static Robust.UnitTesting.Shared.EntitySerialization.EntitySaveTestComponent;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
public sealed class AlwaysPushSerializationTest : RobustIntegrationTest
{
    private const string Prototype = @"
- type: entity
  id: TestEntityCompositionParent
  components:
  - type: EntitySaveTest
    list: [ 1, 2 ]

- type: entity
  id: TestEntityCompositionChild
  parent: TestEntityCompositionParent
  components:
  - type: EntitySaveTest
    list: [ 3 , 4 ]
";

    /// <summary>
    /// This test checks that deserializing an entity with some component that has the
    /// <see cref="AlwaysPushInheritanceAttribute"/> works as intended. Previously the attribute would cause the entity
    /// prototype to **always** append it's contents to the loaded entity, effectively causing
    /// the <see cref="TestAlwaysPushComponent.List"/> data-field to grow each time a map was loaded and saved.
    /// </summary>
    [Test]
    [TestOf(typeof(AlwaysPushInheritanceAttribute))]
    public async Task TestAlwaysPushSerialization()
    {
        var opts = new ServerIntegrationOptions
        {
            ExtraPrototypes = Prototype
        };

        var server = StartServer(opts);
        await server.WaitIdleAsync();
        var entMan = server.EntMan;

        // Create a new map and spawn in some entities.
        MapId mapId = default;
        Entity<TransformComponent, EntitySaveTestComponent> parent1 = default;
        Entity<TransformComponent, EntitySaveTestComponent> parent2 = default;
        Entity<TransformComponent, EntitySaveTestComponent> parent3 = default;
        Entity<TransformComponent, EntitySaveTestComponent> child1 = default;
        Entity<TransformComponent, EntitySaveTestComponent> child2 = default;
        Entity<TransformComponent, EntitySaveTestComponent> child3 = default;

        var path = new ResPath($"{nameof(TestAlwaysPushSerialization)}.yml");

        await server.WaitPost(() =>
        {
            server.System<SharedMapSystem>().CreateMap(out mapId);
            var coords = new MapCoordinates(0, 0, mapId);
            var parent1Uid = entMan.Spawn("TestEntityCompositionParent", coords);
            var parent2Uid = entMan.Spawn("TestEntityCompositionParent", coords);
            var parent3Uid = entMan.Spawn("TestEntityCompositionParent", coords);
            var child1Uid = entMan.Spawn("TestEntityCompositionChild", coords);
            var child2Uid = entMan.Spawn("TestEntityCompositionChild", coords);
            var child3Uid = entMan.Spawn("TestEntityCompositionChild", coords);

            parent1 = Get(parent1Uid, entMan);
            parent2 = Get(parent2Uid, entMan);
            parent3 = Get(parent3Uid, entMan);
            child1 = Get(child1Uid, entMan);
            child2 = Get(child2Uid, entMan);
            child3 = Get(child3Uid, entMan);
        });

        // Assign a unique id to each entity (so they can be identified after saving & loading a map)
        parent1.Comp2!.Id = nameof(parent1);
        parent2.Comp2!.Id = nameof(parent2);
        parent3.Comp2!.Id = nameof(parent3);
        child1.Comp2!.Id = nameof(child1);
        child2.Comp2!.Id = nameof(child2);
        child3.Comp2!.Id = nameof(child3);

        // The inheritance pushing for the prototypes should ensure that the parent & child prototype's lists were merged.
        Assert.That(parent1.Comp2.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent2.Comp2.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent3.Comp2.List.SequenceEqual(new[] {1, 2}));
        Assert.That(child1.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child2.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child3.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2}));

        // Modify data on some components.
        parent2.Comp2.List.Add(-1);
        child2.Comp2.List.Add(-1);
        parent3.Comp2.List.RemoveAt(1);
        child3.Comp2.List.RemoveAt(1);

        Assert.That(parent1.Comp2.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent2.Comp2.List.SequenceEqual(new[] {1, 2, -1}));
        Assert.That(parent3.Comp2.List.SequenceEqual(new[] {1}));
        Assert.That(child1.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child2.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2, -1}));
        Assert.That(child3.Comp2.List.SequenceEqual(new[] {3, 1, 2}));

        // Save map to yaml
        var loader = server.System<MapLoaderSystem>();
        var map = server.System<SharedMapSystem>();
        Assert.That(loader.TrySaveMap(mapId, path));

        // Delete the entities
        await server.WaitPost(() => map.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load the map
        await server.WaitPost(() =>
        {
            Assert.That(loader.TryLoadMap(path, out var ent, out _));
            mapId = ent!.Value.Comp.MapId;
        });

        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(6));

        // Find the deserialized entities
        parent1 = Find(nameof(parent1), entMan);
        parent2 = Find(nameof(parent2), entMan);
        parent3 = Find(nameof(parent3), entMan);
        child1 = Find(nameof(child1), entMan);
        child2 = Find(nameof(child2), entMan);
        child3 = Find(nameof(child3), entMan);

        // Verify that the entity data has not changed.
        Assert.That(parent1.Comp2.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent2.Comp2.List.SequenceEqual(new[] {1, 2, -1}));
        Assert.That(parent3.Comp2.List.SequenceEqual(new[] {1}));
        Assert.That(child1.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child2.Comp2.List.SequenceEqual(new[] {3, 4, 1, 2, -1}));
        Assert.That(child3.Comp2.List.SequenceEqual(new[] {3, 1, 2}));
    }
}
