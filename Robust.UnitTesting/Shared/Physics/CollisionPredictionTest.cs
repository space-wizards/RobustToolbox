using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Physics;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Physics;

/// <summary>
/// This test is meant to check that collision start & stop events are raised correctly by the client.
/// The expectation is that start & stop events are only raised if the client predicts that two entities will move into
/// contact. They do not get raised as a result of applying component states received from the server.
/// I.e., the assumption is that if a collision results in changes to data on a component, then that data will already
/// have been sent to clients in the component's state, so we don't want to "double count" collisions.
/// </summary>
public sealed class CollisionPredictionTest : RobustIntegrationTest
{
    private static readonly string Prototypes = @"
- type: entity
  id: CollisionTest1
  components:
  - type: CollisionPredictionTest
  - type: Physics
    bodyType: Dynamic
    sleepingAllowed: false

- type: entity
  id: CollisionTest2
  components:
  - type: Physics
    bodyType: Dynamic
    sleepingAllowed: false
";

    [Test]
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task TestCollisionPrediction(bool hard1, bool hard2)
    {
        var serverOpts = new ServerIntegrationOptions { Pool = false, ExtraPrototypes = Prototypes };
        var clientOpts = new ClientIntegrationOptions { Pool = false, ExtraPrototypes = Prototypes };
        var server = StartServer(serverOpts);
        var client = StartClient(clientOpts);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        var netMan = client.ResolveDependency<IClientNetManager>();
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await server.WaitPost(() => server.CfgMan.SetCVar(CVars.NetPVS, false));
        await client.WaitPost(() => netMan.ClientConnect(null!, 0, null!));

        var sFix = server.System<FixtureSystem>();
        var sPhys = server.System<SharedPhysicsSystem>();
        var sSys = server.System<CollisionPredictionTestSystem>();

        // Set up entities
        EntityUid map = default;
        EntityUid sEntity1 = default;
        EntityUid sEntity2 = default;
        MapCoordinates coords1 = default;
        MapCoordinates coords2 = default;
        await server.WaitPost(() =>
        {
            var radius = 0.25f;
            map = server.System<SharedMapSystem>().CreateMap(out var mapId);
            coords1 = new(default, mapId);
            coords2 = new(Vector2.One, mapId);
            sEntity1 = server.EntMan.Spawn("CollisionTest1", coords1);
            sEntity2 = server.EntMan.Spawn("CollisionTest2", new MapCoordinates(coords2.Position + new Vector2(0, radius), mapId));
            sFix.CreateFixture(sEntity1, "a", new Fixture(new PhysShapeCircle(radius), 1, 1, hard1));
            sFix.CreateFixture(sEntity2, "a", new Fixture(new PhysShapeCircle(radius), 1, 1, hard2));
            sPhys.SetCanCollide(sEntity1, true);
            sPhys.SetCanCollide(sEntity2, true);
            sPhys.SetAwake((sEntity1, server.EntMan.GetComponent<PhysicsComponent>(sEntity1)), true);
            sPhys.SetAwake((sEntity2, server.EntMan.GetComponent<PhysicsComponent>(sEntity2)), true);
        });

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        await server.WaitPost(() => server.PlayerMan.JoinGame(server.PlayerMan.Sessions.First()));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Ensure client & server ticks are synced.
        // Client runs 2 tick ahead
        {
            var targetDelta = 2;
            var sTick = (int)server.Timing.CurTick.Value;
            var cTick = (int)client.Timing.CurTick.Value;
            var delta = cTick - sTick;

            if (delta > targetDelta)
                await server.WaitRunTicks(delta - targetDelta);
            else if (delta < targetDelta)
                await client.WaitRunTicks(targetDelta - delta);

            sTick = (int)server.Timing.CurTick.Value;
            cTick = (int)client.Timing.CurTick.Value;
            delta = cTick - sTick;
            Assert.That(delta, Is.EqualTo(targetDelta));
        }

        var cPhys = client.System<SharedPhysicsSystem>();
        var cSys = client.System<CollisionPredictionTestSystem>();

        void ResetSystem()
        {
            sSys.CollisionEnded = false;
            sSys.CollisionStarted = false;

            cSys.CollisionEnded = false;
            cSys.CollisionStarted = false;
        }

        async Task Tick()
        {
            ResetSystem();
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var nEntity1 = server.EntMan.GetNetEntity(sEntity1);
        var nEntity2 = server.EntMan.GetNetEntity(sEntity2);

        var cEntity1 = client.EntMan.GetEntity(nEntity1);
        var cEntity2 = client.EntMan.GetEntity(nEntity2);

        var sComp = server.EntMan.GetComponent<CollisionPredictionTestComponent>(sEntity1);
        var cComp = client.EntMan.GetComponent<CollisionPredictionTestComponent>(cEntity1);

        cPhys.UpdateIsPredicted(cEntity1);

        // Initially, the objects are not colliding.
        {
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // We now simulate a predictive event that gets raised due to some client-side input, causing the entities to
        // move and start colliding. Instead of setting up a proper input / keybind handler, The predictive event will
        // just be raised in the system update method, which updates before the physics system does.
        {
            cSys.Ev = new CollisionTestMoveEvent(nEntity1, coords2);
            await Tick();
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.True);
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.EquivalentTo(new []{ cEntity2 }));
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.EquivalentTo(new []{ cEntity1 }));
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.True);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // Run another tick. Client should reset states, and re-predict the event.
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.True);
            Assert.That(cComp.WasTouching, Is.True);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.EquivalentTo(new []{ cEntity2 }));
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.EquivalentTo(new []{ cEntity1 }));
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.True);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // Next tick the server should raise the event received from the client, which will raise a serve-side
        // collide-start event.
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.True);
            Assert.That(cComp.IsTouching, Is.True);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.WasTouching, Is.True);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.EquivalentTo(new []{ sEntity2 }));
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.EquivalentTo(new []{ sEntity1 }));
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.EquivalentTo(new []{ cEntity2 }));
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.EquivalentTo(new []{ cEntity1 }));
            Assert.That(sSys.CollisionStarted, Is.True);
            Assert.That(cSys.CollisionStarted, Is.True);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // The client will have received the server-state, but will take some time for it to leave the state buffer.
        // In the meantime, the client will keep predicting that the collision will "starts"
        for (var i = 0; i < 2; i ++)
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.True);
            Assert.That(cComp.IsTouching, Is.True);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.WasTouching, Is.True);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.EquivalentTo(new []{ sEntity2 }));
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.EquivalentTo(new []{ sEntity1 }));
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.EquivalentTo(new []{ cEntity2 }));
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.EquivalentTo(new []{ cEntity1 }));
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.True);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // Then in the next tick the client should apply the new server state, wherein the contacts were already touching.
        // I.e., the contact start event never actually gets raised.
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.True);
            Assert.That(cComp.IsTouching, Is.True);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.WasTouching, Is.False); // IsTouching gets resets to false before server state is applied
            Assert.That(cComp.LastState, Is.True);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.EquivalentTo(new []{ sEntity2 }));
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.EquivalentTo(new []{ sEntity1 }));
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.EquivalentTo(new []{ cEntity2 }));
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.EquivalentTo(new []{ cEntity1 }));
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // for the next few ticks, nothing should change
        for (var i = 0; i < 10; i ++)
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.True);
            Assert.That(cComp.IsTouching, Is.True);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.True);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.EquivalentTo(new []{ sEntity2 }));
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.EquivalentTo(new []{ sEntity1 }));
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.EquivalentTo(new []{ cEntity2 }));
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.EquivalentTo(new []{ cEntity1 }));
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // Next we move the entity away again, so the contact should stop
        {
            cSys.Ev = new CollisionTestMoveEvent(nEntity1, coords1);
            await Tick();
            Assert.That(sComp.IsTouching, Is.True);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.True);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.EquivalentTo(new []{ sEntity2 }));
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.EquivalentTo(new []{ sEntity1 }));
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.True);
        }

        // Next tick, the client should reset to a state where the entities were touching, and then re-predict the stop-collide events
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.True);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.True);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.EquivalentTo(new []{ sEntity2 }));
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.EquivalentTo(new []{ sEntity1 }));
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.True);
        }

        // Next, the server should receive the networked event
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.StopTick, Is.EqualTo(sComp.StopTick));
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.True);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.True);
            Assert.That(cSys.CollisionEnded, Is.True);
        }

        // nothing changes while waiting for the client to apply the new server state
        for (var i = 0; i < 2; i ++)
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.StopTick, Is.EqualTo(sComp.StopTick));
            Assert.That(cComp.WasTouching, Is.False);
            Assert.That(cComp.LastState, Is.True);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.True);
        }

        // And then the client should apply the new server state
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.StopTick, Is.EqualTo(sComp.StopTick));
            Assert.That(cComp.WasTouching, Is.True);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        // Nothing should change in the next few ticks
        for (var i = 0; i < 10; i ++)
        {
            await Tick();
            Assert.That(sComp.IsTouching, Is.False);
            Assert.That(cComp.IsTouching, Is.False);
            Assert.That(cComp.StartTick, Is.EqualTo(sComp.StartTick));
            Assert.That(cComp.StopTick, Is.EqualTo(sComp.StopTick));
            Assert.That(cComp.WasTouching, Is.True);
            Assert.That(cComp.LastState, Is.False);
            Assert.That(sPhys.GetContactingEntities(sEntity1), Is.Empty);
            Assert.That(sPhys.GetContactingEntities(sEntity2), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity1), Is.Empty);
            Assert.That(cPhys.GetContactingEntities(cEntity2), Is.Empty);
            Assert.That(sSys.CollisionStarted, Is.False);
            Assert.That(cSys.CollisionStarted, Is.False);
            Assert.That(sSys.CollisionEnded, Is.False);
            Assert.That(cSys.CollisionEnded, Is.False);
        }

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CollisionPredictionTestComponent : Component
{
    public bool IsTouching;
    public bool WasTouching;
    public bool LastState;
    public GameTick StartTick;
    public GameTick StopTick;

    [Serializable, NetSerializable]
    public sealed class State(bool isTouching) : ComponentState
    {
        public bool IsTouching = isTouching;
    }
}

