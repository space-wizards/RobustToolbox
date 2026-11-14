using System.Numerics;
using NUnit.Framework;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Client.GameObjects.Components;

[TestFixture]
[TestOf(typeof(TransformSystem))]
public sealed class TransformInterpolationTests : RobustIntegrationTest
{
    // At 30 ticks/s these frames are respectively 60, 30, and 20 FPS apart.
    // The network cases and the wrap are checking prediction.
    [TestCase(false, 0.75f, 0.25f)]
    [TestCase(false, 0.25f, 0.25f)]
    [TestCase(false, 0.25f, 0.75f)]
    [TestCase(true, 0.75f, 0.25f)]
    [TestCase(true, 0.25f, 0.25f)]
    [TestCase(true, 0.25f, 0.75f)]
    public async Task StoppedMovementReachesEndpointAfterTick(bool predicted, float firstPhase, float nextPhase)
    {
        var client = StartClient();
        await client.WaitIdleAsync();
        await client.WaitPost(() => client.Resolve<IBaseClient>().StartSinglePlayer());

        await client.WaitAssertion(() =>
        {
            var entities = client.EntMan;
            var transforms = entities.System<TransformSystem>();
            var maps = entities.System<SharedMapSystem>();
            var timing = client.Resolve<IClientGameTiming>();
            var map = maps.CreateMap();
            var uid = entities.SpawnAttachedTo(null, new EntityCoordinates(map, Vector2.Zero));
            var xform = entities.GetComponent<TransformComponent>(uid);
            transforms.ResetRenderTransforms();

            timing.TickRate = 30;
            timing.TickTimingAdjustment = 0f;
            timing.LastRealTick = timing.LastProcessedTick = new GameTick(10);
            timing.CurTick = new GameTick(predicted ? 14u : 10u);
            timing.InSimulation = true;

            // One movement tick followed by a stationary tick.
            if (!predicted)
                timing.StartStateApplication();
            transforms.SetLocalPosition(uid, new Vector2(0.1f, 0f), xform);
            if (!predicted)
                timing.EndStateApplication();

            // GameLoop increments CurTick before rendering.
            timing.CurTick += 1;
            timing.InSimulation = false;
            timing.TickRemainder = timing.TickPeriod * firstPhase;
            transforms.FrameUpdate(0f);
            var firstPosition = transforms.GetRenderWorldPosition(uid);
            Assert.That(firstPosition.X, Is.EqualTo(0.1f * firstPhase).Within(0.00001f));

            // The next simulation tick has finished with no additional movement.
            timing.CurTick += 1;
            timing.LastRealTick += 1;
            timing.LastProcessedTick += 1;
            timing.TickRemainder = timing.TickPeriod * nextPhase;
            transforms.FrameUpdate((float) timing.TickPeriod.TotalSeconds * (1f + nextPhase - firstPhase));
            var stoppedPosition = transforms.GetRenderWorldPosition(uid);

            Assert.That(stoppedPosition.X, Is.EqualTo(xform.LocalPosition.X).Within(0.00001f),
                "Movement has stopped for a tick, but rendering still trails the endpoint.");
        });
    }

    [Test]
    public async Task StoppedMovementReachesEndpointBeforeFirstRender()
    {
        var client = StartClient();
        await client.WaitIdleAsync();
        await client.WaitPost(() => client.Resolve<IBaseClient>().StartSinglePlayer());
        await client.WaitAssertion(() =>
        {
            var transforms = client.System<TransformSystem>();
            var map = client.System<SharedMapSystem>().CreateMap();
            var uid = client.EntMan.SpawnAttachedTo(null, new EntityCoordinates(map, Vector2.Zero));
            var timing = client.Resolve<IClientGameTiming>();
            transforms.ResetRenderTransforms();
            timing.LastRealTick = timing.LastProcessedTick = new GameTick(10);
            timing.CurTick = new GameTick(14);
            timing.InSimulation = true;
            transforms.SetLocalPosition(uid, new Vector2(0.1f, 0f));

            // A slow frame runs both the movement tick and a stationary tick
            // before displaying this segment for the first time.
            timing.CurTick = new GameTick(16);
            timing.InSimulation = false;
            timing.TickRemainder = timing.TickPeriod / 4;
            transforms.FrameUpdate(0f);
            Assert.That(transforms.GetRenderWorldPosition(uid).X, Is.EqualTo(0.1f).Within(0.00001f));
        });
    }
}
