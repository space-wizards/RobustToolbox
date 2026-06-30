using System.Numerics;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.UnitTesting;
using Is = Robust.UnitTesting.Is;

namespace Robust.Server.IntegrationTests.GameStates;

public sealed class MapGridCullingTest : RobustIntegrationTest
{
    [Test]
    public async Task TestForceSentGridIgnoresRange()
    {
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();
        var mapSys = sEntMan.System<MapSystem>();
        var pvsOverride = sEntMan.System<PvsOverrideSystem>();

        var cEntMan = client.ResolveDependency<IEntityManager>();

        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetMaxUpdateRange, 32f);
            confMan.SetCVar(CVars.NetPvsGridRange, 32f);
        });

        await RunTicksSync(server, client, 5);

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

        await RunTicksSync(server, client, 5);

        var farNetGrid = sEntMan.GetNetEntity(farGrid);
        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.False);

        // Forced PVS entities should bypass grid range checks.
        await server.WaitPost(() => pvsOverride.AddForceSend(farGrid));
        await RunTicksSync(server, client, 5);

        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.True);
    }

    [Test]
    public async Task TestGridRangeCulling()
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
        var cMapMan = client.ResolveDependency<IMapManager>();

        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetMaxUpdateRange, 32f);
            confMan.SetCVar(CVars.NetPvsGridRange, 32f);
        });

        await RunTicksSync(server, client, 5);

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

        await RunTicksSync(server, client, 5);

        var nearNetGrid = sEntMan.GetNetEntity(nearGrid);
        var farNetGrid = sEntMan.GetNetEntity(farGrid);
        var netMap = sEntMan.GetNetEntity(map);

        Assert.That(cEntMan.TryGetEntity(netMap, out var cMap), Is.True);
        Assert.That(cEntMan.TryGetEntity(nearNetGrid, out var cNearGrid), Is.True);
        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.False);

        Assert.That(
            cMapMan.TryFindGridAt(cMap!.Value, new Vector2(0.5f, 0.5f), out var foundGrid, out var _),
            Is.True);
        Assert.That(foundGrid, Is.EqualTo(cNearGrid!.Value));

        // Move the visible grid out of range, then return the player to its old position.
        // The client should not keep seeing the detached grid at its old location.
        await server.WaitPost(() =>
        {
            xforms.SetCoordinates(player, new EntityCoordinates(map, new Vector2(8f, 0.5f)));
            xforms.SetLocalPosition(nearGrid, new Vector2(2048f, 0f));
        });

        await RunTicksSync(server, client, 5);

        await server.WaitPost(() => xforms.SetCoordinates(player, new EntityCoordinates(map, new Vector2(0.5f, 0.5f))));

        await RunTicksSync(server, client, 5);

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
        await RunTicksSync(server, client, 5);

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
    }

    [Test]
    public async Task TestGridRangeZeroDisablesGridCulling()
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

        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetMaxUpdateRange, 32f);
            confMan.SetCVar(CVars.NetPvsGridRange, 0f);
        });

        await RunTicksSync(server, client, 5);

        EntityUid farGrid = default;
        await server.WaitPost(() =>
        {
            var map = server.System<SharedMapSystem>().CreateMap(out var mapId);

            var farGridComp = mapMan.CreateGridEntity(mapId);
            farGrid = farGridComp.Owner;
            mapSys.SetTile(farGridComp, Vector2i.Zero, new Tile(1));
            xforms.SetLocalPosition(farGrid, new Vector2(1024f, 0f));

            var player = sEntMan.SpawnEntity(null, new EntityCoordinates(map, new Vector2(0.5f, 0.5f)));
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 5);

        var farNetGrid = sEntMan.GetNetEntity(farGrid);
        Assert.That(cEntMan.TryGetEntity(farNetGrid, out _), Is.True);
    }

    [Test]
    public async Task TestGridRangeZeroSendsGridsOnNonViewerMaps()
    {
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var mapSys = sEntMan.System<MapSystem>();
        var cEntMan = client.ResolveDependency<IEntityManager>();

        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetPvsMapCulling, true);
            confMan.SetCVar(CVars.NetPvsGridRange, 0f);
        });

        await RunTicksSync(server, client, 5);

        EntityUid map1 = default;
        EntityUid map2 = default;
        EntityUid grid = default;
        await server.WaitPost(() =>
        {
            map1 = server.System<SharedMapSystem>().CreateMap();
            map2 = server.System<SharedMapSystem>().CreateMap();
            var gridComp = mapMan.CreateGridEntity(map2);
            grid = gridComp.Owner;
            mapSys.SetTile(gridComp, Vector2i.Zero, new Tile(1));

            var player = sEntMan.SpawnEntity(null, new EntityCoordinates(map1, default));
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 5);

        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(map1), out _), Is.True);
        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(map2), out _), Is.False);
        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(grid), out _), Is.True);
    }

    [Test]
    public async Task TestGridRangeZeroDoesNotSendFarGridContents()
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

        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetMaxUpdateRange, 32f);
            confMan.SetCVar(CVars.NetPvsGridRange, 0f);
        });

        await RunTicksSync(server, client, 5);

        EntityUid farGrid = default;
        EntityUid farEntity = default;
        await server.WaitPost(() =>
        {
            var map = server.System<SharedMapSystem>().CreateMap(out var mapId);

            var farGridComp = mapMan.CreateGridEntity(mapId);
            farGrid = farGridComp.Owner;
            mapSys.SetTile(farGridComp, Vector2i.Zero, new Tile(1));
            xforms.SetLocalPosition(farGrid, new Vector2(1024f, 0f));

            farEntity = sEntMan.SpawnEntity(null, new EntityCoordinates(farGrid, new Vector2(0.5f, 0.5f)));

            var player = sEntMan.SpawnEntity(null, new EntityCoordinates(map, new Vector2(0.5f, 0.5f)));
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 5);

        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(farGrid), out _), Is.True);
        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(farEntity), out _), Is.False);
    }

    [Test]
    public async Task TestMapCullingCanBeDisabled()
    {
        await using var pair = await StartConnectedPair();
        var (client, server) = pair;

        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();

        server.Post(() =>
        {
            confMan.SetCVar(CVars.NetPVS, true);
            confMan.SetCVar(CVars.NetPvsMapCulling, false);
        });

        await RunTicksSync(server, client, 5);

        EntityUid map1 = default;
        EntityUid map2 = default;
        await server.WaitPost(() =>
        {
            map1 = server.System<SharedMapSystem>().CreateMap();
            map2 = server.System<SharedMapSystem>().CreateMap();
            var player = sEntMan.SpawnEntity(null, new EntityCoordinates(map1, default));

            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicksSync(server, client, 5);

        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(map1), out _), Is.True);
        Assert.That(cEntMan.TryGetEntity(sEntMan.GetNetEntity(map2), out _), Is.True);
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

        await RunTicksSync(server, client, 5);
        await WaitUntilSync(server, client);

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

        await RunTicksSync(server, client, 5);

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
        await RunTicksSync(server, client, 5);

        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map1));

        Assert.That(cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(!cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));

        // Delete the original map.
        await server.WaitPost(() => sEntMan.DeleteEntity(map2));
        await RunTicksSync(server, client, 5);

        Assert.That(xform.ParentUid, Is.EqualTo(grid));
        Assert.That(xform.GridUid, Is.EqualTo(grid));
        Assert.That(xform.MapUid, Is.EqualTo(map1));

        Assert.That(cEntMan.TryGetEntity(nEntity, out _));
        Assert.That(cEntMan.TryGetEntity(nMap1, out _));
        Assert.That(!cEntMan.TryGetEntity(nMap2, out _));
        Assert.That(cEntMan.TryGetEntity(nGrid, out _));

    }
}
