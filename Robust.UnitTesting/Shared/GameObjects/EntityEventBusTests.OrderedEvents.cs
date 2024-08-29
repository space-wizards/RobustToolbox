using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects;

public sealed partial class EntityEventBusTests
{
    // Explanation of what bug this is testing:
    // Because event ordering is keyed on system type, we have a problem.
    // If you register to a directed event like FooEvent twice for different components,
    // you now have different subscriptions with the same key.
    //
    // To trigger this, at least one subscription to this event (possibly another system entirely)
    // needs to demand some ordering calculation to happen.

    [Test]
    public void TestDifferentComponentsOrderedSameKeySub()
    {
        var simulation = RobustServerSimulation
            .NewSimulation()
            .RegisterEntitySystems(factory =>
            {
                factory.LoadExtraSystemType<DifferentComponentsSameKeySubSystem>();
                factory.LoadExtraSystemType<DifferentComponentsSameKeySubSystem2>();
            })
            .RegisterComponents(factory => factory.RegisterClass<FooComponent>())
            .InitializeInstance();

        var map = simulation.CreateMap().MapId;

        var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
        simulation.Resolve<IEntityManager>().AddComponent<FooComponent>(entity);

        var foo = new FooEvent();
        simulation.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(entity, foo, true);

        Assert.That(foo.EventOrder, Is.EquivalentTo(new[]{"Foo", "Transform", "Metadata"}).Or.EquivalentTo(new[]{"Foo", "Metadata", "Transform"}));
    }

    [Reflect(false)]
    private sealed class DifferentComponentsSameKeySubSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<TransformComponent, FooEvent>((_, _, e) => { e.EventOrder.Add("Transform"); });
            SubscribeLocalEvent<MetaDataComponent, FooEvent>((_, _, e) => { e.EventOrder.Add("Metadata"); });
        }
    }

    [Reflect(false)]
    private sealed class DifferentComponentsSameKeySubSystem2 : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<FooComponent, FooEvent>(
                (_, _, e) => e.EventOrder.Add("Foo"),
                before: new[] {typeof(DifferentComponentsSameKeySubSystem)});
        }
    }

    [Reflect(false)]
    private sealed partial class FooComponent : Component
    {

    }

    private sealed class FooEvent
    {
        public List<string> EventOrder = new();
    }
}
