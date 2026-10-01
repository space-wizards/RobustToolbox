using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects;

[TestFixture]
internal sealed partial class EntityManagerIsDefaultTests
{
    private const string PrototypeId = "IsDefaultTestEntity";

    private const string Prototypes = $"""
        - type: entity
          id: {PrototypeId}
          name: test entity
          description: test description
          components:
          - type: TimedDespawn
            lifetime: 5
        """;

    [Test]
    public void IsDefaultComparesAgainstPrototypeData()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(factory =>
            {
                factory.RegisterClass<TimedDespawnComponent>();
                factory.RegisterClass<PlacementReplacementComponent>();
            })
            .RegisterPrototypes(prototypes =>
            {
                prototypes.LoadString(Prototypes);
                prototypes.ResolveResults();
            })
            .InitializeInstance();

        var entMan = (EntityManager) sim.Resolve<IEntityManager>();
        var map = sim.CreateMap().Uid;
        var coords = new EntityCoordinates(map, default);
        var entity = entMan.SpawnEntity(PrototypeId, coords);

        Assert.That(entMan.IsDefault(entity), Is.True);

        entMan.GetComponent<TimedDespawnComponent>(entity).Lifetime = 10;
        Assert.That(entMan.IsDefault(entity), Is.False);
        Assert.That(entMan.IsDefault(entity, new HashSet<string> { "TimedDespawn" }), Is.True);

        entMan.GetComponent<TimedDespawnComponent>(entity).Lifetime = 5;
        entMan.AddComponent<PlacementReplacementComponent>(entity);
        Assert.That(entMan.IsDefault(entity), Is.False);
    }

    [Test]
    public void DataFieldEqualsComparesHashSetsAsSets()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .InitializeInstance();

        var serialization = sim.Resolve<ISerializationManager>();

        Assert.That(serialization.DataFieldEquals(
            new HashSet<int> { 1, 2, 3 },
            new HashSet<int> { 3, 2, 1 }), Is.True);

        Assert.That(serialization.DataFieldEquals(
            new HashSet<int> { 1, 2, 3 },
            new HashSet<int> { 1, 2, 4 }), Is.False);
    }
}
