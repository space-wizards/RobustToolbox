using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class PvsReEntryTest : RobustIntegrationTest
{
#if DEBUG
    /// <summary>
    /// Checks that there are no issues when an entity enters, leaves, then enters pvs while the client drops packets.
    /// </summary>
    [Test]
    public async Task TestLossyReEntry()
    {
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var stateMan = (ClientGameStateManager) client.ResolveDependency<IClientGameStateManager>();

        var cEntMan = client.ResolveDependency<IEntityManager>();
        var cPlayerMan = client.ResolveDependency<ISharedPlayerManager>();

        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        await RunTicksSync(server, client, 10);

        // Ensure client & server ticks are synced.
        // Client runs 1 tick ahead
        {
            var sTick = (int)server.Timing.CurTick.Value;
            var cTick = (int)client.Timing.CurTick.Value;
            var delta = cTick - sTick;

            if (delta > 1)
                await server.WaitRunTicks(delta - 1);
            else if (delta < 1)
                await client.WaitRunTicks(1 - delta);

            sTick = (int)server.Timing.CurTick.Value;
            cTick = (int)client.Timing.CurTick.Value;
            delta = cTick - sTick;
            Assert.That(delta, Is.EqualTo(1));
        }

        // Set up map and spawn player
        EntityUid map = default;
        NetEntity player = default;
        NetEntity entity = default;
        EntityCoordinates coords = default;
        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap();
            coords = new(map, default);

            var playerUid = sEntMan.SpawnEntity(null, coords);
            var entUid = sEntMan.SpawnEntity(null, coords);
            entity = sEntMan.GetNetEntity(entUid);
            player = sEntMan.GetNetEntity(playerUid);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 10);

        Assert.That(player, Is.Not.EqualTo(NetEntity.Invalid));
        Assert.That(entity, Is.Not.EqualTo(NetEntity.Invalid));

        // Check player got properly attached, and has received the other entity.
        MetaDataComponent? meta = default!;
        await client.WaitPost(() =>
        {
            Assert.That(cEntMan.TryGetEntityData(entity, out _, out meta));
            Assert.That(cEntMan.TryGetEntity(player, out var cPlayerUid));
            Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(cPlayerUid));
            Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);
        });

        var lastDirty = meta.LastModifiedTick;
        Assert.That(lastDirty, Is.GreaterThan(GameTick.Zero));

        // Move the player outside of the entity's PVS range
        // note that we move the PLAYER not the entity, as we don't want to dirty the entity.
        var farAway = new EntityCoordinates(map, new Vector2(100, 100));
        await server.WaitPost( () => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));
        await RunTicksSync(server, client, 10);

        // Client should have detached the entity to null space.
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);

        // Move the player back into range
        await server.WaitPost( () => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
        await RunTicksSync(server, client, 10);

        // Entity is back in pvs range
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);

        // Oh no, the client is going through a tunnel!
        stateMan.DropStates = true;
        var timing = client.ResolveDependency<IClientGameTiming>();
        var lastRealTick = timing.LastRealTick;

        await RunTicksSync(server, client, 5);

        // Even though the client is receiving no new states, it will still have applied some from the state buffer.
        Assert.That(timing.LastRealTick, Is.GreaterThan(lastRealTick));
        lastRealTick = timing.LastRealTick;

        // Move the player outside of pvs range.
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));

        await RunTicksSync(server, client, 5);

        // Client should have exhausted the buffer -- client has not been applying any states.
        Assert.That(timing.LastRealTick, Is.EqualTo(lastRealTick));
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        // However pvs-leave messages are sent separately, and are sent reliably. So they are still being received.
        // Though in a realistic scenario they should probably be delayed somewhat.
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.True);

        // Move the entity back into range
        await server.WaitPost( () => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));

        await RunTicksSync(server, client, 5);

        // Still hasn't been applying states.
        Assert.That(timing.LastRealTick, Is.EqualTo(lastRealTick));
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        // The pvs-leave message has not yet been processed, detaching is still queued.
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.True);

        // Client clears the tunnel, starts receiving states again.
        stateMan.DropStates = false;

        await RunTicksSync(server, client, 10);

        // Entity should be in PVS range, client should know about it:
        Assert.That(timing.LastRealTick, Is.GreaterThan(lastRealTick));
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);

        // The entity itself should not have been dirtied since creation -- only the player has been moving.
        // If the test moves the entity instead of the player, then the test doesn't actually work.
        Assert.That(meta.LastModifiedTick, Is.EqualTo(lastDirty));

    }
