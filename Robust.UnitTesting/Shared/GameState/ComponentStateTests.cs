using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.GameState;

public sealed partial class ComponentStateTests : RobustIntegrationTest
{
    /// <summary>
    /// This tests performs a basic check to ensure that there is no issue with entity states referencing other
    /// entities that the client is not yet aware of. It does this by spawning two entities that reference each other,
    /// and then ensuring that they get sent to the client one at a time.
    /// </summary>
    [Test]
    public async Task UnknownEntityTest()
    {
        // Setup auto-comp-states. I hate this. Someone please fix reflection in RobustIntegrationTest
        var serverOpts = new ServerIntegrationOptions { Pool = false };
        var clientOpts = new ClientIntegrationOptions { Pool = false };
        var server = StartServer(serverOpts);
        var client = StartClient(clientOpts);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        var netMan = client.ResolveDependency<IClientNetManager>();
        var xforms = server.System<SharedTransformSystem>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => server.CfgMan.SetCVar(CVars.NetPVS, true));

        // Set up map.
        EntityUid map = default;
        server.Post(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap();
        });

        await RunTicks();

        // Spawn entities
        var coordsA = new EntityCoordinates(map, default);
        var coordsB = new EntityCoordinates(map, new Vector2(100, 100));
        EntityUid player = default;
        EntityUid cPlayer = default;
        EntityUid serverEntA = default;
        EntityUid serverEntB = default;
        NetEntity serverNetA = default;
        NetEntity serverNetB = default;

        await server.WaitPost(() =>
        {
            // Attach player.
            player = server.EntMan.Spawn();
            var session = server.PlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            server.PlayerMan.JoinGame(session);

            // Spawn test entities.
            serverEntA = server.EntMan.SpawnAttachedTo(null, coordsA);
            serverEntB = server.EntMan.SpawnAttachedTo(null, coordsB);
            serverNetA = server.EntMan.GetNetEntity(serverEntA);
            serverNetB = server.EntMan.GetNetEntity(serverEntB);

            // Setup components
            var cmp = server.EntMan.EnsureComponent<UnknownEntityTestComponent>(serverEntA);
            cmp.Other = serverEntB;
            server.EntMan.Dirty(serverEntA, cmp);

            cmp = server.EntMan.EnsureComponent<UnknownEntityTestComponent>(serverEntB);
            cmp.Other = serverEntA;
            server.EntMan.Dirty(serverEntB, cmp);
        });

        await RunTicks();

        // Check player got properly attached and only knows about the expected entities
        await client.WaitPost(() =>
        {
            cPlayer = client.EntMan.GetEntity(server.EntMan.GetNetEntity(player));
            Assert.That(client.AttachedEntity, Is.EqualTo(cPlayer));
            Assert.That(client.EntMan.EntityExists(cPlayer));
            Assert.That(client.EntMan.EntityExists(client.EntMan.GetEntity(serverNetA)), Is.False);
            Assert.That(client.EntMan.EntityExists(client.EntMan.GetEntity(serverNetB)), Is.False);
        });

        // Move the player into PVS range of one of the entities.
        await server.WaitPost(() => xforms.SetCoordinates(player, coordsB));
        await RunTicks();

        await client.WaitPost(() =>
        {
            var clientEntA = client.EntMan.GetEntity(serverNetA);
            var clientEntB = client.EntMan.GetEntity(serverNetB);
            Assert.That(client.EntMan.EntityExists(clientEntB), Is.True);
            Assert.That(client.EntMan.EntityExists(client.EntMan.GetEntity(serverNetA)), Is.False);

            Assert.That(client.EntMan.TryGetComponent(clientEntB, out UnknownEntityTestComponent? cmp));
            Assert.That(cmp?.Other, Is.EqualTo(clientEntA));
        });

        // Move the player into PVS range of the other entity
        await server.WaitPost(() => xforms.SetCoordinates(player, coordsA));
        await RunTicks();

        await client.WaitPost(() =>
        {
            var clientEntA = client.EntMan.GetEntity(serverNetA);
            var clientEntB = client.EntMan.GetEntity(serverNetB);
            Assert.That(client.EntMan.EntityExists(clientEntB), Is.True);
            Assert.That(client.EntMan.EntityExists(clientEntA), Is.True);

            Assert.That(client.EntMan.TryGetComponent(clientEntB, out UnknownEntityTestComponent? cmp));
            Assert.That(cmp?.Other, Is.EqualTo(clientEntA));

            Assert.That(client.EntMan.TryGetComponent(clientEntA, out cmp));
            Assert.That(cmp?.Other, Is.EqualTo(clientEntB));
        });

        server.Post(() => server.CfgMan.SetCVar(CVars.NetPVS, false));

        // wait for errors.
        await RunTicks();

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server!.WaitRunTicks(1);
                await client!.WaitRunTicks(1);
            }
        }

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }

    /// <summary>
    /// This is a variant of <see cref="UnknownEntityTest"/> that deletes one of the entities before the other entity gets sent.
    /// </summary>
    [Test]
    public async Task UnknownEntityDeleteTest()
    {
        // The first chunk of the test just follows UnknownEntityTest
        var serverOpts = new ServerIntegrationOptions { Pool = false };
        var clientOpts = new ClientIntegrationOptions { Pool = false };
        var server = StartServer(serverOpts);
        var client = StartClient(clientOpts);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        var netMan = client.ResolveDependency<IClientNetManager>();
        var xforms = server.System<SharedTransformSystem>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => server.CfgMan.SetCVar(CVars.NetPVS, true));

        // Set up map.
        EntityUid map = default;
        server.Post(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap();
        });

        await RunTicks();

        // Spawn entities
        var coordsA = new EntityCoordinates(map, default);
        var coordsB = new EntityCoordinates(map, new Vector2(100, 100));
        EntityUid player = default;
        EntityUid cPlayer = default;
        EntityUid serverEntA = default;
        EntityUid serverEntB = default;
        NetEntity serverNetA = default;
        NetEntity serverNetB = default;

        await server.WaitPost(() =>
        {
            // Attach player.
            player = server.EntMan.Spawn();
            var session = server.PlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            server.PlayerMan.JoinGame(session);

            // Spawn test entities.
            serverEntA = server.EntMan.SpawnAttachedTo(null, coordsA);
            serverEntB = server.EntMan.SpawnAttachedTo(null, coordsB);
            serverNetA = server.EntMan.GetNetEntity(serverEntA);
            serverNetB = server.EntMan.GetNetEntity(serverEntB);

            // Setup components
            var cmp = server.EntMan.EnsureComponent<UnknownEntityTestComponent>(serverEntA);
            cmp.Other = serverEntB;
            server.EntMan.Dirty(serverEntA, cmp);

            cmp = server.EntMan.EnsureComponent<UnknownEntityTestComponent>(serverEntB);
            cmp.Other = serverEntA;
            server.EntMan.Dirty(serverEntB, cmp);
        });

        await RunTicks();

        // Check player got properly attached and only knows about the expected entities
        await client.WaitPost(() =>
        {
            cPlayer = client.EntMan.GetEntity(server.EntMan.GetNetEntity(player));
            Assert.That(client.AttachedEntity, Is.EqualTo(cPlayer));
            Assert.That(client.EntMan.EntityExists(cPlayer));
            Assert.That(client.EntMan.EntityExists(client.EntMan.GetEntity(serverNetA)), Is.False);
            Assert.That(client.EntMan.EntityExists(client.EntMan.GetEntity(serverNetB)), Is.False);
        });

        // Move the player into PVS range of one of the entities.
        await server.WaitPost(() => xforms.SetCoordinates(player, coordsB));
        await RunTicks();

        await client.WaitPost(() =>
        {
            var clientEntA = client.EntMan.GetEntity(serverNetA);
            var clientEntB = client.EntMan.GetEntity(serverNetB);
            Assert.That(client.EntMan.EntityExists(clientEntB), Is.True);
            Assert.That(client.EntMan.EntityExists(clientEntA), Is.False);

            Assert.That(client.EntMan.TryGetComponent(clientEntB, out UnknownEntityTestComponent? cmp));
            Assert.That(cmp?.Other, Is.EqualTo(clientEntA));
        });

        // This is where the test difffers from UnknownEntityTest:
        // We delete the entity that the player knows about before it receives the entity that it references.
        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(serverEntB);
            var comp = server.EntMan.GetComponent<UnknownEntityTestComponent>(serverEntA);
            comp.Other = EntityUid.Invalid;
            server.EntMan.Dirty(serverEntA, comp);
        });
        await RunTicks();

        await client.WaitPost(() =>
        {
            var clientEntA = client.EntMan.GetEntity(serverNetA);
            var clientEntB = client.EntMan.GetEntity(serverNetB);
            Assert.That(clientEntA, Is.EqualTo(EntityUid.Invalid));
            Assert.That(clientEntB, Is.EqualTo(EntityUid.Invalid));
        });

        // Move the player into PVS range of the other entity
        await server.WaitPost(() => xforms.SetCoordinates(player, coordsA));
        await RunTicks();

        await client.WaitPost(() =>
        {
            var clientEntA = client.EntMan.GetEntity(serverNetA);
            var clientEntB = client.EntMan.GetEntity(serverNetB);
            Assert.That(clientEntB, Is.EqualTo(EntityUid.Invalid));
            Assert.That(client.EntMan.EntityExists(clientEntA), Is.True);
            Assert.That(client.EntMan.TryGetComponent(clientEntA, out UnknownEntityTestComponent? cmp));
            Assert.That(cmp?.Other, Is.EqualTo(EntityUid.Invalid));
        });

        server.Post(() => server.CfgMan.SetCVar(CVars.NetPVS, false));

        // wait for errors.
        await RunTicks();

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server!.WaitRunTicks(1);
                await client!.WaitRunTicks(1);
            }
        }

        await client.WaitPost(() => netMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnknownEntityTestComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Other;
}
