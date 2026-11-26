using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects;

[TestFixture, TestOf(typeof(Archetype))]
internal sealed partial class ArchetypeTests
{
    private static (EntityManager EntMan, ArchetypeTestSystem Sys) Setup()
    {
        var sim = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(f =>
            {
                f.RegisterClass<ArchAComponent>();
                f.RegisterClass<ArchBComponent>();
                f.RegisterClass<ArchCComponent>();
            })
            .RegisterEntitySystems(f => f.LoadExtraSystemType<ArchetypeTestSystem>())
            .InitializeInstance();

        var entMan = (EntityManager) sim.Resolve<IEntityManager>();
        return (entMan, entMan.System<ArchetypeTestSystem>());
    }

    [Test]
    public void SameComponentsShareArchetype()
    {
        var (entMan, _) = Setup();

        var a = entMan.Spawn();
        var b = entMan.Spawn();

        // Different add order, same set.
        entMan.AddComponent<ArchAComponent>(a);
        entMan.AddComponent<ArchBComponent>(a);
        entMan.AddComponent<ArchBComponent>(b);
        entMan.AddComponent<ArchAComponent>(b);

        var archA = entMan.GetArchetypeIndex(a);
        Assert.That(archA, Is.Not.EqualTo(EntityManager.NoArchetype));
        Assert.That(entMan.GetArchetypeIndex(b), Is.EqualTo(archA));

        var archetype = entMan.GetArchetype(archA);
        Assert.That(archetype.Has(CompIdx.Index<ArchAComponent>()));
        Assert.That(archetype.Has(CompIdx.Index<ArchBComponent>()));
        Assert.That(archetype.Has(CompIdx.Index<ArchCComponent>()), Is.False);

        // Removing and re-adding round trips to the same archetypes.
        entMan.RemoveComponent<ArchBComponent>(a);
        var without = entMan.GetArchetypeIndex(a);
        Assert.That(without, Is.Not.EqualTo(archA));
        Assert.That(entMan.GetArchetype(without).Has(CompIdx.Index<ArchBComponent>()), Is.False);

        entMan.AddComponent<ArchBComponent>(a);
        Assert.That(entMan.GetArchetypeIndex(a), Is.EqualTo(archA));

        entMan.DeleteEntity(a);
        Assert.That(entMan.GetArchetypeIndex(a), Is.EqualTo(EntityManager.NoArchetype));
    }

