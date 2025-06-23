using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
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
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var stateMan = (ClientGameStateManager) client.ResolveDependency<IClientGameStateManager>();

        var cEntMan = client.ResolveDependency<IEntityManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();
        var cPlayerMan = client.ResolveDependency<ISharedPlayerManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

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

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

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
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Client should have detached the entity to null space.
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);

        // Move the player back into range
        await server.WaitPost( () => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Entity is back in pvs range
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);

        // Oh no, the client is going through a tunnel!
        stateMan.DropStates = true;
        var timing = client.ResolveDependency<IClientGameTiming>();
        var lastRealTick = timing.LastRealTick;

        for (int i = 0; i < 5; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Even though the client is receiving no new states, it will still have applied some from the state buffer.
        Assert.That(timing.LastRealTick, Is.GreaterThan(lastRealTick));
        lastRealTick = timing.LastRealTick;

        // Move the player outside of pvs range.
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));

        for (int i = 0; i < 5; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Client should have exhausted the buffer -- client has not been applying any states.
        Assert.That(timing.LastRealTick, Is.EqualTo(lastRealTick));
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        // However pvs-leave messages are sent separately, and are sent reliably. So they are still being received.
        // Though in a realistic scenario they should probably be delayed somewhat.
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.True);

        // Move the entity back into range
        await server.WaitPost( () => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));

        for (int i = 0; i < 5; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Still hasn't been applying states.
        Assert.That(timing.LastRealTick, Is.EqualTo(lastRealTick));
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        // The pvs-leave message has not yet been processed, detaching is still queued.
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.True);

        // Client clears the tunnel, starts receiving states again.
        stateMan.DropStates = false;

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Entity should be in PVS range, client should know about it:
        Assert.That(timing.LastRealTick, Is.GreaterThan(lastRealTick));
        Assert.That(meta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        Assert.That(stateMan.IsQueuedForDetach(entity), Is.False);

        // The entity itself should not have been dirtied since creation -- only the player has been moving.
        // If the test moves the entity instead of the player, then the test doesn't actually work.
        Assert.That(meta.LastModifiedTick, Is.EqualTo(lastDirty));

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
#endif
}

