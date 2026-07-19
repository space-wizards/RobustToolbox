using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Timing;

[TestFixture, NonParallelizable, TestOf(typeof(EntityTimerSystem))]
internal sealed partial class EntityTimerSystemTest
{
    private static readonly EntityTimerId FirstId = new("first");
    private static readonly EntityTimerId SecondId = new("second");
    private static readonly EntityTimerId SharedId = new("shared");
    private static readonly EntityTimerId RepeatId = new("repeat");
    private static readonly EntityTimerId ParentId = new("parent");
    private static readonly EntityTimerId ChildId = new("child");

    [Test]
    public void MultipleComponentsReplaceCancelAndEqualDeadlineOrder()
    {
        var simulation = new TimerSimulation();
        var (uid, first, second) = simulation.SpawnOwner();
        var deadline = TimeSpan.FromSeconds(10);

        simulation.Timers.SetTimerAt<TimerAComponent>((uid, first), FirstId, TimeSpan.FromSeconds(5));
        simulation.Timers.SetTimerAt<TimerAComponent>((uid, first), FirstId, deadline);
        simulation.Timers.SetTimerAt<TimerBComponent>((uid, second), SharedId, deadline);
        simulation.Timers.SetTimerAt<TimerAComponent>((uid, first), SecondId, deadline);
        simulation.Timers.SetTimerAt<TimerAComponent>((uid, first), SharedId, deadline);

        Assert.Multiple(() =>
        {
            Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, FirstId, out var timer), Is.True);
            Assert.That(timer.Deadline, Is.EqualTo(deadline));
            Assert.That(timer.Remaining, Is.EqualTo(deadline));
            Assert.That(simulation.Timers.TryGetTimer<TimerBComponent>(uid, SharedId, out _), Is.True);
            Assert.That(simulation.Timers.CancelTimer<TimerAComponent>(uid, SecondId), Is.True);
            Assert.That(simulation.Timers.CancelTimer<TimerAComponent>(uid, SecondId), Is.False);
        });

        simulation.Now = TimeSpan.FromSeconds(5);
        Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, FirstId, out var halfway), Is.True);
        Assert.That(halfway.Remaining, Is.EqualTo(TimeSpan.FromSeconds(5)));
        simulation.Update();
        Assert.That(simulation.System.Events, Is.Empty);

        simulation.Now = deadline;
        simulation.Update();

        Assert.That(simulation.System.Events, Is.EqualTo(new[]
        {
            new FiredTimer("A", FirstId, 1, null),
            new FiredTimer("B", SharedId, 1, null),
            new FiredTimer("A", SharedId, 1, null),
        }));
    }

    [Test]
    public void RepeatingTimerPreservesPhaseAndCanBeCancelledByCallback()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();
        simulation.System.CancelOnFire = RepeatId;

        simulation.Timers.SetTimerAt<TimerAComponent>(
            (uid, first),
            RepeatId,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));

        simulation.Now = TimeSpan.FromSeconds(17);
        simulation.Update();

        Assert.Multiple(() =>
        {
            Assert.That(simulation.System.Events, Is.EqualTo(new[]
            {
                new FiredTimer("A", RepeatId, 3, TimeSpan.FromSeconds(20)),
            }));
            Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, RepeatId, out _), Is.False);
        });
    }

    [Test]
    public void TimerScheduledByCallbackWaitsUntilNextUpdate()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();
        simulation.System.ScheduleChildOnFire = ParentId;

        simulation.Timers.SetTimer<TimerAComponent>((uid, first), ParentId, TimeSpan.Zero);
        simulation.Update();

        Assert.That(simulation.System.Events, Is.EqualTo(new[]
        {
            new FiredTimer("A", ParentId, 1, null),
        }));

        simulation.Update();
        Assert.That(simulation.System.Events, Has.Count.EqualTo(1));

        simulation.Tick++;
        simulation.Update();

        Assert.That(simulation.System.Events, Is.EqualTo(new[]
        {
            new FiredTimer("A", ParentId, 1, null),
            new FiredTimer("A", ChildId, 1, null),
        }));
    }

    [Test]
    public void EntityPauseSuspendsTimersUnlessIgnored()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();

        simulation.Timers.SetTimer<TimerAComponent>((uid, first), FirstId, TimeSpan.FromSeconds(5));
        simulation.Timers.SetTimer<TimerAComponent>(
            (uid, first),
            SecondId,
            TimeSpan.FromSeconds(5),
            flags: EntityTimerFlags.IgnoreEntityPause);

        simulation.MetaData.SetEntityPaused(uid, true);
        Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, FirstId, out var suspended), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(suspended.Suspended, Is.True);
            Assert.That(suspended.Remaining, Is.EqualTo(TimeSpan.FromSeconds(5)));
        });

        simulation.Now = TimeSpan.FromSeconds(10);
        Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, FirstId, out suspended), Is.True);
        Assert.That(suspended.Remaining, Is.EqualTo(TimeSpan.FromSeconds(5)));
        simulation.Update();
        Assert.That(simulation.System.Events, Is.EqualTo(new[]
        {
            new FiredTimer("A", SecondId, 1, null),
        }));

        simulation.MetaData.SetEntityPaused(uid, false);
        Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, FirstId, out var resumed), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(resumed.Suspended, Is.False);
            Assert.That(resumed.Deadline, Is.EqualTo(TimeSpan.FromSeconds(15)));
            Assert.That(resumed.Remaining, Is.EqualTo(TimeSpan.FromSeconds(5)));
        });

        simulation.Now = TimeSpan.FromSeconds(15);
        simulation.Update();
        Assert.That(simulation.System.Events[^1], Is.EqualTo(new FiredTimer("A", FirstId, 1, null)));
    }

    [Test]
    public void RemainingIsClampedAndRepeatingTimerReportsNextOccurrence()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();
        simulation.Timers.SetTimerAt<TimerAComponent>(
            (uid, first),
            RepeatId,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));

        simulation.Now = TimeSpan.FromSeconds(17);
        Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, RepeatId, out var overdue), Is.True);
        Assert.That(overdue.Remaining, Is.EqualTo(TimeSpan.Zero));

        simulation.Update();

        Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, RepeatId, out var repeated), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(repeated.Deadline, Is.EqualTo(TimeSpan.FromSeconds(20)));
            Assert.That(repeated.Remaining, Is.EqualTo(TimeSpan.FromSeconds(3)));
            Assert.That(repeated.Interval, Is.EqualTo(TimeSpan.FromSeconds(5)));
        });
    }

    [Test]
    public void ComponentAndEntityRemovalCancelOwnedTimers()
    {
        var simulation = new TimerSimulation();
        var (uid, first, second) = simulation.SpawnOwner();
        simulation.Timers.SetTimer<TimerAComponent>((uid, first), FirstId, TimeSpan.FromSeconds(5));
        simulation.Timers.SetTimer<TimerBComponent>((uid, second), FirstId, TimeSpan.FromSeconds(5));

        simulation.Entities.RemoveComponent<TimerAComponent>(uid);

        Assert.Multiple(() =>
        {
            Assert.That(simulation.Timers.TryGetTimer<TimerAComponent>(uid, FirstId, out _), Is.False);
            Assert.That(simulation.Timers.TryGetTimer<TimerBComponent>(uid, FirstId, out _), Is.True);
        });

        simulation.Entities.DeleteEntity(uid);
        Assert.That(simulation.Timers.CancelTimers(uid), Is.Zero);

        simulation.Now = TimeSpan.FromSeconds(5);
        simulation.Update();
        Assert.That(simulation.System.Events, Is.Empty);
    }

    [Test]
    public void EntityFlushClearsAllTimers()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();
        simulation.Timers.SetTimer<TimerAComponent>((uid, first), FirstId, TimeSpan.FromSeconds(5));

        simulation.Entities.FlushEntities();

        Assert.That(simulation.Timers.CancelTimers(uid), Is.Zero);
        simulation.Now = TimeSpan.FromSeconds(5);
        simulation.Update();
        Assert.That(simulation.System.Events, Is.Empty);
    }

    [Test]
    public void ServerProcessesAuthoritativeTimerOnceWhenNoPredictions()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();
        simulation.Timers.SetTimer<TimerAComponent>((uid, first), FirstId, TimeSpan.Zero);

        simulation.Timers.UpdateTimers(noPredictions: true);
        simulation.Timers.UpdateTimers(noPredictions: true);

        Assert.That(simulation.System.Events, Is.EqualTo(new[]
        {
            new FiredTimer("A", FirstId, 1, null),
        }));
    }

    [Test]
    public void EntitySystemManagerDispatchesTimersBeforeSystemUpdates()
    {
        var simulation = new TimerSimulation();
        var (uid, first, _) = simulation.SpawnOwner();
        simulation.System.CheckTimerBeforeUpdate = FirstId;
        simulation.Timers.SetTimer<TimerAComponent>((uid, first), FirstId, TimeSpan.Zero);

        simulation.Entities.TickUpdate(0f, noPredictions: false);

        Assert.That(simulation.System.TimerWasDispatchedBeforeUpdate, Is.True);
    }

    private sealed class TimerSimulation
    {
        public readonly ISimulation Simulation;
        public readonly IEntityManager Entities;
        public readonly EntityTimerSystem Timers;
        public readonly MetaDataSystem MetaData;
        public readonly TimerTestSystem System;
        public TimeSpan Now;
        public uint Tick = 1;

        public TimerSimulation()
        {
            var timing = new Mock<IGameTiming>();
            timing.SetupGet(x => x.CurTime).Returns(() => Now);
            timing.SetupGet(x => x.CurTick).Returns(() => new GameTick(Tick));

            Simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterDependencies(dependencies =>
                    dependencies.RegisterInstance<IGameTiming>(timing.Object, overwrite: true))
                .RegisterComponents(factory =>
                {
                    factory.RegisterClass<TimerAComponent>();
                    factory.RegisterClass<TimerBComponent>();
                })
                .RegisterEntitySystems(factory => factory.LoadExtraSystemType<TimerTestSystem>())
                .InitializeInstance();

            Entities = Simulation.Resolve<IEntityManager>();
            Timers = Simulation.System<EntityTimerSystem>();
            MetaData = Entities.System<MetaDataSystem>();
            System = Entities.System<TimerTestSystem>();
        }

        public (EntityUid Uid, TimerAComponent First, TimerBComponent Second) SpawnOwner()
        {
            var uid = Entities.SpawnEntity(null, MapCoordinates.Nullspace);
            var first = Entities.AddComponent<TimerAComponent>(uid);
            var second = Entities.AddComponent<TimerBComponent>(uid);
            return (uid, first, second);
        }

        public void Update()
        {
            Timers.UpdateTimers(noPredictions: false);
        }
    }

    [Reflect(false)]
    private sealed partial class TimerTestSystem : EntitySystem
    {
        [Dependency] private EntityTimerSystem _timers = default!;

        public readonly List<FiredTimer> Events = new();
        public EntityTimerId? CancelOnFire;
        public EntityTimerId? ScheduleChildOnFire;
        public EntityTimerId? CheckTimerBeforeUpdate;
        public bool TimerWasDispatchedBeforeUpdate;

        public override void Initialize()
        {
            SubscribeLocalEvent<TimerAComponent, EntityTimerEvent>(OnTimerA);
            SubscribeLocalEvent<TimerBComponent, EntityTimerEvent>(OnTimerB);
        }

        public override void Update(float frameTime)
        {
            if (CheckTimerBeforeUpdate is { } id)
                TimerWasDispatchedBeforeUpdate = Events.Exists(timer => timer.Id == id);
        }

        private void OnTimerA(EntityUid uid, TimerAComponent component, ref EntityTimerEvent args)
        {
            Events.Add(new FiredTimer("A", args.Id, args.ElapsedCount, args.NextDeadline));

            if (args.Id == CancelOnFire)
                _timers.CancelTimer<TimerAComponent>(uid, args.Id);

            if (args.Id == ScheduleChildOnFire)
                _timers.SetTimer<TimerAComponent>((uid, component), ChildId, TimeSpan.Zero);
        }

        private void OnTimerB(EntityUid uid, TimerBComponent component, ref EntityTimerEvent args)
        {
            Events.Add(new FiredTimer("B", args.Id, args.ElapsedCount, args.NextDeadline));
        }
    }

    [Reflect(false)]
    private sealed partial class TimerAComponent : Component;

    [Reflect(false)]
    private sealed partial class TimerBComponent : Component;

    private readonly record struct FiredTimer(
        string Component,
        EntityTimerId Id,
        uint ElapsedCount,
        TimeSpan? NextDeadline);
}
