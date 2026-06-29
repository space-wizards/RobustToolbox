using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

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
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();

        server.Post(() => confMan.SetCVar(CVars.NetPVS, false));

        await RunTicksSync(server, client, 10);

        var session = playerMan.Sessions.First();
        await server.WaitPost(() => playerMan.JoinGame(session));

        await RunTicksSync(server, client, 10);

        // Spawn a default unmodified entity.
        NetEntity ent = default;
        await server.WaitPost(() =>
        {
            ent = sEntMan.GetNetEntity(sEntMan.Spawn());
        });

        await RunTicksSync(server, client, 10);

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

        await RunTicksSync(server, client, 10);

        Assert.That(sEntMan.EntityExists(sEntMan.GetEntity(player)));
        Assert.That(cEntMan.EntityExists(cEntMan.GetEntity(player)));

        await server.WaitPost(() =>
        {
            ent = sEntMan.GetNetEntity(sEntMan.SpawnAtPosition(null, coords));
        });

        await RunTicksSync(server, client, 10);

        Assert.That(sEntMan.EntityExists(sEntMan.GetEntity(ent)));
        Assert.That(cEntMan.EntityExists(cEntMan.GetEntity(ent)));

    }
}

