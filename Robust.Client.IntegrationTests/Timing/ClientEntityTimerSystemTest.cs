using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Client.Timing;

[TestFixture, NonParallelizable, TestOf(typeof(EntityTimerSystem))]
internal sealed class ClientEntityTimerSystemTest : RobustUnitTest
{
    protected override Type[]? ExtraComponents => [typeof(ClientTimerComponent)];
    protected override Type[]? ExtraSystems => [typeof(ClientEntityTimerSystem), typeof(ClientTimerTestSystem)];

    public override UnitTestProject Project => UnitTestProject.Client;

    [Test]
    public void PredictionQueuesAndStateApplication()
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        var timers = entities.System<EntityTimerSystem>();
        var timing = IoCManager.Resolve<IClientGameTiming>();
        var system = entities.System<ClientTimerTestSystem>();
        var uid = entities.SpawnEntity(null, MapCoordinates.Nullspace);
        var component = entities.AddComponent<ClientTimerComponent>(uid);

        var predicted = new EntityTimerId("predicted");
        var outside = new EntityTimerId("outside");
        timers.SetTimer<ClientTimerComponent>((uid, component), predicted, TimeSpan.Zero);
        timers.SetTimer<ClientTimerComponent>(
            (uid, component),
            outside,
            TimeSpan.Zero,
            flags: EntityTimerFlags.UpdatesOutsidePrediction);

        timers.UpdateTimers(noPredictions: true);
        Assert.That(system.Events, Is.EqualTo(new[] { outside }));

        timers.UpdateTimers(noPredictions: false);
        Assert.That(system.Events, Is.EqualTo(new[] { outside, predicted }));

        var applyingPredicted = new EntityTimerId("applying-predicted");
        var applyingOutside = new EntityTimerId("applying-outside");
        timers.SetTimer<ClientTimerComponent>((uid, component), applyingPredicted, TimeSpan.Zero);
        timers.SetTimer<ClientTimerComponent>(
            (uid, component),
            applyingOutside,
            TimeSpan.Zero,
            flags: EntityTimerFlags.UpdatesOutsidePrediction);

        timing.StartStateApplication();
        timers.UpdateTimers(noPredictions: false);
        Assert.That(system.Events, Has.Count.EqualTo(2));
        timing.EndStateApplication();

        timers.UpdateTimers(noPredictions: false);
        Assert.That(system.Events, Is.EqualTo(new[]
        {
            outside,
            predicted,
            applyingPredicted,
            applyingOutside,
        }));

        var pastPrediction = new EntityTimerId("past-prediction");
        timers.SetTimer<ClientTimerComponent>((uid, component), pastPrediction, TimeSpan.Zero);
        timing.StartPastPrediction();
        timers.UpdateTimers(noPredictions: false);
        timing.EndPastPrediction();

        Assert.That(system.Events[^1], Is.EqualTo(pastPrediction));
    }
}

internal sealed partial class ClientTimerTestSystem : EntitySystem
{
    public readonly List<EntityTimerId> Events = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ClientTimerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTimer(EntityUid uid, ClientTimerComponent component, ref EntityTimerEvent args)
    {
        Events.Add(args.Id);
    }
}

internal sealed partial class ClientTimerComponent : Component;
