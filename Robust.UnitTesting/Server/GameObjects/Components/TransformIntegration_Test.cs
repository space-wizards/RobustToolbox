using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Server.GameObjects.Components;

[TestFixture]
public sealed class TransformIntegration_Test
{
    /// <summary>
    /// Asserts that calling SetWorldPosition while in a container correctly removes the entity from its container.
    /// </summary>
    [Test]
    public void WorldPositionContainerSet()
    {
        var factory = RobustServerSimulation.NewSimulation();

        var sim = factory.InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var containerSystem = entManager.System<SharedContainerSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();
        var map1 = sim.CreateMap().Uid;

        var ent1 = entManager.SpawnEntity(null, new EntityCoordinates(map1, Vector2.Zero));
        var ent2 = entManager.SpawnEntity(null, new EntityCoordinates(map1, Vector2.Zero));
        var ent3 = entManager.SpawnEntity(null, new EntityCoordinates(map1, Vector2.Zero));

        var container = containerSystem.EnsureContainer<ContainerSlot>(ent1, "a");

        // Assert that setting worldpos updates parent correctly.
        containerSystem.Insert(ent2, container, force: true);

        Assert.That(containerSystem.IsEntityInContainer(ent2));

        xformSystem.SetWorldPosition(ent2, Vector2.One);

        Assert.That(!containerSystem.IsEntityInContainer(ent2));
        Assert.That(xformSystem.GetWorldPosition(ent2), Is.EqualTo(Vector2.One));

        // Assert that you can set recursively contained (but not directly contained) entities correctly.
        containerSystem.Insert(ent2, container);
        xformSystem.SetParent(ent3, ent2);

        Assert.That(xformSystem.GetParentUid(ent3), Is.EqualTo(ent2));

        xformSystem.SetWorldPosition(ent3, Vector2.One);

        Assert.That(xformSystem.GetParentUid(ent3), Is.EqualTo(map1));
        Assert.That(xformSystem.GetWorldPosition(ent3).Equals(Vector2.One));

        // Cleanup
        entManager.DeleteEntity(map1);
    }
}
