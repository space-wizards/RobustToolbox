using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameStates;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class PvsChunkTest : RobustIntegrationTest
{
    [Test]
    public async Task TestForceSentGridIgnoresRange()
    {
        // TODO: Update to new test infra
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var mapSys = sEntMan.System<MapSystem>();
        var pvsOverride = sEntMan.System<PvsOverrideSystem>();

        var cEntMan = client.ResolveDependency<IEntityManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetMaxUpdateRange, 32f);
            confMan.SetCVar(CVars.NetPvsGridRange, 32f);
        });

        async Task RunTicks()
        {
            for (var i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }

        await RunTicks();

        // Create a grid outside normal grid PVS range and attach the player near the map origin.
        EntityUid farGrid = default;
        EntityUid player = default;
        await server.WaitPost(() =>
        {
            var map = server.System<SharedMapSystem>().CreateMap(out var mapId);

            var farGridComp = mapMan.CreateGridEntity(mapId);
            farGrid = farGridComp.Owner;
            mapSys.SetTile(farGridComp, Vector2i.Zero, new Tile(1));
            xforms.SetLocalPosition(farGrid, new Vector2(1024f, 0f));

            player = sEntMan.SpawnEntity(null, new EntityCoordinates(map, new Vector2(0.5f, 0.5f)));

            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicks();

        var farNetGrid = sEntMan.GetNetEntity(farGrid);
        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.False);

        // Forced PVS entities should bypass grid range checks.
        await server.WaitPost(() => pvsOverride.AddForceSend(farGrid));
        await RunTicks();

        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.True);

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }

    [Test]
    public async Task TestGridRangeCulling()
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
        var cMapMan = client.ResolveDependency<IMapManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetMaxUpdateRange, 32f);
            confMan.SetCVar(CVars.NetPvsGridRange, 32f);
        });

        async Task RunTicks()
        {
            for (var i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }

        await RunTicks();

        // Create one grid in range and one grid far outside the configured grid PVS range.
        EntityUid nearGrid = default;
        EntityUid farGrid = default;
        EntityUid map = default;
        EntityUid player = default;
        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap(out var mapId);

            var nearGridComp = mapMan.CreateGridEntity(mapId);
            nearGrid = nearGridComp.Owner;
            mapSys.SetTile(nearGridComp, Vector2i.Zero, new Tile(1));

            var farGridComp = mapMan.CreateGridEntity(mapId);
            farGrid = farGridComp.Owner;
            mapSys.SetTile(farGridComp, Vector2i.Zero, new Tile(1));
            xforms.SetLocalPosition(farGrid, new Vector2(1024f, 0f));

            player = sEntMan.SpawnEntity(null, new EntityCoordinates(map, new Vector2(0.5f, 0.5f)));

            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicks();

        var nearNetGrid = sEntMan.GetNetEntity(nearGrid);
        var farNetGrid = sEntMan.GetNetEntity(farGrid);
        var netMap = sEntMan.GetNetEntity(map);

        Assert.That(cEntMan.TryGetEntity(netMap, out var cMap), Is.True);
        Assert.That(cEntMan.TryGetEntity(nearNetGrid, out var cNearGrid), Is.True);
        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.False);

        Assert.That(
            cMapMan.TryFindGridAt(cMap!.Value, new Vector2(0.5f, 0.5f), out var foundGrid, out MapGridComponent? _),
            Is.True);
        Assert.That(foundGrid, Is.EqualTo(cNearGrid!.Value));

        // Move the visible grid out of range, then return the player to its old position.
        // The client should not keep seeing the detached grid at its old location.
        await server.WaitPost(() =>
        {
            xforms.SetCoordinates(player, new EntityCoordinates(map, new Vector2(8f, 0.5f)));
            xforms.SetLocalPosition(nearGrid, new Vector2(2048f, 0f));
        });
        await RunTicks();

        await server.WaitPost(() => xforms.SetCoordinates(player, new EntityCoordinates(map, new Vector2(0.5f, 0.5f))));
        await RunTicks();

        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.False);
        Assert.That(
            cMapMan.TryFindGridAt(cMap.Value, new Vector2(0.5f, 0.5f), out _, out MapGridComponent? _),
            Is.False);

        Assert.That(cEntMan.TryGetEntity(nearNetGrid, out cNearGrid), Is.True);
        var cNearMeta = cEntMan.GetComponent<MetaDataComponent>(cNearGrid!.Value);
        var cNearXform = cEntMan.GetComponent<TransformComponent>(cNearGrid.Value);
        Assert.That(cNearMeta.Flags.HasFlag(MetaDataFlags.Detached), Is.True);
        Assert.That(cNearXform.ParentUid, Is.EqualTo(EntityUid.Invalid));

        // Moving into the grid's new location should reattach it and make lookup find it there.
        await server.WaitPost(() => xforms.SetCoordinates(player, new EntityCoordinates(map, new Vector2(2048.5f, 0.5f))));
        await RunTicks();

        Assert.That(cEntMan.TryGetEntity(nearNetGrid, out cNearGrid), Is.True);
        cNearMeta = cEntMan.GetComponent<MetaDataComponent>(cNearGrid!.Value);
        cNearXform = cEntMan.GetComponent<TransformComponent>(cNearGrid.Value);
        Assert.That(cNearMeta.Flags.HasFlag(MetaDataFlags.Detached), Is.False);
        Assert.That(cNearXform.ParentUid, Is.EqualTo(cMap.Value));

        Assert.That(
            cMapMan.TryFindGridAt(cMap.Value, new Vector2(2048.5f, 0.5f), out foundGrid, out MapGridComponent? _),
            Is.True);
        Assert.That(foundGrid, Is.EqualTo(cNearGrid.Value));

        await server.WaitPost(() => sEntMan.DeleteEntity(nearGrid));
        await RunTicks();

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }

    [Test]
    public async Task TestGridMapChange()
    {
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var mapSys = sEntMan.System<MapSystem>();

        var cEntMan = client.ResolveDependency<IEntityManager>();

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

        // Set up entities
        EntityUid map1 = default;
        EntityUid map2 = default;
        EntityUid grid = default;
        EntityUid player = default;
        EntityUid entity = default;
        EntityCoordinates mapCoords = default;
        await server.WaitPost(() =>
        {
            map1 = server.System<SharedMapSystem>().CreateMap();
            mapCoords = new(map1, default);

            map2 = server.System<SharedMapSystem>().CreateMap();
            var gridComp = mapMan.CreateGridEntity(map2);
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

        await RunTicksSync(server, client, 10);

        var nEntity = sEntMan.GetNetEntity(entity);
        var nGrid = sEntMan.GetNetEntity(grid);
        var nMap1 = sEntMan.GetNetEntity(map1);
        var nMap2 = sEntMan.GetNetEntity(map2);

        var xform = sEntMan.GetComponent<TransformComponent>(entity);
        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map2));

        // The second map and its grid are out of range, so only the player's current map should be known.
        Assert.That(!cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(!cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(!cEntMan.TryGetEntity(nGrid, out _));

        // Teleport the grid to the player's map. Its contents should enter PVS.
        await server.WaitPost(() => xforms.SetCoordinates(grid, mapCoords));
        await RunTicksSync(server, client, 10);

        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map1));

        Assert.That(cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(!cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));

        // Delete the original map.
        await server.WaitPost(() => sEntMan.DeleteEntity(map2));
        await RunTicksSync(server, client, 10);

        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map1));

        Assert.That(cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(!cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));

    }
}
