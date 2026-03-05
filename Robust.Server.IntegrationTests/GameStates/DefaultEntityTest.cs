using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class DefaultEntityTest : RobustIntegrationTest
{
    /// <summary>
    /// Simple test that just spawns a default entity without any components or modifications and checks that the
    /// client receives the entity.
    /// </summary>
    [Test]
    public async Task TestSpawnDefaultEntity()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();

        client.SetConnectTarget(server);
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, false));

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var session = playerMan.Sessions.First();
        await server.WaitPost(() => playerMan.JoinGame(session));

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Spawn a default unmodified entity.
        NetEntity ent = default;
        await server.WaitPost(() =>
        {
            ent = sEntMan.GetNetEntity(sEntMan.Spawn());
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check that server & client both think the entity exists.
        Assert.That(sEntMan.EntityExists(sEntMan.GetEntity(ent)));
        Assert.That(cEntMan.EntityExists(cEntMan.GetEntity(ent)));

        // Enable PVS and repeat the test.
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        // Set up map and spawn player entity
        NetEntity player = default;
        EntityCoordinates coords = default!;
        await server.WaitPost(() =>
        {
            var map = server.System<SharedMapSystem>().CreateMap();
            coords = new(map, default);
            var playerUid = sEntMan.SpawnEntity(null, coords);
            player = sEntMan.GetNetEntity(playerUid);
            server.PlayerMan.SetAttachedEntity(session, playerUid);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        Assert.That(sEntMan.EntityExists(sEntMan.GetEntity(player)));
        Assert.That(cEntMan.EntityExists(cEntMan.GetEntity(player)));

        await server.WaitPost(() =>
        {
            ent = sEntMan.GetNetEntity(sEntMan.SpawnAtPosition(null, coords));
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        Assert.That(sEntMan.EntityExists(sEntMan.GetEntity(ent)));
        Assert.That(cEntMan.EntityExists(cEntMan.GetEntity(ent)));

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

