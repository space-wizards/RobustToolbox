using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class DetachedParentTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that the client can handle an entity getting attached to an entity that is outside of their PVS range, or
    /// that they have never seen. Previously this could result in entities with improperly assigned GridUids due to
    /// an existing/initialized entity being attached to an un-initialized entity on an already initialized grid.
    /// </summary>
    [Test]
    public async Task TestDetachedParent()
    {
        var server = StartServer(new() {Pool = false});
        var client = StartClient(new() {Pool = false});

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapSys = server.System<SharedMapSystem>();
        var xformSys = server.System<SharedTransformSystem>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (var i = 0; i < 10; i++)
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
        MapId mapId = default;
        EntityUid map = default;
        EntityUid grid = default;
        EntityUid parent = default;
        EntityUid player = default;
        EntityUid child = default;
        EntityCoordinates gridCoords = default;
        EntityCoordinates mapCoords = default;
        await server.WaitPost(() =>
        {
            // Cycle through some EntityUids to avoid server-side and client-side uids accidentally matching up.
            // I made a mistake earlier in this test where I used a server-side uid on the client
            for (var i = 0; i < 10; i++)
            {
                server.EntMan.DeleteEntity(server.EntMan.SpawnEntity(null, MapCoordinates.Nullspace));
            }

            map = mapSys.CreateMap(out mapId);

            var gridEnt = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridEnt.Owner, gridEnt.Comp, Vector2i.Zero, new Tile(1));
            gridCoords = new EntityCoordinates(gridEnt, .5f, .5f);
            mapCoords = new EntityCoordinates(map, 200, 200);
            grid = gridEnt.Owner;

            parent = sEntMan.SpawnEntity(null, gridCoords);
            player = sEntMan.SpawnEntity(null, gridCoords);
            child = sEntMan.SpawnEntity(null, mapCoords);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check that transforms are as expected.
        var childX = server.Transform(child);
        var parentX = server.Transform(parent);
        var playerX = server.Transform(player);
        var gridX = server.Transform(grid);

        Assert.That(childX.MapID, Is.EqualTo(mapId));
        Assert.That(parentX.MapID, Is.EqualTo(mapId));
        Assert.That(playerX.MapID, Is.EqualTo(mapId));
        Assert.That(gridX.MapID, Is.EqualTo(mapId));

        Assert.That(childX.ParentUid, Is.EqualTo(map));
        Assert.That(parentX.ParentUid, Is.EqualTo(grid));
        Assert.That(playerX.ParentUid, Is.EqualTo(grid));
        Assert.That(gridX.ParentUid, Is.EqualTo(map));

        Assert.That(childX.GridUid, Is.Null);
        Assert.That(parentX.GridUid, Is.EqualTo(grid));
        Assert.That(playerX.GridUid, Is.EqualTo(grid));
        Assert.That(gridX.GridUid, Is.EqualTo(grid));

        // Check that the player received the entities, and that their transforms are as expected.
        // Note that the child entity should be outside of PVS range.

        var cMap = client.EntMan.GetEntity(server.EntMan.GetNetEntity(map));
        var cGrid = client.EntMan.GetEntity(server.EntMan.GetNetEntity(grid));
        var cPlayer = client.EntMan.GetEntity(server.EntMan.GetNetEntity(player));
        var cParent = client.EntMan.GetEntity(server.EntMan.GetNetEntity(parent));
        var cChild = client.EntMan.GetEntity(server.EntMan.GetNetEntity(child));

        Assert.That(cMap, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cGrid, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cPlayer, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cParent, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cChild, Is.EqualTo(EntityUid.Invalid));

        var cParentX = client.Transform(cParent);
        var cPlayerX = client.Transform(cPlayer);
        var cGridX = client.Transform(cGrid);

        Assert.That(cParentX.MapID, Is.EqualTo(mapId));
        Assert.That(cPlayerX.MapID, Is.EqualTo(mapId));
        Assert.That(cGridX.MapID, Is.EqualTo(mapId));

        Assert.That(cParentX.ParentUid, Is.EqualTo(cGrid));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cGrid));
        Assert.That(cGridX.ParentUid, Is.EqualTo(cMap));

        Assert.That(cParentX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cPlayerX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cGridX.GridUid, Is.EqualTo(cGrid));

        // Move the player into pvs range of the child, which will move them outside of the grid & parent's PVS range.
        await server.WaitPost(() => xformSys.SetCoordinates(player, mapCoords));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // the client now knows about the child.
        cChild = client.EntMan.GetEntity(server.EntMan.GetNetEntity(child));
        Assert.That(cChild, Is.Not.EqualTo(EntityUid.Invalid));
        var cChildX = client.Transform(cChild);
        Assert.That(childX.MapID, Is.EqualTo(mapId));
        Assert.That(cChildX.ParentUid, Is.EqualTo(cMap));
        Assert.That(cChildX.GridUid, Is.Null);

        // Player transform has updated
        Assert.That(cPlayerX.GridUid, Is.Null);
        Assert.That(cPlayerX.MapID, Is.EqualTo(mapId));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cMap));

        // But the other entities have left PVS range
        Assert.That(cParentX.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(cParentX.MapID, Is.EqualTo(MapId.Nullspace));
        Assert.That(cParentX.GridUid, Is.Null);
        Assert.That((client.MetaData(cParent).Flags & MetaDataFlags.Detached) != 0);

        // Attach the child & player entities to the parent
        // This is the main step that the test is actually checking

        var parentCoords = new EntityCoordinates(parent, Vector2.Zero);
        await server.WaitPost(() => xformSys.SetCoordinates(player, parentCoords));
        await server.WaitPost(() => xformSys.SetCoordinates(child, parentCoords));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check that server-side transforms are as expected
        Assert.That(childX.ParentUid, Is.EqualTo(parent));
        Assert.That(parentX.ParentUid, Is.EqualTo(grid));
        Assert.That(playerX.ParentUid, Is.EqualTo(parent));
        Assert.That(gridX.ParentUid, Is.EqualTo(map));

        Assert.That(childX.GridUid, Is.EqualTo(grid));
        Assert.That(parentX.GridUid, Is.EqualTo(grid));
        Assert.That(playerX.GridUid, Is.EqualTo(grid));
        Assert.That(gridX.GridUid, Is.EqualTo(grid));

        // Next check the client-side transforms
        Assert.That((client.MetaData(cParent).Flags & MetaDataFlags.Detached) == 0);

        Assert.That(cChildX.ParentUid, Is.EqualTo(cParent));
        Assert.That(cParentX.ParentUid, Is.EqualTo(cGrid));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cParent));
        Assert.That(cGridX.ParentUid, Is.EqualTo(cMap));

        Assert.That(cChildX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cParentX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cPlayerX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cGridX.GridUid, Is.EqualTo(cGrid));

        // Repeat the previous test, but this time attaching to an entity that gets spawned outside of PVS range, that
        // the client never new about previously.
        await server.WaitPost(() => xformSys.SetCoordinates(player, mapCoords));
        await server.WaitPost(() => xformSys.SetCoordinates(child, mapCoords));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Child transform has updated.
        Assert.That(childX.MapID, Is.EqualTo(mapId));
        Assert.That(cChildX.ParentUid, Is.EqualTo(cMap));
        Assert.That(cChildX.GridUid, Is.Null);

        // Player transform has updated
        Assert.That(cPlayerX.GridUid, Is.Null);
        Assert.That(cPlayerX.MapID, Is.EqualTo(mapId));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cMap));

        // The other entities have left PVS range
        Assert.That(cParentX.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(cParentX.MapID, Is.EqualTo(MapId.Nullspace));
        Assert.That(cParentX.GridUid, Is.Null);
        Assert.That((client.MetaData(cParent).Flags & MetaDataFlags.Detached) != 0);

        // Create a new parent entity
        EntityUid parent2 = default;
        await server.WaitPost(() => parent2 = sEntMan.SpawnEntity(null, gridCoords));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var parent2X = server.Transform(parent2);
        Assert.That(parent2X.MapID, Is.EqualTo(mapId));
        Assert.That(parent2X.ParentUid, Is.EqualTo(grid));
        Assert.That(parent2X.GridUid, Is.EqualTo(grid));

        // Client does not know that parent2 exists yet.
        var cParent2 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(parent2));
        Assert.That(cParent2, Is.EqualTo(EntityUid.Invalid));

        // Attach player & child to the new parent.
        var parent2Coords = new EntityCoordinates(parent2, Vector2.Zero);
        await server.WaitPost(() => xformSys.SetCoordinates(player, parent2Coords));
        await server.WaitPost(() => xformSys.SetCoordinates(child, parent2Coords));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check all the transforms
        cParent2 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(parent2));
        Assert.That(cParent2, Is.Not.EqualTo(EntityUid.Invalid));
        var cParent2X = client.Transform(cParent2);

        Assert.That(cChildX.ParentUid, Is.EqualTo(cParent2));
        Assert.That(cParent2X.ParentUid, Is.EqualTo(cGrid));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cParent2));
        Assert.That(cGridX.ParentUid, Is.EqualTo(cMap));

        Assert.That(cParent2X.GridUid, Is.EqualTo(cGrid));
        Assert.That(cChildX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cPlayerX.GridUid, Is.EqualTo(cGrid));
        Assert.That(cGridX.GridUid, Is.EqualTo(cGrid));

        // Repeat again, but with a new map.
        // Set up map and spawn player
        MapId mapId2 = default;
        EntityUid map2 = default;
        EntityUid grid2 = default;
        EntityUid parent3 = default;
        await server.WaitPost(() =>
        {
            map2 = mapSys.CreateMap(out mapId2);
            var gridEnt = mapMan.CreateGridEntity(mapId2);
            mapSys.SetTile(gridEnt.Owner, gridEnt.Comp, Vector2i.Zero, new Tile(1));
            var grid2Coords = new EntityCoordinates(gridEnt, .5f, .5f);
            grid2 = gridEnt.Owner;
            parent3 = sEntMan.SpawnEntity(null, grid2Coords);
        });

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check server-side transforms
        var grid2X = server.Transform(grid2);
        var parent3X = server.Transform(parent3);

        Assert.That(parent3X.MapID, Is.EqualTo(mapId2));
        Assert.That(grid2X.MapID, Is.EqualTo(mapId2));

        Assert.That(parent3X.ParentUid, Is.EqualTo(grid2));
        Assert.That(grid2X.ParentUid, Is.EqualTo(map2));

        Assert.That(parent3X.GridUid, Is.EqualTo(grid2));
        Assert.That(grid2X.GridUid, Is.EqualTo(grid2));

        // Client does not know that parent3 exists, but (at least for now) clients always know about all maps and grids.
        var cParent3 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(parent3));
        var cGrid2 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(grid2));
        var cMap2 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(map2));
        Assert.That(cMap2, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cGrid2, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cParent3, Is.EqualTo(EntityUid.Invalid));

        // Attach the entities to the parent on the new map.
        var parent3Coords = new EntityCoordinates(parent3, Vector2.Zero);
        await server.WaitPost(() => xformSys.SetCoordinates(player, parent3Coords));
        await server.WaitPost(() => xformSys.SetCoordinates(child, parent3Coords));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check all the transforms
        cParent3 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(parent3));
        Assert.That(cParent3, Is.Not.EqualTo(EntityUid.Invalid));

        var cParent3X = client.Transform(cParent3);
        var cGrid2X = client.Transform(cGrid2);

        Assert.That(cChildX.ParentUid, Is.EqualTo(cParent3));
        Assert.That(cParent3X.ParentUid, Is.EqualTo(cGrid2));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cParent3));
        Assert.That(cGrid2X.ParentUid, Is.EqualTo(cMap2));

        Assert.That(cParent3X.GridUid, Is.EqualTo(cGrid2));
        Assert.That(cChildX.GridUid, Is.EqualTo(cGrid2));
        Assert.That(cPlayerX.GridUid, Is.EqualTo(cGrid2));
        Assert.That(cGrid2X.GridUid, Is.EqualTo(cGrid2));

        Assert.That(cParent3X.MapID, Is.EqualTo(mapId2));
        Assert.That(cChildX.MapID, Is.EqualTo(mapId2));
        Assert.That(cPlayerX.MapID, Is.EqualTo(mapId2));
        Assert.That(cGrid2X.MapID, Is.EqualTo(mapId2));

        Assert.That(cParent3X.MapUid, Is.EqualTo(cMap2));
        Assert.That(cChildX.MapUid, Is.EqualTo(cMap2));
        Assert.That(cPlayerX.MapUid, Is.EqualTo(cMap2));
        Assert.That(cGrid2X.MapUid, Is.EqualTo(cMap2));


        // Create a new map & grid and move entities in the same tick
        MapId mapId3 = default;
        EntityUid map3 = default;
        EntityUid grid3 = default;
        EntityUid parent4 = default;
        await server.WaitPost(() =>
        {
            map3 = mapSys.CreateMap(out mapId3);
            var gridEnt = mapMan.CreateGridEntity(mapId3);
            mapSys.SetTile(gridEnt.Owner, gridEnt.Comp, Vector2i.Zero, new Tile(1));
            var grid3Coords = new EntityCoordinates(gridEnt, .5f, .5f);
            grid3 = gridEnt.Owner;
            parent4 = sEntMan.SpawnEntity(null, grid3Coords);

            var parent4Coords = new EntityCoordinates(parent4, Vector2.Zero);

            // Move existing entity to new parent
            xformSys.SetCoordinates(player, parent4Coords);

            // Move existing parent & child combination to new grid
            xformSys.SetCoordinates(parent3, grid3Coords);
        });


        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check all the transforms
        var cParent4 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(parent4));
        var cMap3 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(map3));
        var cGrid3 = client.EntMan.GetEntity(server.EntMan.GetNetEntity(grid3));

        Assert.That(cParent4, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cMap3, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(cGrid3, Is.Not.EqualTo(EntityUid.Invalid));

        var cParent4X = client.Transform(cParent4);
        var cGrid3X = client.Transform(cGrid3);

        Assert.That(cChildX.ParentUid, Is.EqualTo(cParent3));
        Assert.That(cPlayerX.ParentUid, Is.EqualTo(cParent4));
        Assert.That(cParent3X.ParentUid, Is.EqualTo(cGrid3));
        Assert.That(cParent4X.ParentUid, Is.EqualTo(cGrid3));
        Assert.That(cGrid3X.ParentUid, Is.EqualTo(cMap3));

        Assert.That(cChildX.GridUid, Is.EqualTo(cGrid3));
        Assert.That(cPlayerX.GridUid, Is.EqualTo(cGrid3));
        Assert.That(cParent3X.GridUid, Is.EqualTo(cGrid3));
        Assert.That(cParent4X.GridUid, Is.EqualTo(cGrid3));
        Assert.That(cGrid3X.GridUid, Is.EqualTo(cGrid3));

        Assert.That(cChildX.MapID, Is.EqualTo(mapId3));
        Assert.That(cPlayerX.MapID, Is.EqualTo(mapId3));
        Assert.That(cParent3X.MapID, Is.EqualTo(mapId3));
        Assert.That(cParent4X.MapID, Is.EqualTo(mapId3));
        Assert.That(cGrid3X.MapID, Is.EqualTo(mapId3));

        Assert.That(cChildX.MapUid, Is.EqualTo(cMap3));
        Assert.That(cPlayerX.MapUid, Is.EqualTo(cMap3));
        Assert.That(cParent3X.MapUid, Is.EqualTo(cMap3));
        Assert.That(cParent4X.MapUid, Is.EqualTo(cMap3));
        Assert.That(cGrid3X.MapUid, Is.EqualTo(cMap3));

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

