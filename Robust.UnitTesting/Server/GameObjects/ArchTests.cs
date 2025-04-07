using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using NUnit.Framework;
using Robust.Shared.Console.Commands;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Server.GameObjects;

/// <summary>
/// Tests engine integrations with Arch.
/// </summary>
[TestFixture]
public sealed class ArchTests
{
    /// <summary>
    /// Asserts that EntityUids match the expected Arch EntityReference.
    /// </summary>
    [Test]
    public void EntityTest()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();

        var entity = entManager.Spawn(null, MapCoordinates.Nullspace);
        var entReference = (Entity)entity;
        Assert.That(entity.Id, Is.EqualTo(entReference.Id + EntityUid.ArchUidOffset));
        Assert.That(entity.Version, Is.EqualTo(entReference.Version + EntityUid.ArchVersionOffset));

        entManager.DeleteEntity(entity);
    }

    /// <summary>
    /// Asserts that deleted entities stay deleted despite entity recycling.
    /// If we don't account for EntityReference / versions correctly old entities may return as being nopt deleted.
    /// </summary>
    [Test]
    public void EntityVersionTest()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();

        var entity = entManager.Spawn(null, MapCoordinates.Nullspace);
        entManager.DeleteEntity(entity);
        Assert.That(entManager.Deleted(entity));
        Assert.That(!entManager.EntityExists(entity));

        // Spawn a new entity and check it is a recycled ID.
        var entity2 = entManager.Spawn(null, MapCoordinates.Nullspace);
        Assert.That(entity.Id, Is.EqualTo(entity2.Id));
        Assert.That(entity.Version, Is.Not.EqualTo(entity2.Version));

        // Assert the old entity still returns deleted but new one isn't.
        Assert.That(entManager.Deleted(entity));
        Assert.That(!entManager.Deleted(entity2));
        Assert.That(!entManager.EntityExists(entity));
        Assert.That(entManager.EntityExists(entity2));
        entManager.DeleteEntity(entity2);
    }
}