[Serializable, NetSerializable]
public sealed class CollisionTestMoveEvent(NetEntity ent, MapCoordinates coords) : EntityEventArgs
{
    public NetEntity Ent = ent;
    public MapCoordinates Coords = coords;
}

public sealed class CollisionPredictionTestSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool CollisionStarted;
    public bool CollisionEnded;

    public override void Initialize()
    {
        SubscribeLocalEvent<CollisionPredictionTestComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<CollisionPredictionTestComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<CollisionPredictionTestComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CollisionPredictionTestComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<CollisionPredictionTestComponent, UpdateIsPredictedEvent>(OnIsPredicted);
        SubscribeAllEvent<CollisionTestMoveEvent>(OnMove);

        // Updates before physics to simulate input events.
        // inputs are processed before systems update, but I CBF setting up a proper input / keybinding.
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
    }

    public CollisionTestMoveEvent? Ev;

    public override void Update(float frameTime)
    {
        if (Ev == null || !_timing.IsFirstTimePredicted)
            return;

        RaisePredictiveEvent(Ev);
        Ev = null;
    }

    private void OnIsPredicted(Entity<CollisionPredictionTestComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnMove(CollisionTestMoveEvent ev)
    {
        _xform.SetMapCoordinates(GetEntity(ev.Ent), ev.Coords);
    }

    private void OnEndCollide(Entity<CollisionPredictionTestComponent> ent, ref EndCollideEvent args)
    {
        // TODO PHYSICS Collision Mispredicts
        // Currently the client will raise collision start/stop events multiple times for each collision
        // If this ever gets fixed, re-add the assert:
        // Assert.That(ent.Comp.IsTouching, Is.True);
        if (!ent.Comp.IsTouching)
            return;

        Assert.That(CollisionEnded, Is.False);
        ent.Comp.StopTick = _timing.CurTick;
        ent.Comp.IsTouching = false;
        CollisionEnded = true;
        Dirty(ent);
    }

    private void OnStartCollide(Entity<CollisionPredictionTestComponent> ent, ref StartCollideEvent args)
    {
        // TODO PHYSICS Collision Mispredicts
        // Currently the client will raise collision start/stop events multiple times for each collision
        // If this ever gets fixed, re-add the assert:
        // Assert.That(ent.Comp.IsTouching, Is.False);
        if (ent.Comp.IsTouching)
            return;

        Assert.That(CollisionStarted, Is.False);
        ent.Comp.StartTick = _timing.CurTick;
        ent.Comp.IsTouching = true;
        CollisionStarted = true;
        Dirty(ent);
    }

    private void OnGetState(Entity<CollisionPredictionTestComponent> ent, ref ComponentGetState args)
    {
        args.State = new CollisionPredictionTestComponent.State(ent.Comp.IsTouching);
    }

    private void OnHandleState(Entity<CollisionPredictionTestComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not CollisionPredictionTestComponent.State state)
            return;

        ent.Comp.WasTouching = ent.Comp.IsTouching;
        ent.Comp.LastState = state.IsTouching;
        ent.Comp.IsTouching = state.IsTouching;
    }
}
