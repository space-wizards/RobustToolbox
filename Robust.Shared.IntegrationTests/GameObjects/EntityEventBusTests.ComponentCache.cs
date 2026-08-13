using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects;

internal sealed partial class EntityEventBusTests
{
    [Test]
    public void DirectedComponentCacheSingleMultiAndRefDispatch()
    {
        var simulation = NewComponentCacheSimulation<ComponentCacheDispatchSystem>();
        var entMan = simulation.Resolve<IEntityManager>();
        var entity = SpawnComponentCacheEntity(entMan);

        entMan.AddComponent<CacheAComponent>(entity);
        entMan.EventBus.RaiseLocalEvent(entity, new CacheEvent());
        Assert.That(ComponentCacheDispatchSystem.ValueCount, Is.EqualTo(1));

        entMan.AddComponent<CacheBComponent>(entity);
        entMan.AddComponent<CacheCComponent>(entity);
        var refEvent = new CacheRefEvent();
        entMan.EventBus.RaiseLocalEvent(entity, ref refEvent);
        Assert.That(refEvent.Count, Is.EqualTo(3));
    }

    [Test]
    public void DirectedComponentCacheAllowsNestedSameEntityDispatchWithoutMutation()
    {
        var simulation = NewComponentCacheSimulation<ComponentCacheNestedSystem>();
        var entMan = simulation.Resolve<IEntityManager>();
        var entity = SpawnComponentCacheEntity(entMan);
        entMan.AddComponent<CacheAComponent>(entity);

        entMan.EventBus.RaiseLocalEvent(entity, new CacheEvent());

        Assert.That(ComponentCacheNestedSystem.OuterCount, Is.EqualTo(1));
        Assert.That(ComponentCacheNestedSystem.NestedCount, Is.EqualTo(1));
    }

    [Test]
    public void DirectedComponentCacheAllowsOtherEntityEventsAndMutation()
    {
        var simulation = NewComponentCacheSimulation<ComponentCacheOtherEntitySystem>();
        var entMan = simulation.Resolve<IEntityManager>();
        var first = SpawnComponentCacheEntity(entMan);
        var second = SpawnComponentCacheEntity(entMan);
        entMan.AddComponent<CacheAComponent>(first);
        entMan.AddComponent<CacheBComponent>(second);

        ComponentCacheOtherEntitySystem.Other = second;
        entMan.EventBus.RaiseLocalEvent(first, new CacheEvent());

        Assert.That(ComponentCacheOtherEntitySystem.OtherEventCount, Is.EqualTo(1));
        Assert.That(entMan.HasComponent<CacheCComponent>(second), Is.True);
    }

#if DEBUG
    [TestCase(ComponentCacheMutationKind.Add)]
    [TestCase(ComponentCacheMutationKind.RemoveImmediate)]
    [TestCase(ComponentCacheMutationKind.RemoveDeferredCull)]
    [TestCase(ComponentCacheMutationKind.Replace)]
    [TestCase(ComponentCacheMutationKind.DeleteEntity)]
    public void DirectedComponentCacheAssertsSameEntityMutationInDebug(ComponentCacheMutationKind mutation)
    {
        var simulation = NewComponentCacheSimulation<ComponentCacheMutationSystem>();
        var entMan = simulation.Resolve<IEntityManager>();
        var entity = SpawnComponentCacheEntity(entMan);
        entMan.AddComponent<CacheAComponent>(entity);

        ComponentCacheMutationSystem.Mutation = mutation;

        Assert.Throws<DebugAssertException>(() => entMan.EventBus.RaiseLocalEvent(entity, new CacheEvent()));
    }
#endif

    private static ISimulation NewComponentCacheSimulation<TSystem>()
        where TSystem : EntitySystem, new()
    {
        ComponentCacheDispatchSystem.ValueCount = 0;
        ComponentCacheNestedSystem.OuterCount = 0;
        ComponentCacheNestedSystem.NestedCount = 0;
        ComponentCacheOtherEntitySystem.Other = default;
        ComponentCacheOtherEntitySystem.OtherEventCount = 0;
        ComponentCacheMutationSystem.Mutation = default;

        return RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(factory =>
            {
                factory.RegisterClass<CacheAComponent>();
                factory.RegisterClass<CacheBComponent>();
                factory.RegisterClass<CacheCComponent>();
            })
            .RegisterEntitySystems(factory => factory.LoadExtraSystemType<TSystem>())
            .InitializeInstance();
    }

