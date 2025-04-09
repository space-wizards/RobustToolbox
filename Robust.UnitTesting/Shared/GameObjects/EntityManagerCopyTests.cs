using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects;

[TestFixture]
public sealed partial class EntityManagerCopyTests
{
    [Test]
    public void CopyComponentGeneric()
    {
        var instant = RobustServerSimulation.NewSimulation();
        instant.RegisterComponents(fac =>
        {
            fac.RegisterClass<AComponent>();
        });

        var sim = instant.InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        mapSystem.CreateMap(out var mapId);

        var original = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        var comp = entManager.AddComponent<AComponent>(original);

        Assert.That(comp.Value, Is.EqualTo(false));
        comp.Value = true;

        var target = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        Assert.That(!entManager.HasComponent<AComponent>(target));

        var targetComp = entManager.CopyComponent(original, target, comp);

        Assert.That(entManager.GetComponent<AComponent>(target), Is.EqualTo(targetComp));
        Assert.That(targetComp.Value, Is.EqualTo(comp.Value));
        Assert.That(!ReferenceEquals(comp, targetComp));
    }

    [Test]
    public void CopyComponentNonGeneric()
    {
        var instant = RobustServerSimulation.NewSimulation();
        instant.RegisterComponents(fac =>
        {
            fac.RegisterClass<AComponent>();
        });

        var sim = instant.InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        mapSystem.CreateMap(out var mapId);

        var original = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        var comp = entManager.AddComponent<AComponent>(original);

        Assert.That(comp.Value, Is.EqualTo(false));
        comp.Value = true;

        var target = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        Assert.That(!entManager.HasComponent<AComponent>(target));

        var targetComp = entManager.CopyComponent(original, target, (IComponent) comp);

        Assert.That(entManager.GetComponent<AComponent>(target), Is.EqualTo(targetComp));
        Assert.That(((AComponent) targetComp).Value, Is.EqualTo(comp.Value));
        Assert.That(!ReferenceEquals(comp, targetComp));
    }

    [Test]
    public void CopyComponentMultiple()
    {
        var instant = RobustServerSimulation.NewSimulation();
        instant.RegisterComponents(fac =>
        {
            fac.RegisterClass<AComponent>();
            fac.RegisterClass<BComponent>();
        });

        var sim = instant.InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        mapSystem.CreateMap(out var mapId);

        var original = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        var comp = entManager.AddComponent<AComponent>(original);
        var comp2 = entManager.AddComponent<BComponent>(original);

        Assert.That(comp.Value, Is.EqualTo(false));
        comp.Value = true;

        var target = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        Assert.That(!entManager.HasComponent<AComponent>(target));

        entManager.CopyComponents(original, target, null, comp, comp2);
        var targetComp = entManager.GetComponent<AComponent>(target);
        var targetComp2 = entManager.GetComponent<BComponent>(target);

        Assert.That(entManager.GetComponent<AComponent>(target), Is.EqualTo(targetComp));
        Assert.That(targetComp.Value, Is.EqualTo(comp.Value));

        Assert.That(entManager.GetComponent<BComponent>(target), Is.EqualTo(targetComp2));
        Assert.That(targetComp2.Value, Is.EqualTo(comp2.Value));

        Assert.That(!ReferenceEquals(comp, targetComp));
        Assert.That(!ReferenceEquals(comp2, targetComp2));
    }

    [Test]
    public void CopyComponentMultipleViaTry()
    {
        var instant = RobustServerSimulation.NewSimulation();
        instant.RegisterComponents(fac =>
        {
            fac.RegisterClass<AComponent>();
            fac.RegisterClass<BComponent>();
        });

        var sim = instant.InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        mapSystem.CreateMap(out var mapId);

        var original = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        var comp = entManager.AddComponent<AComponent>(original);
        var comp2 = entManager.AddComponent<BComponent>(original);

        Assert.That(comp.Value, Is.EqualTo(false));
        comp.Value = true;

        var target = entManager.Spawn(null, new MapCoordinates(Vector2.Zero, mapId));
        Assert.That(!entManager.HasComponent<AComponent>(target));

        entManager.TryCopyComponents(original, target, null, comp.GetType(), comp2.GetType());
        var targetComp = entManager.GetComponent<AComponent>(target);
        var targetComp2 = entManager.GetComponent<BComponent>(target);

        Assert.That(entManager.GetComponent<AComponent>(target), Is.EqualTo(targetComp));
        Assert.That(targetComp.Value, Is.EqualTo(comp.Value));

        Assert.That(entManager.GetComponent<BComponent>(target), Is.EqualTo(targetComp2));
        Assert.That(targetComp2.Value, Is.EqualTo(comp2.Value));

        Assert.That(!ReferenceEquals(comp, targetComp));
        Assert.That(!ReferenceEquals(comp2, targetComp2));
    }

    [DataDefinition]
    private sealed partial class AComponent : Component
    {
        [DataField]
        public bool Value = false;
    }

    [DataDefinition]
    private sealed partial class BComponent : Component
    {
        [DataField]
        public bool Value = false;
    }
}
