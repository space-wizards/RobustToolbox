using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.EntitySerialization;

/// <summary>
/// Simple test component that contains a data-field with a <see cref="AlwaysPushInheritanceAttribute"/>
/// </summary>
[RegisterComponent]
public sealed partial class TestAlwaysPushComponent : Component
{
    [DataField, AlwaysPushInheritance] public List<int> List = [];

    [DataField] public int Id;
}

[TestFixture]
public sealed class AlwaysPushSerializationTest : RobustIntegrationTest
{
    private const string Prototype = @"
- type: entity
  id: TestEntityCompositionParent
  components:
  - type: TestAlwaysPush
    list: [ 1, 2 ]

- type: entity
  id: TestEntityCompositionChild
  parent: TestEntityCompositionParent
  components:
  - type: TestAlwaysPush
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

        // Create a new map and spawn in some entities.
        MapId mapId = default;
        Entity<TestAlwaysPushComponent> parent1 = default;
        Entity<TestAlwaysPushComponent> parent2 = default;
        Entity<TestAlwaysPushComponent> parent3 = default;
        Entity<TestAlwaysPushComponent> child1 = default;
        Entity<TestAlwaysPushComponent> child2 = default;
        Entity<TestAlwaysPushComponent> child3 = default;

        var path = new ResPath($"{nameof(TestAlwaysPushSerialization)}.yml");

        await server.WaitPost(() =>
        {
            server.System<SharedMapSystem>().CreateMap(out mapId);
            var coords = new MapCoordinates(0, 0, mapId);
            var uidParent1 = server.EntMan.Spawn("TestEntityCompositionParent", coords);
            var uidParent2 = server.EntMan.Spawn("TestEntityCompositionParent", coords);
            var uidParent3 = server.EntMan.Spawn("TestEntityCompositionParent", coords);
            var uidChild1 = server.EntMan.Spawn("TestEntityCompositionChild", coords);
            var uidChild2 = server.EntMan.Spawn("TestEntityCompositionChild", coords);
            var uidChild3 = server.EntMan.Spawn("TestEntityCompositionChild", coords);

            parent1 = (uidParent1, server.EntMan.GetComponent<TestAlwaysPushComponent>(uidParent1));
            parent2 = (uidParent2, server.EntMan.GetComponent<TestAlwaysPushComponent>(uidParent2));
            parent3 = (uidParent3, server.EntMan.GetComponent<TestAlwaysPushComponent>(uidParent3));
            child1 = (uidChild1, server.EntMan.GetComponent<TestAlwaysPushComponent>(uidChild1));
            child2 = (uidChild2, server.EntMan.GetComponent<TestAlwaysPushComponent>(uidChild2));
            child3 = (uidChild3, server.EntMan.GetComponent<TestAlwaysPushComponent>(uidChild3));
        });

        // Assign a unique id to each entity (so they can be identified after saving & loading a map)
        parent1.Comp!.Id = 1;
        parent2.Comp!.Id = 2;
        parent3.Comp!.Id = 3;
        child1.Comp!.Id = 4;
        child2.Comp!.Id = 5;
        child3.Comp!.Id = 6;

        // The inheritance pushing for the prototypes should ensure that the parent & child prototype's lists were merged.
        Assert.That(parent1.Comp.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent2.Comp.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent3.Comp.List.SequenceEqual(new[] {1, 2}));
        Assert.That(child1.Comp.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child2.Comp.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child3.Comp.List.SequenceEqual(new[] {3, 4, 1, 2}));

        // Modify data on some components.
        parent2.Comp.List.Add(-1);
        child2.Comp.List.Add(-1);
        parent3.Comp.List.RemoveAt(1);
        child3.Comp.List.RemoveAt(1);

        Assert.That(parent1.Comp.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent2.Comp.List.SequenceEqual(new[] {1, 2, -1}));
        Assert.That(parent3.Comp.List.SequenceEqual(new[] {1}));
        Assert.That(child1.Comp.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child2.Comp.List.SequenceEqual(new[] {3, 4, 1, 2, -1}));
        Assert.That(child3.Comp.List.SequenceEqual(new[] {3, 1, 2}));

        // Save map to yaml
        var loader = server.System<MapLoaderSystem>();
        var map = server.System<SharedMapSystem>();
        loader.SaveMap(mapId, path);

        // Delete the entities
        await server.WaitPost(() => map.DeleteMap(mapId));
        var ents = server.EntMan.AllEntities<TestAlwaysPushComponent>();
        Assert.That(ents.Length, Is.EqualTo(0));

        // Load the map
        await server.WaitPost(() =>
        {
            Assert.That(loader.TryLoadMap(path, out var ent, out _));
            mapId = ent!.Value.Comp.MapId;
        });

        ents = server.EntMan.AllEntities<TestAlwaysPushComponent>();
        Assert.That(ents.Length, Is.EqualTo(6));

        parent1 = ents.Single(x => x.Comp.Id == 1);
        parent2 = ents.Single(x => x.Comp.Id == 2);
        parent3 = ents.Single(x => x.Comp.Id == 3);
        child1 = ents.Single(x => x.Comp.Id == 4);
        child2 = ents.Single(x => x.Comp.Id == 5);
        child3 = ents.Single(x => x.Comp.Id == 6);

        // Verify that the entity data has not changed.
        Assert.That(parent1.Comp.List.SequenceEqual(new[] {1, 2}));
        Assert.That(parent2.Comp.List.SequenceEqual(new[] {1, 2, -1}));
        Assert.That(parent3.Comp.List.SequenceEqual(new[] {1}));
        Assert.That(child1.Comp.List.SequenceEqual(new[] {3, 4, 1, 2}));
        Assert.That(child2.Comp.List.SequenceEqual(new[] {3, 4, 1, 2, -1}));
        Assert.That(child3.Comp.List.SequenceEqual(new[] {3, 1, 2}));
    }
}
