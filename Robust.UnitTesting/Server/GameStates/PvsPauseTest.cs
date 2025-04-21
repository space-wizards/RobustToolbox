using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class PvsPauseTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that the client "pauses" entities that have left their PVS range.
    /// </summary>
    [Test]
    public async Task PauseTest()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sEntMan = server.EntMan;
        var confMan = server.CfgMan;
        var sPlayerMan = server.PlayerMan;
        var xforms = sEntMan.System<SharedTransformSystem>();
        var metaSys = sEntMan.System<MetaDataSystem>();
        var stateMan = (ClientGameStateManager) client.ResolveDependency<IClientGameStateManager>();

        var cEntMan = client.EntMan;
        var cPlayerMan = client.PlayerMan;
        var netMan = client.ResolveDependency<IClientNetManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }

        await RunTicks();

        // Set up map and spawn player
        EntityUid map = default;
        EntityUid playerUid = default;
        EntityUid sEnt = default;
        EntityCoordinates coords = default;
        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap();
            coords = new(map, default);

            playerUid = sEntMan.SpawnEntity(null, coords);
            sEnt = sEntMan.SpawnEntity(null, coords);
            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);
        });

        await RunTicks();
        var farAway = new EntityCoordinates(map, new Vector2(100, 100));
        var netEnt = sEntMan.GetNetEntity(sEnt);
        var player = sEntMan.GetNetEntity(playerUid);
        Assert.That(player, Is.Not.EqualTo(NetEntity.Invalid));
        Assert.That(netEnt, Is.Not.EqualTo(NetEntity.Invalid));

        // Check player got properly attached, and has received the other entity.
        Assert.That(cEntMan.TryGetEntity(netEnt, out var uid));
        Assert.That(cEntMan.TryGetEntity(player, out var cPlayerUid));
        var cEnt = uid!.Value;
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(cPlayerUid));

        void AssertEnt(bool paused, bool detached, bool clientPaused)
        {
            var cMeta = client.MetaData(cEnt);
            var sMeta = server.MetaData(sEnt);

            Assert.That(cMeta.Flags.HasFlag(MetaDataFlags.Detached), Is.EqualTo(detached));
            Assert.That(sMeta.Flags.HasFlag(MetaDataFlags.Detached), Is.False);

            Assert.That(stateMan.IsQueuedForDetach(netEnt), Is.False);
            Assert.That(sMeta.EntityPaused, Is.EqualTo(paused));
            Assert.That(cMeta.EntityPaused, Is.EqualTo(clientPaused));

            if (detached)
                Assert.That(cMeta.PauseTime, Is.EqualTo(TimeSpan.Zero));
            if (clientPaused)
                Assert.That(cMeta.PauseTime, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
            else
                Assert.That(cMeta.PauseTime, Is.Null);

            if (paused)
                Assert.That(sMeta.PauseTime, Is.GreaterThan(TimeSpan.Zero));
            else
                Assert.That(sMeta.PauseTime, Is.Null);
        }

        // Entity is initially in view and not paused.
        AssertEnt(paused: false, detached: false, clientPaused: false);

        // Move the player out of the entity's PVS range
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));
        await RunTicks();

        // Client should now have detached & locally paused the entity.
        AssertEnt(paused: false, detached: true, clientPaused: true);

        // Move the player back into range
        await server.WaitPost( () => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
        await RunTicks();
        AssertEnt(paused: false, detached: false, clientPaused: false);

        // Actually pause the entity.
        await server.WaitPost(() => metaSys.SetEntityPaused(sEnt, true));
        await RunTicks();
        AssertEnt(paused: true, detached: false, clientPaused: true);

        // Out of range
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));
        await RunTicks();
        AssertEnt(paused: true, detached: true, clientPaused: true);

        // Back in range
        await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
        await RunTicks();
        AssertEnt(paused: true, detached: false, clientPaused: true);

         // Unpause the entity while out of range
         {
             await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));
             await RunTicks();
             AssertEnt(paused: true, detached: true, clientPaused: true);

             await server.WaitPost(() => metaSys.SetEntityPaused(sEnt, false));
             await RunTicks();
             AssertEnt(paused: false, detached: true, clientPaused: true);

             await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
             await RunTicks();
             AssertEnt(paused: false, detached: false, clientPaused: false);
         }

         // Pause the entity while out of range
         {
             await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), farAway));
             await RunTicks();
             AssertEnt(paused: false, detached: true, clientPaused: true);

             await server.WaitPost(() => metaSys.SetEntityPaused(sEnt, true));
             await RunTicks();
             AssertEnt(paused: true, detached: true, clientPaused: true);

             await server.WaitPost(() => xforms.SetCoordinates(sEntMan.GetEntity(player), coords));
             await RunTicks();
             AssertEnt(paused: true, detached: false, clientPaused: true);
         }
    }
}

