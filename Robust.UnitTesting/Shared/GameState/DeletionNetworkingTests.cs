using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using cIPlayerManager = Robust.Client.Player.IPlayerManager;
using sIPlayerManager = Robust.Server.Player.IPlayerManager;

// ReSharper disable AccessToStaticMemberViaDerivedType

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
        var cPlayerMan = client.ResolveDependency<cIPlayerManager>();
        var sPlayerMan = server.ResolveDependency<sIPlayerManager>();
        var xformSys = sEntMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

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
        EntityUid map = default;
        await server.WaitPost(() =>
        {
            var mapId = mapMan.CreateMap();
            map = mapMan.GetMapEntityId(mapId);
            var gridComp = mapMan.CreateGrid(mapId);
            gridComp.SetTile(Vector2i.Zero, new Tile(1));
            grid1 = gridComp.Owner;
            xformSys.SetLocalPosition(grid1, new Vector2(-2,0));

            gridComp = mapMan.CreateGrid(mapId);
            gridComp.SetTile(Vector2i.Zero, new Tile(1));
            grid2 = gridComp.Owner;
            xformSys.SetLocalPosition(grid2, new Vector2(2,0));
        });

        // Spawn player entity on grid 1
        EntityUid player = default;
        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(grid1, new Vector2(0.5f, 0.5f));
            player = sEntMan.SpawnEntity("", coords);
            var session = (IPlayerSession) sPlayerMan.Sessions.First();
            session.AttachToEntity(player);
            session.JoinGame();
        });

        await RunTicks();

        // Check player got properly attached
        await client.WaitPost(() =>
        {
            var ent = cPlayerMan.LocalPlayer?.ControlledEntity;
            Assert.That(ent, Is.EqualTo(player));
        });

        // Spawn two entities, each with one child.
        EntityUid entA = default;
        EntityUid entB = default;
        EntityUid childA = default;
        EntityUid childB = default;
        var coords = new EntityCoordinates(grid2, new Vector2(0.5f, 0.5f));
        await server.WaitPost(() =>
        {
            entA = sEntMan.SpawnEntity("", coords);
            entB = sEntMan.SpawnEntity("", coords);
            childA = sEntMan.SpawnEntity("", new EntityCoordinates(entA, default));
            childB = sEntMan.SpawnEntity("", new EntityCoordinates(entB, default));
        });

        await RunTicks();

        // Spawn client-side children and one client-side entity
        EntityUid entC = default;
        EntityUid childC = default;
        EntityUid clientChildA = default;
        EntityUid clientChildB = default;
        await client.WaitPost(() =>
        {
            entC = cEntMan.SpawnEntity("", coords);
            childC = cEntMan.SpawnEntity("", new EntityCoordinates(entC, default));
            clientChildA = cEntMan.SpawnEntity("", new EntityCoordinates(entA, default));
            clientChildB = cEntMan.SpawnEntity("", new EntityCoordinates(entB, default));
        });

        await RunTicks();

        // Verify entities exist and have the correct parents.
        EntityUid Parent(EntityUid uid) => cEntMan!.GetComponent<TransformComponent>(uid).ParentUid;
        await client.WaitPost(() =>
        {
            // Exist
            Assert.That(cEntMan.EntityExists(entA));
            Assert.That(cEntMan.EntityExists(entB));
            Assert.That(cEntMan.EntityExists(entC));
            Assert.That(cEntMan.EntityExists(childA));
            Assert.That(cEntMan.EntityExists(childB));
            Assert.That(cEntMan.EntityExists(childC));
            Assert.That(cEntMan.EntityExists(clientChildA));
            Assert.That(cEntMan.EntityExists(clientChildB));

            // Client-side where appropriate
            Assert.That(entC.IsClientSide());
            Assert.That(childC.IsClientSide());
            Assert.That(clientChildA.IsClientSide());
            Assert.That(clientChildB.IsClientSide());
            Assert.That(!entA.IsClientSide());
            Assert.That(!entB.IsClientSide());
            Assert.That(!childA.IsClientSide());
            Assert.That(!childB.IsClientSide());

            // Correct parents.
            Assert.That(Parent(entA), Is.EqualTo(grid2));
            Assert.That(Parent(entB), Is.EqualTo(grid2));
            Assert.That(Parent(entC), Is.EqualTo(grid2));
            Assert.That(Parent(childA), Is.EqualTo(entA));
            Assert.That(Parent(childB), Is.EqualTo(entB));
            Assert.That(Parent(childC), Is.EqualTo(entC));
            Assert.That(Parent(clientChildA), Is.EqualTo(entA));
            Assert.That(Parent(clientChildB), Is.EqualTo(entB));
        });

        // Delete client-side entity.
        await client.WaitPost(() => cEntMan.DeleteEntity(entC));
        await RunTicks();
        await client.WaitPost(() =>
        {
            Assert.That(!cEntMan.EntityExists(entC));
            Assert.That(!cEntMan.EntityExists(childC));
        });

        // Delete server-side entity.
        await server.WaitPost(() => sEntMan.DeleteEntity(entB));
        await RunTicks();
        await client.WaitPost(() =>
        {
            Assert.That(!cEntMan.EntityExists(entB));
            Assert.That(!cEntMan.EntityExists(childB));

            // Was never explicitly deleted by the client.
            Assert.That(cEntMan.EntityExists(clientChildB));
        });

        // Delete the grid (and thus also entity A and all the children)
        await server.WaitPost(() => sEntMan.DeleteEntity(grid2));
        await RunTicks();
        await client.WaitPost(() =>
        {
            Assert.That(!cEntMan.EntityExists(grid2));
            Assert.That(!cEntMan.EntityExists(entA));
            Assert.That(!cEntMan.EntityExists(childA));

            // Was never explicitly deleted by the client.
            Assert.That(cEntMan.EntityExists(clientChildA));
        });
    }
}

