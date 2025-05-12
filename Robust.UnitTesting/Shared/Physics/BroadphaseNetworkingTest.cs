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
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

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
        var cPlayerMan = client.ResolveDependency<ISharedPlayerManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var fixturesSystem = sEntMan.EntitySysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = sEntMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = sEntMan.EntitySysManager.GetEntitySystem<SharedMapSystem>();

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
            map1 = mapSystem.CreateMap(out var mapId);
            var gridEnt = mapMan.CreateGridEntity(mapId);
            mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
            grid1 = gridEnt.Owner;
        });

        var map1Net = sEntMan.GetNetEntity(map1);

        // Spawn player entity on grid 1
        EntityUid player = default;
        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(grid1, new Vector2(0.5f, 0.5f));
            player = sEntMan.SpawnEntity(null, coords);

            // Enable physics
            var physics = sEntMan.AddComponent<PhysicsComponent>(player);
            var xform = sEntMan.GetComponent<TransformComponent>(player);
            var shape = new PolygonShape();
            shape.SetAsBox(0.5f, 0.5f);
            var fixture = new Fixture(shape, 0, 0, true);
            fixturesSystem.CreateFixture(player, "fix1", fixture, body: physics, xform: xform);
            physicsSystem.SetCanCollide(player, true, body: physics);
            physicsSystem.SetBodyType(player, BodyType.Dynamic);
            Assert.That(physics.CanCollide);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        var playerNet = sEntMan.GetNetEntity(player);

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Check player got properly attached
        await client.WaitPost(() =>
        {
            var ent = cEntMan.GetNetEntity(cPlayerMan.LocalEntity);
            Assert.That(ent, Is.EqualTo(playerNet));
        });

        var sPlayerXform = sEntMan.GetComponent<TransformComponent>(player);
        var cPlayerXform = cEntMan.GetComponent<TransformComponent>(cEntMan.GetEntity(playerNet));

        // Client initially has correct transform data.
        var broadphase = new BroadphaseData(grid1, map1, true, false);
        var grid1Net = sEntMan.GetNetEntity(grid1);

        Assert.That(cPlayerXform.GridUid, Is.EqualTo(cEntMan.GetEntity(grid1Net)));
        Assert.That(sPlayerXform.GridUid, Is.EqualTo(grid1));
        Assert.That(cPlayerXform.MapUid, Is.EqualTo(cEntMan.GetEntity(map1Net)));
        Assert.That(sPlayerXform.MapUid, Is.EqualTo(map1));

        Assert.That(cPlayerXform.Broadphase?.Uid, Is.EqualTo(cEntMan.GetEntity(sEntMan.GetNetEntity(broadphase.Uid))));
        Assert.That(cPlayerXform.Broadphase?.PhysicsMap, Is.EqualTo(cEntMan.GetEntity(sEntMan.GetNetEntity(broadphase.PhysicsMap))));
        Assert.That(cPlayerXform.Broadphase?.Static, Is.EqualTo(broadphase.Static));
        Assert.That(cPlayerXform.Broadphase?.CanCollide, Is.EqualTo(broadphase.CanCollide));
        Assert.That(sPlayerXform.Broadphase, Is.EqualTo(broadphase));

        // Set up maps 2 & grid 2 and move the player there (in the same tick).
        EntityUid grid2 = default;
        EntityUid map2 = default;
        await server.WaitPost(() =>
        {
            // Create grid
            map2 = mapSystem.CreateMap(out var mapId);
            var gridEnt = mapMan.CreateGridEntity(mapId);
            mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
            grid2 = gridEnt.Owner;

            // Move player
            var coords = new EntityCoordinates(grid2, Vector2.Zero);
            sEntMan.System<SharedTransformSystem>().SetCoordinates(player, coords);

        });

        var grid2Net = sEntMan.GetNetEntity(grid2);
        var map2Net = sEntMan.GetNetEntity(map2);

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Player & server xforms should match.
        broadphase = new BroadphaseData(grid2, map2, true, false);
        Assert.That(cEntMan.GetNetEntity(cPlayerXform.GridUid), Is.EqualTo(grid2Net));
        Assert.That(sPlayerXform.GridUid, Is.EqualTo(grid2));
        Assert.That(cEntMan.GetNetEntity(cPlayerXform.MapUid), Is.EqualTo(map2Net));
        Assert.That(sPlayerXform.MapUid, Is.EqualTo(map2));

        Assert.That(cPlayerXform.Broadphase?.Uid, Is.EqualTo(cEntMan.GetEntity(sEntMan.GetNetEntity(broadphase.Uid))));
        Assert.That(cPlayerXform.Broadphase?.PhysicsMap, Is.EqualTo(cEntMan.GetEntity(sEntMan.GetNetEntity(broadphase.PhysicsMap))));
        Assert.That(cPlayerXform.Broadphase?.Static, Is.EqualTo(broadphase.Static));
        Assert.That(cPlayerXform.Broadphase?.CanCollide, Is.EqualTo(broadphase.CanCollide));
        Assert.That(sPlayerXform.Broadphase, Is.EqualTo(broadphase));

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

