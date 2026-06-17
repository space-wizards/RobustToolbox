using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.GameObjects;

[Reflect(false)]
internal sealed partial class StaggeredUpdateComponent : Component, IStaggeredUpdate
{
    public static TimeSpan UpdateInterval => TimeSpan.FromSeconds(1);
    public static TimeSpan MaxInitialDelay => UpdateInterval;
}

[Reflect(false)]
internal sealed partial class ZeroIntervalStaggeredUpdateComponent : Component, IStaggeredUpdate
{
    public static TimeSpan UpdateInterval => TimeSpan.Zero;
    public static TimeSpan MaxInitialDelay => UpdateInterval;
}

[Reflect(false)]
internal sealed partial class NegativeIntervalStaggeredUpdateComponent : Component, IStaggeredUpdate
{
    public static TimeSpan UpdateInterval => TimeSpan.FromTicks(-1);
    public static TimeSpan MaxInitialDelay => UpdateInterval;
}

[TestFixture, Parallelizable, TestOf(typeof(EntitySystem))]
public sealed class EntitySystemStaggeredUpdateUnit
{
    private const int TickRate = 10;
    private static MapInitEvent _mapInitEventInstance = new();

    private readonly Dictionary<EntityUid, IComponent> _components = [];
    private readonly Dictionary<EntityUid, IComponent> _metas = [];

    private StaggeredUpdateTracker<StaggeredUpdateComponent> _updateTracker = null!;
    private Mock<IRobustRandom> _random = null!;
    private GameTiming _timing = null!;

    [SetUp]
    public void Before()
    {
        _random = new Mock<IRobustRandom>();
        _timing = new GameTiming { TickRate = TickRate };
        _updateTracker = CreateUpdateTracker<StaggeredUpdateComponent>();
    }

    [TearDown]
    public void After()
    {
        _components.Clear();
        _metas.Clear();
    }

    [Test]
    public void TestRemovedComponentsAreUntracked()
    {
        var entity = new EntityUid(1);
        var comp = CreateComponent(entity);

        _timing.CurTick += _timing.TickRate;
        Assert.That(ToList(_updateTracker), Contains.Item((entity, comp)));

        _timing.CurTick += _timing.TickRate;
        _components.Remove(entity);
        Assert.That(ToList(_updateTracker), Is.Empty);

        _timing.CurTick += _timing.TickRate;
        _components.Add(entity, comp);
        Assert.That(
            ToList(_updateTracker),
            Is.Empty,
            "Do not return the entity, even when the component is added back");
    }

    [Test]
    public void TestDoubleAdd()
    {
        var entity = new EntityUid(1);
        var comp = CreateComponent(entity);

        _components.Remove(entity);
        _timing.CurTick += 1;
        Assert.That(ToList(_updateTracker), Is.Empty);

        _components.Add(entity, comp);
        MapInit(entity, comp);
        _timing.CurTick += _timing.TickRate;
        Assert.That(ToList(_updateTracker), Has.Exactly(1).EqualTo((entity, comp)));
    }

    [Test]
    public void TestRegularUpdate()
    {
        var entity = new EntityUid(1);
        SetRandomOffset(1);
        var comp = CreateComponent(entity);

        _timing.CurTick += 1;
        Assert.That(ToList(_updateTracker), Is.Empty);

        _timing.CurTick += 1;
        Assert.That(
            ToList(_updateTracker),
            Contains.Item((entity, comp)),
            "Update after exactly offset + 1 ticks");

        _timing.CurTick += (byte)(_timing.TickRate - 1);
        Assert.That(
            ToList(_updateTracker),
            Is.Empty,
            "Only return entity once until time is advanced by a full second");

        _timing.CurTick += 1;
        Assert.That(
            ToList(_updateTracker),
            Contains.Item((entity, comp)),
            "Update exactly one second after previous update");
    }

    [Test]
    public void TestPausedEntityIsSkippedUntilNextInterval()
    {
        var entity = new EntityUid(1);
        SetRandomOffset(0);
        var comp = CreateComponent(entity);

        ((MetaDataComponent)_metas[entity]).PauseTime = TimeSpan.MinValue;
        _timing.CurTick += 1;
        Assert.That(
            ToList(_updateTracker),
            Is.Empty,
            "Paused entities do not update even when their scheduled time has elapsed");

        ((MetaDataComponent)_metas[entity]).PauseTime = null;
        _timing.CurTick += (byte)(_timing.TickRate - 1);
        Assert.That(
            ToList(_updateTracker),
            Is.Empty,
            "The skipped update is not replayed immediately when the entity is unpaused");

        _timing.CurTick += 1;
        Assert.That(
            ToList(_updateTracker),
            Contains.Item((entity, comp)),
            "The entity updates again on its next scheduled interval after being unpaused");
    }

