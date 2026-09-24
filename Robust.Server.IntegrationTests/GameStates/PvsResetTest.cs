using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class PvsResetTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that the client doesn't reset dirty detached entities. They should remain in nullspace.
    /// </summary>
    [Test]
    public async Task ResetTest()
    {
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var sEntMan = server.EntMan;
        var confMan = server.CfgMan;
        var sPlayerMan = server.PlayerMan;
        var xforms = sEntMan.System<SharedTransformSystem>();

        var cEntMan = client.EntMan;
        var cPlayerMan = client.PlayerMan;

        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        await RunTicksSync(server, client, 10);

        // Set up map and spawn player
        EntityUid sMap = default;
        EntityUid playerUid = default;
        EntityUid sEnt = default;
        EntityCoordinates coords = default;
        await server.WaitPost(() =>
        {
            sMap = server.System<SharedMapSystem>().CreateMap();
            coords = new(sMap, default);

            playerUid = sEntMan.SpawnEntity(null, coords);
            sEnt = sEntMan.SpawnEntity(null, coords);
            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 10);
        var farAway = new EntityCoordinates(sMap, new Vector2(100, 100));
        var netEnt = sEntMan.GetNetEntity(sEnt);
        var player = sEntMan.GetNetEntity(playerUid);
        Assert.That(player, Is.Not.EqualTo(NetEntity.Invalid));
        Assert.That(netEnt, Is.Not.EqualTo(NetEntity.Invalid));

        // Check player got properly attached, and has received the other entity.
        Assert.That(cEntMan.TryGetEntity(netEnt, out var uid));
        Assert.That(cEntMan.TryGetEntity(player, out var cPlayerUid));
        var cEnt = uid!.Value;
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(cPlayerUid));
        var cMap = cEntMan.GetEntity(sEntMan.GetNetEntity(sMap));

        void AssertDetached(bool detached)
        {
            var cXform = client.Transform(cEnt);
            var sXform = server.Transform(sEnt);
            var meta = client.MetaData(cEnt);

            Assert.That(sXform.MapUid, Is.EqualTo(sMap));
            Assert.That(sXform.ParentUid, Is.EqualTo(sMap));

            if (detached)
            {
                Assert.That(meta.Flags.HasFlag(MetaDataFlags.Detached));
                Assert.That(cXform.MapUid, Is.Null);
                Assert.That(cXform.ParentUid, Is.EqualTo(EntityUid.Invalid));
            }
            else
            {
                Assert.That(!meta.Flags.HasFlag(MetaDataFlags.Detached));
                Assert.That(cXform.MapUid, Is.EqualTo(cMap));
                Assert.That(cXform.ParentUid, Is.EqualTo(cMap));
            }
        }

        // Entity is initially in view
        AssertDetached(false);

        // Move the player out of the entity's PVS range
        await server.WaitPost(() => xforms.SetCoordinates(playerUid, farAway));
        await RunTicksSync(server, client, 10);

        // Client should now have detached the entity, moving it into nullspace
        AssertDetached(true);

        // Marking the entity as dirty due to client-side prediction should have effect
        await client.WaitPost(() => client.EntMan.Dirty(cEnt, client.Transform(cEnt)));
        await RunTicksSync(server, client, 10);
        AssertDetached(true);

        // Move the player back into range
        await server.WaitPost( () => xforms.SetCoordinates(playerUid, coords));
        await RunTicksSync(server, client, 10);
        AssertDetached(false);

        // Marking the entity as dirty due to client-side prediction should have no real effect
        await client.WaitPost(() => client.EntMan.Dirty(cEnt, client.Transform(cEnt)));
        await RunTicksSync(server, client, 10);
        AssertDetached(false);

    }
}