    private static EntityUid SpawnComponentCacheEntity(IEntityManager entMan)
    {
        entMan.System<SharedMapSystem>().CreateMap(out var map);
        return entMan.Spawn(null, new MapCoordinates(0, 0, map));
    }

    [Reflect(false)]
    private sealed class ComponentCacheDispatchSystem : EntitySystem
    {
        public static int ValueCount;

        public override void Initialize()
        {
            SubscribeLocalEvent<CacheAComponent, CacheEvent>((_, _, _) => ValueCount++);
            SubscribeLocalEvent<CacheAComponent, CacheRefEvent>(OnRef);
            SubscribeLocalEvent<CacheBComponent, CacheRefEvent>(OnRef);
            SubscribeLocalEvent<CacheCComponent, CacheRefEvent>(OnRef);
        }

        private static void OnRef<T>(EntityUid uid, T component, ref CacheRefEvent ev)
        {
            ev.Count++;
        }
    }

    [Reflect(false)]
    private sealed class ComponentCacheNestedSystem : EntitySystem
    {
        public static int OuterCount;
        public static int NestedCount;

        public override void Initialize()
        {
            SubscribeLocalEvent<CacheAComponent, CacheEvent>(OnOuter);
            SubscribeLocalEvent<CacheAComponent, CacheNestedEvent>((_, _, _) => NestedCount++);
        }

        private void OnOuter(EntityUid uid, CacheAComponent component, CacheEvent ev)
        {
            OuterCount++;
            RaiseLocalEvent(uid, new CacheNestedEvent());
        }
    }

    [Reflect(false)]
    private sealed class ComponentCacheOtherEntitySystem : EntitySystem
    {
        public static EntityUid Other;
        public static int OtherEventCount;

        public override void Initialize()
        {
            SubscribeLocalEvent<CacheAComponent, CacheEvent>(OnA);
            SubscribeLocalEvent<CacheBComponent, CacheOtherEvent>((_, _, _) => OtherEventCount++);
        }

        private void OnA(EntityUid uid, CacheAComponent component, CacheEvent ev)
        {
            RaiseLocalEvent(Other, new CacheOtherEvent());
            EntityManager.AddComponent<CacheCComponent>(Other);
        }
    }

    [Reflect(false)]
    private sealed class ComponentCacheMutationSystem : EntitySystem
    {
        public static ComponentCacheMutationKind Mutation;

        public override void Initialize()
        {
            SubscribeLocalEvent<CacheAComponent, CacheEvent>(OnEvent);
        }

        private void OnEvent(EntityUid uid, CacheAComponent component, CacheEvent ev)
        {
            switch (Mutation)
            {
                case ComponentCacheMutationKind.Add:
                    EntityManager.AddComponent<CacheBComponent>(uid);
                    break;
                case ComponentCacheMutationKind.RemoveImmediate:
                    EntityManager.RemoveComponent<CacheAComponent>(uid);
                    break;
                case ComponentCacheMutationKind.RemoveDeferredCull:
                    EntityManager.RemoveComponentDeferred<CacheAComponent>(uid);
                    EntityManager.CullRemovedComponents();
                    break;
                case ComponentCacheMutationKind.Replace:
                    EntityManager.AddComponent(uid, new CacheAComponent(), overwrite: true);
                    break;
                case ComponentCacheMutationKind.DeleteEntity:
                    EntityManager.DeleteEntity(uid);
                    break;
            }
        }
    }

    public enum ComponentCacheMutationKind
    {
        Add,
        RemoveImmediate,
        RemoveDeferredCull,
        Replace,
        DeleteEntity
    }

    private sealed partial class CacheAComponent : Component;
    private sealed partial class CacheBComponent : Component;
    private sealed partial class CacheCComponent : Component;

    private sealed class CacheEvent;
    private sealed class CacheNestedEvent;
    private sealed class CacheOtherEvent;

    [ByRefEvent]
    private struct CacheRefEvent
    {
        public int Count;
    }
}
