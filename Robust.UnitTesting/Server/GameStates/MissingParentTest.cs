using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class MissingParentTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that PVS & clients can handle entities being sent before their parents are.
    /// </summary>
    [Test]
    public async Task TestMissingParent()
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
        var cConfMan = client.ResolveDependency<IConfigurationManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Limit client to receiving at most 1 entity per tick.
        cConfMan.SetCVar(CVars.NetPVSEntityBudget, 1);

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

        // Set up map and spawn player
        NetEntity player = default;
        NetEntity entity = default;
        EntityCoordinates coords = default;
        NetCoordinates nCoords = default;
        await server.WaitPost(() =>
        {
            var map = server.System<SharedMapSystem>().CreateMap();
            coords = new(map, default);

            var playerUid = sEntMan.SpawnEntity(null, coords);
            var entUid = sEntMan.SpawnEntity(null, coords);
            entity = sEntMan.GetNetEntity(entUid);
            player = sEntMan.GetNetEntity(playerUid);
            nCoords = sEntMan.GetNetCoordinates(coords);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        Assert.That(player, Is.Not.EqualTo(NetEntity.Invalid));
        Assert.That(entity, Is.Not.EqualTo(NetEntity.Invalid));

        // Check player got properly attached, and has received the other entity.
        Assert.That(cEntMan.TryGetEntityData(entity, out _, out var meta));
        Assert.That(cEntMan.TryGetEntity(player, out var cPlayerUid));
        Assert.That(cPlayerMan.LocalEntity, Is.EqualTo(cPlayerUid));
        Assert.That(server.Transform(player).Coordinates, Is.EqualTo(coords));
        Assert.That(client.Transform(player).Coordinates, Is.EqualTo(client.EntMan.GetCoordinates(nCoords)));
        Assert.That(client.Transform(entity).ParentUid.IsValid(), Is.True);
        Assert.That(client.MetaData(entity).Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        // Spawn 20 new entities
        NetEntity first = default;
        NetEntity last = default;
        await server.WaitPost(() =>
        {
            first = sEntMan.GetNetEntity(sEntMan.SpawnEntity(null, coords));
            for (var i = 0; i < 18; i++)
            {
                sEntMan.SpawnEntity(null, coords);
            }
            last = sEntMan.GetNetEntity(sEntMan.SpawnEntity(null, coords));
        });

        // Wait for the client to receive some, but not all, of the entities
        for (int i = 0; i < 8; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }
        Assert.That(cEntMan.TryGetEntity(first, out _), Is.True);
        Assert.That(cEntMan.TryGetEntity(last, out _), Is.False);

        // Re-parent the known entity to an entity that the client has not received yet.
        await server.WaitPost(() =>
        {
            var newCoords = new EntityCoordinates(sEntMan.GetEntity(last), default);
            server.System<SharedTransformSystem>().SetCoordinates(sEntMan.GetEntity(entity), newCoords);
        });

        // Wait a few more ticks
        for (int i = 0; i < 8; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // Client should still not have received the new parent, however this shouldn't cause any issues.
        // The already known entity should just have been moved to nullspace.
        Assert.That(cEntMan.TryGetEntity(last, out _), Is.False);
        Assert.That(client.Transform(entity).ParentUid.IsValid(), Is.False);
        Assert.That(client.MetaData(entity).Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        // Wait untill the client receives the parent entity
        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        // now that the parent was received the entity should no longer be in nullspace.
        Assert.That(cEntMan.TryGetEntity(last, out var newParent), Is.True);
        Assert.That(client.Transform(entity).ParentUid.IsValid(), Is.True);
        Assert.That(client.Transform(entity).ParentUid, Is.EqualTo(newParent));
        Assert.That(client.MetaData(entity).Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