    [Test]
    public void RemoveComponentMidEvent()
    {
        var (entMan, sys) = Setup();
        var uid = entMan.Spawn();
        entMan.AddComponent<ArchAComponent>(uid);
        entMan.AddComponent<ArchBComponent>(uid);

        // Whichever of A / B runs first removes the other, so only one handler should run.
        sys.Mode = TestMode.RemoveOther;
        var ev = new ArchTestEvent();
        entMan.EventBus.RaiseLocalEvent(uid, ref ev);
        Assert.That(ev.Count, Is.EqualTo(1));

        // Ordered path.
        entMan.EnsureComponent<ArchAComponent>(uid);
        entMan.EnsureComponent<ArchBComponent>(uid);
        var ordered = new ArchOrderedTestEvent();
        entMan.EventBus.RaiseLocalEvent(uid, ref ordered);
        Assert.That(ordered.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddComponentMidEvent()
    {
        var (entMan, sys) = Setup();
        var uid = entMan.Spawn();
        entMan.AddComponent<ArchAComponent>(uid);

        sys.Mode = TestMode.AddCu;
        var ev = new ArchTestEvent();
        entMan.EventBus.RaiseLocalEvent(uid, ref ev);
        Assert.That(ev.Count, Is.EqualTo(1));
        Assert.That(entMan.HasComponent<ArchCComponent>(uid));

        // Next raise sees both.
        sys.Mode = TestMode.None;
        ev = new ArchTestEvent();
        entMan.EventBus.RaiseLocalEvent(uid, ref ev);
        Assert.That(ev.Count, Is.EqualTo(2));
    }

    [Test]
    public void DeleteEntityMidEvent()
    {
        var (entMan, sys) = Setup();
        var uid = entMan.Spawn();
        entMan.AddComponent<ArchAComponent>(uid);
        entMan.AddComponent<ArchBComponent>(uid);
        entMan.AddComponent<ArchCComponent>(uid);

        sys.Mode = TestMode.Delete;
        var ev = new ArchTestEvent();
        Assert.DoesNotThrow(() => entMan.EventBus.RaiseLocalEvent(uid, ref ev));
        Assert.That(ev.Count, Is.EqualTo(1));
        Assert.That(entMan.Deleted(uid));

        // Raising on a deleted entity does nothing.
        ev = new ArchTestEvent();
        entMan.EventBus.RaiseLocalEvent(uid, ref ev);
        Assert.That(ev.Count, Is.EqualTo(0));
    }

    [Test]
    public void ReplacedComponentReceivesEvent()
    {
        var (entMan, sys) = Setup();
        var uid = entMan.Spawn();
        entMan.AddComponent<ArchAComponent>(uid);
        entMan.AddComponent<ArchBComponent>(uid);

        var original = entMan.GetComponent<ArchBComponent>(uid);
        entMan.RemoveComponent<ArchBComponent>(uid);
        var replacement = entMan.AddComponent<ArchBComponent>(uid);

        sys.Mode = TestMode.None;
        sys.LastB = null;
        var ev = new ArchTestEvent();
        entMan.EventBus.RaiseLocalEvent(uid, ref ev);
        Assert.That(ev.Count, Is.EqualTo(2));
        Assert.That(sys.LastB, Is.SameAs(replacement));
        Assert.That(sys.LastB, Is.Not.SameAs(original));
    }

    internal enum TestMode : byte
    {
        None,
        RemoveOther,
        AddCu,
        Delete,
    }

    [Reflect(false)]
    internal sealed class ArchetypeTestSystem : EntitySystem
    {
        public TestMode Mode;
        public ArchBComponent? LastB;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArchAComponent, ArchTestEvent>(OnA);
            SubscribeLocalEvent<ArchBComponent, ArchTestEvent>(OnB);
            SubscribeLocalEvent<ArchCComponent, ArchTestEvent>(OnC);

            SubscribeLocalEvent<ArchAComponent, ArchOrderedTestEvent>(OnOrderedA, before: [typeof(SharedMapSystem)]);
            SubscribeLocalEvent<ArchBComponent, ArchOrderedTestEvent>(OnOrderedB, before: [typeof(SharedMapSystem)]);
        }

        private void OnA(EntityUid uid, ArchAComponent comp, ref ArchTestEvent args)
        {
            args.Count++;
            Act(uid, removeOther: typeof(ArchBComponent));
        }

        private void OnB(EntityUid uid, ArchBComponent comp, ref ArchTestEvent args)
        {
            args.Count++;
            LastB = comp;
            Act(uid, removeOther: typeof(ArchAComponent));
        }

        private void OnC(EntityUid uid, ArchCComponent comp, ref ArchTestEvent args)
        {
            args.Count++;
            if (Mode == TestMode.Delete)
                EntityManager.DeleteEntity(uid);
        }

        private void OnOrderedA(EntityUid uid, ArchAComponent comp, ref ArchOrderedTestEvent args)
        {
            args.Count++;
            Act(uid, removeOther: typeof(ArchBComponent));
        }

        private void OnOrderedB(EntityUid uid, ArchBComponent comp, ref ArchOrderedTestEvent args)
        {
            args.Count++;
            Act(uid, removeOther: typeof(ArchAComponent));
        }

        private void Act(EntityUid uid, System.Type removeOther)
        {
            switch (Mode)
            {
                case TestMode.RemoveOther:
                    EntityManager.RemoveComponent(uid, removeOther);
                    break;
                case TestMode.AddCu:
                    EntityManager.EnsureComponent<ArchCComponent>(uid);
                    break;
                case TestMode.Delete:
                    EntityManager.DeleteEntity(uid);
                    break;
            }
        }
    }

    [ByRefEvent]
    internal struct ArchTestEvent
    {
        public int Count;
    }

    [ByRefEvent]
    internal struct ArchOrderedTestEvent
    {
        public int Count;
    }

    internal sealed partial class ArchAComponent : Component;

    internal sealed partial class ArchBComponent : Component;

    internal sealed partial class ArchCComponent : Component;
}
