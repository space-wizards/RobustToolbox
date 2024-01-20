using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class PvsChunkTest : RobustIntegrationTest
{
    [Test]
    public async Task TestGridMapChange()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var mapSys = sEntMan.System<MapSystem>();

        var cEntMan = client.ResolveDependency<IEntityManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();

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

        // Set up entities
        EntityUid map1 = default;
        EntityUid map2 = default;
        EntityUid grid = default;
        EntityUid player = default;
        EntityUid entity = default;
        EntityCoordinates mapCoords = default;
        await server.WaitPost(() =>
        {
            var mapId = mapMan.CreateMap();
            map1 = mapMan.GetMapEntityId(mapId);
            mapCoords = new(map1, default);

            var map2Id = mapMan.CreateMap();
            map2 = mapMan.GetMapEntityId(map2Id);

            var gridComp = mapMan.CreateGridEntity(map2Id);
            grid = gridComp.Owner;
            mapSys.SetTile(grid, gridComp, Vector2i.Zero, new Tile(1));
            var gridCoords = new EntityCoordinates(grid, .5f, .5f);

            player = sEntMan.SpawnEntity(null, mapCoords);
            entity = sEntMan.SpawnEntity(null, gridCoords);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var nEntity = sEntMan.GetNetEntity(entity);
        var nGrid = sEntMan.GetNetEntity(grid);
        var nMap1 = sEntMan.GetNetEntity(map1);
        var nMap2 = sEntMan.GetNetEntity(map2);

        var xform = sEntMan.GetComponent<TransformComponent>(entity);
        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map2));

        Assert.That(!cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));

        // Teleport grid to new map
        await server.WaitPost(() => xforms.SetCoordinates(grid, mapCoords));
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map1));

        Assert.That(cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));

        // Delete the original map.
        await server.WaitPost(() => sEntMan.DeleteEntity(map2));
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map1));

        Assert.That(cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(!cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));
    }
}