#endif

    /// <summary>
    /// Checks that PVS re-entry only reapplies component states for components dirtied while the entity was out of PVS.
    /// </summary>
    [Test]
    public async Task TestReEntryOnlyReplaysDirtyComponents()
    {
        await using var pair = await StartConnectedPair(
            new ServerIntegrationOptions { Pool = false },
            new ClientIntegrationOptions { Pool = false });

        var (client, server) = pair;

        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();

        var cEntMan = client.ResolveDependency<IEntityManager>();
        PvsReEntryReplayTestSystem? cSystem = null;

        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        await RunTicksSync(server, client, 10);

        EntityUid map = default;
        NetEntity entity = default;
        NetEntity dirtyEntity = default;
        NetEntity player = default;
        EntityCoordinates coords = default;

        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap();
            coords = new EntityCoordinates(map, default);

            var playerUid = sEntMan.SpawnEntity(null, coords);
            var entUid = sEntMan.SpawnEntity(null, coords);
            var comp = sEntMan.EnsureComponent<PvsReEntryReplayTestComponent>(entUid);
            comp.Value = 1;
            sEntMan.Dirty(entUid, comp);

            var dirtyEntUid = sEntMan.SpawnEntity(null, coords);
            comp = sEntMan.EnsureComponent<PvsReEntryReplayTestComponent>(dirtyEntUid);
            comp.Value = 1;
            sEntMan.Dirty(dirtyEntUid, comp);

            entity = sEntMan.GetNetEntity(entUid);
            dirtyEntity = sEntMan.GetNetEntity(dirtyEntUid);
            player = sEntMan.GetNetEntity(playerUid);

            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 10);

        MetaDataComponent? meta = null;
        MetaDataComponent? dirtyMeta = null;
        var initialHandleCount = 0;
        await client.WaitPost(() =>
        {
            cSystem = client.System<PvsReEntryReplayTestSystem>();
            Assert.That(cEntMan.TryGetEntityData(entity, out _, out meta), Is.True);
            Assert.That(cEntMan.TryGetEntityData(dirtyEntity, out _, out dirtyMeta), Is.True);
            Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(dirtyMeta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            initialHandleCount = cSystem.HandleStateCount;
            Assert.That(initialHandleCount, Is.GreaterThanOrEqualTo(2));
        });

        var farAway = new EntityCoordinates(map, new Vector2(100, 100));
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));
        await RunTicksSync(server, client, 10);

        await client.WaitPost(() =>
        {
            Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
            Assert.That(dirtyMeta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
            Assert.That(cSystem!.HandleStateCount, Is.EqualTo(initialHandleCount));
        });

        await server.WaitPost(() =>
        {
            var comp = sEntMan.GetComponent<PvsReEntryReplayTestComponent>(sEntMan.GetEntity(dirtyEntity));
            comp.Value = 2;
            sEntMan.Dirty(sEntMan.GetEntity(dirtyEntity), comp);
        });

        await RunTicksSync(server, client, 5);

        await client.WaitPost(() =>
        {
            Assert.That(cSystem!.HandleStateCount, Is.EqualTo(initialHandleCount));
        });

        // Re-enter after dirtying one of the test components. Only the dirtied component should get replayed.
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
        await RunTicksSync(server, client, 10);

        await client.WaitPost(() =>
        {
            var uid = cEntMan.GetEntity(entity);
            var dirtyUid = cEntMan.GetEntity(dirtyEntity);
            Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(dirtyMeta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(cSystem!.HandleStateCount, Is.EqualTo(initialHandleCount + 1));
            Assert.That(cEntMan.GetComponent<PvsReEntryReplayTestComponent>(uid).Value, Is.EqualTo(1));
            Assert.That(cEntMan.GetComponent<PvsReEntryReplayTestComponent>(dirtyUid).Value, Is.EqualTo(2));
        });
    }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class PvsReEntryReplayTestComponent : Component
{
    public int Value;
}

public sealed partial class PvsReEntryReplayTestSystem : EntitySystem
{
    public int HandleStateCount;

    public override void Initialize()
    {
        SubscribeLocalEvent<PvsReEntryReplayTestComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PvsReEntryReplayTestComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(Entity<PvsReEntryReplayTestComponent> ent, ref ComponentGetState args)
    {
        args.State = new MetaDataComponentState(null, ent.Comp.Value.ToString(), null, null);
    }

    private void OnHandleState(Entity<PvsReEntryReplayTestComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not MetaDataComponentState state)
            return;

        ent.Comp.Value = int.Parse(state.Description!);
        HandleStateCount++;
    }
}

