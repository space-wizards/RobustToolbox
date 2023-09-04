using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;

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
        var compReg = () => IoCManager.Resolve<IComponentFactory>().RegisterClass<UnknownEntityTestComponent>();
        var sysReg = () => IoCManager.Resolve<IEntitySystemManager>().LoadExtraSystemType<UnknownEntityTestComponent.UnknownEntityTestComponent_AutoNetworkSystem>();
        var serverOpts = new ServerIntegrationOptions
        {
            Pool = false,
            BeforeRegisterComponents = compReg,
            BeforeStart = sysReg,
        };
        var clientOpts = new ClientIntegrationOptions
        {
            Pool = false,
            BeforeRegisterComponents = compReg,
            BeforeStart = sysReg,
        };
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
        await server.WaitPost(() =>
        {
            var mapId = server.MapMan.CreateMap();
            map = server.MapMan.GetMapEntityId(mapId);
        });

        await RunTicks();

        // Spawn entities
        var coordsA = new EntityCoordinates(map, default);
        var coordsB = new EntityCoordinates(map, new Vector2(100, 100));
        EntityUid player = default;
        EntityUid entA = default;
        EntityUid entB = default;

        await server.WaitPost(() =>
        {
            // Attach player.
            player = server.EntMan.Spawn();
            var session = (IPlayerSession) server.PlayerMan.Sessions.First();
            server.System<ActorSystem>().Attach(player, session);
            session.JoinGame();

            // Spawn test entities.
            entA = server.EntMan.SpawnAttachedTo(null, coordsA);
            entB = server.EntMan.SpawnAttachedTo(null, coordsB);

            // Setup components
            var cmp = server.EntMan.EnsureComponent<UnknownEntityTestComponent>(entA);
            cmp.Other = entB;
            server.EntMan.Dirty(entA, cmp);

            cmp = server.EntMan.EnsureComponent<UnknownEntityTestComponent>(entB);
            cmp.Other = entA;
            server.EntMan.Dirty(entB, cmp);
        });

        await RunTicks();

        // Check player got properly attached and only knows about the expected entities
        await client.WaitPost(() =>
        {
            Assert.That(client.AttachedEntity, Is.EqualTo(player));
            Assert.That(client.EntMan.EntityExists(player));
            Assert.That(client.EntMan.EntityExists(entA), Is.False);
            Assert.That(client.EntMan.EntityExists(entB), Is.False);
        });

        // Move the player into PVS range of one of the entities.
        await server.WaitPost(() => xforms.SetCoordinates(player, coordsB));
        await RunTicks();

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(entB), Is.True);
            Assert.That(client.EntMan.EntityExists(entA), Is.False);

            Assert.That(client.EntMan.TryGetComponent(entB, out UnknownEntityTestComponent? cmp));
            Assert.That(cmp?.Other, Is.EqualTo(entA));
        });

        // Move the player into PVS range of the other entity
        await server.WaitPost(() => xforms.SetCoordinates(player, coordsA));
        await RunTicks();

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(entB), Is.True);
            Assert.That(client.EntMan.EntityExists(entA), Is.True);

            Assert.That(client.EntMan.TryGetComponent(entB, out UnknownEntityTestComponent? cmp));
            Assert.That(cmp?.Other, Is.EqualTo(entA));

            Assert.That(client.EntMan.TryGetComponent(entA, out cmp));
            Assert.That(cmp?.Other, Is.EqualTo(entB));
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
    }

}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnknownEntityTestComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? Other;
}