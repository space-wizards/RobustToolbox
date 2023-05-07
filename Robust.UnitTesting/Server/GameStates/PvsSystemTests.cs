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
using cIPlayerManager = Robust.Client.Player.IPlayerManager;
using sIPlayerManager = Robust.Server.Player.IPlayerManager;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class PvsSystemTests : RobustIntegrationTest
{
    /// <summary>
    /// Checks that there are no issues when an entity changes PVS chunk location multiple times in a single tick.
    /// </summary>
    [Test]
    public async Task TestMultipleIndexChange()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var mapMan = server.ResolveDependency<IMapManager>();
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var cPlayerMan = client.ResolveDependency<cIPlayerManager>();
        var sPlayerMan = server.ResolveDependency<sIPlayerManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Set up map and grid
        EntityUid grid = default;
        EntityUid map = default;
        await server.WaitPost(() =>
        {
            var mapId = mapMan.CreateMap();
            map = mapMan.GetMapEntityId(mapId);
            var gridComp = mapMan.CreateGrid(mapId);
            gridComp.SetTile(Vector2i.Zero, new Tile(1));
            grid = gridComp.Owner;
        });

        // Spawn player entity on grid 1
        EntityUid player = default;
        EntityUid other = default;
        TransformComponent otherXform = default!;
        var gridCoords = new EntityCoordinates(grid, (0.5f, 0.5f));
        var mapCoords = new EntityCoordinates(map, (2, 2));
        await server.WaitPost(() =>
        {
            player = sEntMan.SpawnEntity("", gridCoords);
            other = sEntMan.SpawnEntity("", gridCoords);
            otherXform = sEntMan.GetComponent<TransformComponent>(other);

            // Ensure map PVS chunk is not empty
            sEntMan.SpawnEntity("", mapCoords);

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

        // Move the player off-grid and back onto the grid in the same tick
        xforms.SetCoordinates(other, otherXform, mapCoords);
        xforms.SetCoordinates(other, otherXform, gridCoords);

        // Run for a few ticks. The test just checks that no PVS asserts/errors happen.
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Repeat but in the opposite direction ( map -> grid -> map )
        // first move to map and wait a bit.
        xforms.SetCoordinates(other, otherXform, mapCoords);
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Move to and off grid in the same tick
        xforms.SetCoordinates(other, otherXform, gridCoords);
        xforms.SetCoordinates(other, otherXform, mapCoords);

        // wait for errors.
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }
    }
}

