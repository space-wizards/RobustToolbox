using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameStates;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Server.GameStates;

public sealed class ChunkEntityPvsTest : RobustIntegrationTest
{
    /// <summary>
    /// Verifies that chunk entities follow normal PVS visibility: they are exposed while in range, filtered while
    /// detached, and exposed again after re-entering range.
    /// </summary>
    [Test]
    public async Task ChunkEntityDetachesAndReattachesWithPvs()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var netMan = client.ResolveDependency<IClientNetManager>();
        var xforms = sEntMan.System<SharedTransformSystem>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (var i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        NetEntity player = default;
        NetEntity chunkEntity = default;
        NetEntity mapNet = default;
        EntityUid map = default;
        EntityCoordinates origin = default;

        await server.WaitPost(() =>
        {
            // Create a player and a chunk entity on the same map chunk so the client initially receives it via PVS.
            map = server.System<SharedMapSystem>().CreateMap();
            origin = new EntityCoordinates(map, default);
            mapNet = sEntMan.GetNetEntity(map);

            var playerUid = sEntMan.SpawnEntity(null, origin);
            player = sEntMan.GetNetEntity(playerUid);

            var chunk = sEntMan.System<ChunkEntitySystem>().GetOrCreateChunk(map, Vector2i.Zero);
            chunkEntity = sEntMan.GetNetEntity(chunk.Owner);

            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);
        });

        for (var i = 0; i < 20; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        MetaDataComponent? chunkMeta = null;
        await client.WaitPost(() =>
        {
            // The chunk starts in range, so it should be registered and visible to TryGetChunk.
            Assert.That(cEntMan.TryGetEntityData(chunkEntity, out _, out chunkMeta), Is.True);
            Assert.That(cEntMan.TryGetEntity(mapNet, out var cMap), Is.True);
            Assert.That(cEntMan.System<ChunkEntitySystem>().TryGetChunk(cMap!.Value, Vector2i.Zero, out var chunk), Is.True);
            Assert.That(chunk!.Value.Owner, Is.EqualTo(cEntMan.GetEntity(chunkEntity)));
            Assert.That(chunkMeta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
        });

        await server.WaitPost(() =>
        {
            // Move the player far enough away that the original map chunk leaves their PVS.
            xforms.SetCoordinates(sEntMan.GetEntity(player), new EntityCoordinates(map, new Vector2(1000, 1000)));
        });

        for (var i = 0; i < 20; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        await client.WaitPost(() =>
        {
            // Detached chunk entities must be filtered out so client-side chunk lookups don't expose it anymore.
            Assert.That(chunkMeta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
            Assert.That(cEntMan.TryGetEntity(mapNet, out var cMap), Is.True);
            Assert.That(cEntMan.System<ChunkEntitySystem>().TryGetChunk(cMap!.Value, Vector2i.Zero, out _), Is.False);
        });

        await server.WaitPost(() =>
        {
            // Moving back into range should reattach the same chunk entity and make it queryable again.
            xforms.SetCoordinates(sEntMan.GetEntity(player), origin);
        });

        for (var i = 0; i < 20; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        await client.WaitPost(() =>
        {
            Assert.That(chunkMeta!.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(cEntMan.TryGetEntity(mapNet, out var cMap), Is.True);
            Assert.That(cEntMan.System<ChunkEntitySystem>().TryGetChunk(cMap!.Value, Vector2i.Zero, out var chunk), Is.True);
            Assert.That(chunk!.Value.Owner, Is.EqualTo(cEntMan.GetEntity(chunkEntity)));
        });

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}
