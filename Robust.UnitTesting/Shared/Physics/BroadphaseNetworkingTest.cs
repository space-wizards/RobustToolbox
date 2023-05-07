using System.Linq;
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

namespace Robust.UnitTesting.Shared.Physics;

public sealed class BroadphaseNetworkingTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that the transform/broadphase is properly networked when a player moves to a newly spawned map/grid.
    /// </summary>
    /// <remarks>
    /// See PR #3919 or issue #3924
    /// </remarks>
    [Test]
    public async Task TestBroadphaseNetworking()
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
        var fixturesSystem = sEntMan.EntitySysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = sEntMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Set up maps 1 & grid 1
        EntityUid grid1 = default;
        EntityUid map1 = default;
        await server.WaitPost(() =>
        {
            var mapId = mapMan.CreateMap();
            map1 = mapMan.GetMapEntityId(mapId);
            var gridComp = mapMan.CreateGrid(mapId);
            gridComp.SetTile(Vector2i.Zero, new Tile(1));
            grid1 = gridComp.Owner;
        });

        // Spawn player entity on grid 1
        EntityUid player = default;
        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(grid1, (0.5f, 0.5f));
            player = sEntMan.SpawnEntity("", coords);

            // Enable physics
            var physics = sEntMan.AddComponent<PhysicsComponent>(player);
            var xform = sEntMan.GetComponent<TransformComponent>(player);
            var shape = new PolygonShape();
            shape.SetAsBox(0.5f, 0.5f);
            var fixture = new Fixture("fix1", shape, 0, 0, true);
            fixturesSystem.CreateFixture(player, fixture, body: physics, xform: xform);
            physicsSystem.SetCanCollide(player, true, body: physics);
            physicsSystem.SetBodyType(player, BodyType.Dynamic);
            Assert.That(physics.CanCollide);

            // Attach player.
            var session = (IPlayerSession) sPlayerMan.Sessions.First();
            session.AttachToEntity(player);
            session.JoinGame();
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check player got properly attached
        await client.WaitPost(() =>
        {
            var ent = cPlayerMan.LocalPlayer?.ControlledEntity;
            Assert.That(ent, Is.EqualTo(player));
        });

        var sPlayerXform = sEntMan.GetComponent<TransformComponent>(player);
        var cPlayerXform = cEntMan.GetComponent<TransformComponent>(player);

        // Client initially has correct transform data.
        var broadphase = new BroadphaseData(grid1, map1, true, false);
        Assert.That(cPlayerXform.GridUid, Is.EqualTo(grid1));
        Assert.That(sPlayerXform.GridUid, Is.EqualTo(grid1));
        Assert.That(cPlayerXform.MapUid, Is.EqualTo(map1));
        Assert.That(sPlayerXform.MapUid, Is.EqualTo(map1));
        Assert.That(cPlayerXform.Broadphase, Is.EqualTo(broadphase));
        Assert.That(sPlayerXform.Broadphase, Is.EqualTo(broadphase));

        // Set up maps 2 & grid 2 and move the player there (in the same tick).
        EntityUid grid2 = default;
        EntityUid map2 = default;
        await server.WaitPost(() =>
        {
            // Create grid
            var mapId = mapMan.CreateMap();
            map2 = mapMan.GetMapEntityId(mapId);
            var gridComp = mapMan.CreateGrid(mapId);
            gridComp.SetTile(Vector2i.Zero, new Tile(1));
            grid2 = gridComp.Owner;

            // Move player
            var coords = new EntityCoordinates(grid2, Vector2.Zero);
            sEntMan.System<SharedTransformSystem>().SetCoordinates(player, coords);

        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Player & server xforms should match.
        broadphase = new BroadphaseData(grid2, map2, true, false);
        Assert.That(cPlayerXform.GridUid, Is.EqualTo(grid2));
        Assert.That(sPlayerXform.GridUid, Is.EqualTo(grid2));
        Assert.That(cPlayerXform.MapUid, Is.EqualTo(map2));
        Assert.That(sPlayerXform.MapUid, Is.EqualTo(map2));
        Assert.That(cPlayerXform.Broadphase, Is.EqualTo(broadphase));
        Assert.That(sPlayerXform.Broadphase, Is.EqualTo(broadphase));
    }
}

