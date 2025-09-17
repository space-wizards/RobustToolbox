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

namespace Robust.UnitTesting.Shared.GameState;

/// <summary>
/// This test checks that when entities get deleted, the client receives the game states and deletes the entities.
/// </summary>
/// <remarks>
/// Should help prevent the issue fixed in PR #4044 from reoccurring.
/// </remarks>
public sealed class DeletionNetworkingTests : RobustIntegrationTest
{
    [Test]
    public async Task DeletionNetworkingTest()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var cPlayerMan = client.ResolveDependency<ISharedPlayerManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var xformSys = sEntMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var mapSys = sEntMan.EntitySysManager.GetEntitySystem<SharedMapSystem>();

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

        // Set up map & grids
        EntityUid grid1 = default;
        EntityUid grid2 = default;
        NetEntity grid1Net = default;
        NetEntity grid2Net = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out var mapId);
            var gridComp = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridComp, Vector2i.Zero, new Tile(1));
            grid1 = gridComp.Owner;
            xformSys.SetLocalPosition(grid1, new Vector2(-2,0));
            grid1Net = sEntMan.GetNetEntity(grid1);

            gridComp = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridComp, Vector2i.Zero, new Tile(1));
            grid2 = gridComp.Owner;
            xformSys.SetLocalPosition(grid2, new Vector2(2,0));
            grid2Net = sEntMan.GetNetEntity(grid2);
        });

        // Spawn player entity on grid 1
        EntityUid player = default;
        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(grid1, new Vector2(0.5f, 0.5f));
            player = sEntMan.SpawnEntity(null, coords);
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicks();

        // Check player got properly attached
        await client.WaitPost(() =>
        {
            var ent = cEntMan.GetNetEntity(cPlayerMan.LocalEntity);
            Assert.That(ent, Is.EqualTo(sEntMan.GetNetEntity(player)));
        });

        // Spawn two entities, each with one child.
        EntityUid entA = default;
        EntityUid entB = default;
        EntityUid childA = default;
        EntityUid childB = default;

        NetEntity entANet = default;
        NetEntity entBNet = default;
        NetEntity childANet = default;
        NetEntity childBNet = default!;

        var coords = new EntityCoordinates(grid2, new Vector2(0.5f, 0.5f));
        await server.WaitPost(() =>
        {
            entA = sEntMan.SpawnEntity(null, coords);
            entB = sEntMan.SpawnEntity(null, coords);
            childA = sEntMan.SpawnEntity(null, new EntityCoordinates(entA, default));
            childB = sEntMan.SpawnEntity(null, new EntityCoordinates(entB, default));

            entANet = sEntMan.GetNetEntity(entA);
            entBNet = sEntMan.GetNetEntity(entB);
            childANet = sEntMan.GetNetEntity(childA);
            childBNet = sEntMan.GetNetEntity(childB);
        });

        await RunTicks();

        // Get the client version of the entities.
        var cEntA = cEntMan.GetEntity(entANet);
        var cEntB = cEntMan.GetEntity(entBNet);
        var cChildA = cEntMan.GetEntity(childANet);
        var cChildB = cEntMan.GetEntity(childBNet);
        var cGrid2 = cEntMan.GetEntity(grid2Net);

        // Spawn client-side children and one client-side entity
        EntityUid cEntC = default;
        EntityUid cChildC = default;
        EntityUid clientChildA = default;
        EntityUid clientChildB = default;

        NetEntity entCNet = NetEntity.Invalid;

        await client.WaitPost(() =>
        {
            cEntC = cEntMan.SpawnEntity(null, cEntMan.GetCoordinates(sEntMan.GetNetCoordinates(coords)));
            entCNet = cEntMan.GetNetEntity(cEntC);
            cChildC = cEntMan.SpawnEntity(null, new EntityCoordinates(cEntC, default));
            clientChildA = cEntMan.SpawnEntity(null, new EntityCoordinates(cEntA, default));
            clientChildB = cEntMan.SpawnEntity(null, new EntityCoordinates(cEntB, default));
        });

        await RunTicks();

        // Verify entities exist and have the correct parents.
        NetEntity Parent(EntityUid uid) => cEntMan.GetNetEntity(cEntMan.GetComponent<TransformComponent>(uid).ParentUid);
        await client.WaitPost(() =>
        {
            // Exist
            Assert.That(cEntMan.EntityExists(cEntA));
            Assert.That(cEntMan.EntityExists(cEntB));
            Assert.That(cEntMan.EntityExists(cEntC));
            Assert.That(cEntMan.EntityExists(cChildA));
            Assert.That(cEntMan.EntityExists(cChildB));
            Assert.That(cEntMan.EntityExists(cChildC));
            Assert.That(cEntMan.EntityExists(clientChildA));
            Assert.That(cEntMan.EntityExists(clientChildB));

            // Client-side where appropriate
            Assert.That(cEntMan.IsClientSide(cEntC));
            Assert.That(cEntMan.IsClientSide(cChildC));
            Assert.That(cEntMan.IsClientSide(clientChildA));
            Assert.That(cEntMan.IsClientSide(clientChildB));
            Assert.That(!cEntMan.IsClientSide(cEntA));
            Assert.That(!cEntMan.IsClientSide(cEntB));
            Assert.That(!cEntMan.IsClientSide(cChildA));
            Assert.That(!cEntMan.IsClientSide(cChildB));

            // Correct parents.

            Assert.That(Parent(cEntA), Is.EqualTo(grid2Net));
            Assert.That(Parent(cEntB), Is.EqualTo(grid2Net));
            Assert.That(Parent(cEntC), Is.EqualTo(grid2Net));
            Assert.That(Parent(cChildA), Is.EqualTo(entANet));
            Assert.That(Parent(cChildB), Is.EqualTo(entBNet));
            Assert.That(Parent(cChildC), Is.EqualTo(entCNet));
            Assert.That(Parent(clientChildA), Is.EqualTo(entANet));
            Assert.That(Parent(clientChildB), Is.EqualTo(entBNet));
        });

        // Delete client-side entity.
        await client.WaitPost(() => cEntMan.DeleteEntity(cEntC));
        await RunTicks();
        await client.WaitPost(() =>
        {
            Assert.That(!cEntMan.EntityExists(cEntC));
            Assert.That(!cEntMan.EntityExists(cChildC));
        });

        // Delete server-side entity.
        await server.WaitPost(() => sEntMan.DeleteEntity(entB));
        await RunTicks();
        await client.WaitPost(() =>
        {
            Assert.That(!cEntMan.EntityExists(cEntB));
            Assert.That(!cEntMan.EntityExists(cChildB));

            // Was never explicitly deleted by the client.
            Assert.That(cEntMan.EntityExists(clientChildB));
        });

        // Delete the grid (and thus also entity A and all the children)
        await server.WaitPost(() => sEntMan.DeleteEntity(grid2));
        await RunTicks();
        await client.WaitPost(() =>
        {
            Assert.That(!cEntMan.EntityExists(cGrid2));
            Assert.That(!cEntMan.EntityExists(cEntA));
            Assert.That(!cEntMan.EntityExists(cChildA));

            // Was never explicitly deleted by the client.
            Assert.That(cEntMan.EntityExists(clientChildA));
        });

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