    [Test]
    public void TestDifferentRandomOffsetsUpdateIndependently()
    {
        var earlyEntity = new EntityUid(1);
        var lateEntity = new EntityUid(2);

        _random.SetupSequence(m => m.Next(TimeSpan.Zero, It.IsAny<TimeSpan>()))
            .Returns(_timing.TickPeriod.Mul(0))
            .Returns(_timing.TickPeriod.Mul(5));

        var earlyComp = CreateComponent(earlyEntity);
        var lateComp = CreateComponent(lateEntity);

        _timing.CurTick += 1;
        Assert.That(ToList(_updateTracker), Is.EqualTo([(earlyEntity, earlyComp)]));

        _timing.CurTick += 4;
        Assert.That(
            ToList(_updateTracker),
            Is.Empty,
            "The later entity should not update before its own randomized offset has elapsed");

        _timing.CurTick += 1;
        Assert.That(ToList(_updateTracker), Is.EqualTo([(lateEntity, lateComp)]));
    }

    [Test]
    public void TestChainedMapInitHandlerIsInvoked()
    {
        Entity<StaggeredUpdateComponent>? eventEntity = null;
        var mapInitCalls = 0;
        _updateTracker = CreateUpdateTracker<StaggeredUpdateComponent>(
            (ent, ref _) =>
            {
                eventEntity = ent;
                mapInitCalls++;
            });

        var entity = new EntityUid(1);
        var comp = CreateComponent(entity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapInitCalls, Is.EqualTo(1));
            Assert.That(eventEntity, Is.EqualTo(WrapEnt(entity, comp)));
        }
    }

    [Test]
    public void TestUpdateIntervalMustBePositive()
    {
        Assert.Throws<InvalidOperationException>(() => CreateUpdateTracker<ZeroIntervalStaggeredUpdateComponent>());
        Assert.Throws<InvalidOperationException>(() => CreateUpdateTracker<NegativeIntervalStaggeredUpdateComponent>());
    }

    private void SetRandomOffset(int ticks)
    {
        _random.Setup(m => m.Next(TimeSpan.Zero, It.IsAny<TimeSpan>()))
            .Returns(_timing.TickPeriod.Mul(ticks));
    }

    private StaggeredUpdateTracker<TComp> CreateUpdateTracker<TComp>(
        EntityEventRefHandler<TComp, MapInitEvent>? chainedHandler = null) where TComp : IComponent, IStaggeredUpdate
    {
        var compQuery = new EntityQuery<TComp>(null, _components);
        var metaQuery = new EntityQuery<MetaDataComponent>(null, _metas);

        var tracker = new StaggeredUpdateTracker<TComp>(
            chainedHandler,
            compQuery,
            metaQuery,
            _random.Object,
            _timing);

        return tracker;
    }

    private StaggeredUpdateComponent CreateComponent(EntityUid entity)
    {
        var comp = new StaggeredUpdateComponent();
        _components.Add(entity, comp);
        _metas.Add(entity, new MetaDataComponent());
        MapInit(entity, comp);
        return comp;
    }

    private void MapInit(EntityUid entity, StaggeredUpdateComponent comp)
    {
        _updateTracker.OnMapInit(WrapEnt(entity, comp), ref _mapInitEventInstance);
    }

    private static List<(EntityUid, TComp)> ToList<TComp>(StaggeredUpdateTracker<TComp> tracker)
        where TComp : IComponent, IStaggeredUpdate
    {
        var ret = new List<(EntityUid, TComp)>();
        var t = tracker.GetEnumerator();
        while (t.MoveNext(out var uid, out var comp))
        {
            ret.Add((uid, comp));
        }

        return ret;
    }

    private Entity<StaggeredUpdateComponent> WrapEnt(EntityUid entity, StaggeredUpdateComponent comp)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        comp.Owner = entity; // have to set this to pass AssertOwner check
#pragma warning restore CS0618 // Type or member is obsolete

        return new Entity<StaggeredUpdateComponent>(entity, comp);
    }
}
