using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Shared.Map;

public sealed class GridDeleteSingleTileRemoveTestTest : RobustIntegrationTest
{
    /// <summary>
    /// Spawns a simple 1-tile grid with an entity on it, and then sets the tile to "space".
    /// This should delete the grid without deleting the entity.
    /// This also checks the networking to players, as previously this caused clients to crash.
    /// </summary>
    [Test]
    public async Task TestRemoveSingleTile()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();

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

        // Set up map, grid, entity, and player
        Entity<MapGridComponent> grid = default;
        EntityUid sEntity = default;
        EntityUid sMap = default;
        EntityUid sPlayer = default;
        var sys = sEntMan.System<SharedMapSystem>();
        await server.WaitPost(() =>
        {
            sMap = sys.CreateMap(out var mapId);
            var comp = mapMan.CreateGridEntity(mapId);
            grid = (comp.Owner, comp);
            sys.SetTile(grid, grid, new Vector2i(0, 0), new Tile(typeId: 1, flags: 1, variant: 1));
            var coords = new EntityCoordinates(grid, 0.5f, 0.5f);

            sPlayer = sEntMan.SpawnEntity(null, coords);
            sEntity = sEntMan.SpawnEntity(null, coords);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, sPlayer);
            sPlayerMan.JoinGame(session);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var nEntity = sEntMan.GetNetEntity(sEntity);
        var nPlayer = sEntMan.GetNetEntity(sPlayer);
        var nGrid = sEntMan.GetNetEntity(grid);
        var nMap = sEntMan.GetNetEntity(sMap);

        // Check player got properly attached, and has received the other entity.
        Assert.That(cEntMan.TryGetEntity(nEntity, out var cEntity));
        Assert.That(cEntMan.TryGetEntity(nPlayer, out var cPlayerUid));
        Assert.That(cEntMan.TryGetEntity(nGrid, out var cGrid));
        Assert.That(cEntMan.TryGetEntity(nMap, out var cMap));
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(cPlayerUid));

        var sQuery = sEntMan.GetEntityQuery<TransformComponent>();
        Assert.That(sQuery.GetComponent(sEntity).ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(sQuery.GetComponent(grid.Owner).ParentUid, Is.EqualTo(sMap));

        var cQuery = cEntMan.GetEntityQuery<TransformComponent>();
        Assert.That(cQuery.GetComponent(cEntity!.Value).ParentUid, Is.EqualTo(cGrid));
        Assert.That(cQuery.GetComponent(cGrid!.Value).ParentUid, Is.EqualTo(cMap));

        // Remove the tile.
        await server.WaitPost(() =>
        {
            sys.SetTile(grid, grid, new Vector2i(0, 0), Tile.Empty);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Grid should no longer exist.
        Assert.That(!sEntMan.EntityExists(grid));
        Assert.That(!cEntMan.EntityExists(cGrid));

        // Entity should now be parented to the map
        Assert.That(sQuery.GetComponent(sEntity).ParentUid, Is.EqualTo(sMap));
        Assert.That(cQuery.GetComponent(cEntity.Value).ParentUid, Is.EqualTo(cMap));

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}
