using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.GameState;

public sealed partial class AutoNetworkingTests : RobustIntegrationTest
{
    /// <summary>
    /// Does basic testing for AutoNetworkedFieldAttribute and AutoGenerateComponentStateAttribute
    /// to make sure the datafields are correctly networked to the client when dirtied.
    /// TODO: Add test for field deltas.
    /// </summary>
    [Test]
    public async Task AutoNetworkingTest()
    {
        var serverOpts = new ServerIntegrationOptions { Pool = false };
        var clientOpts = new ClientIntegrationOptions { Pool = false };
        using var server = StartServer(serverOpts);
        using var client = StartClient(clientOpts);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        var netMan = client.ResolveDependency<IClientNetManager>();

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
        var coords = new EntityCoordinates(map, default);
        EntityUid player = default;
        EntityUid cPlayer = default;
        EntityUid serverEnt1 = default;
        EntityUid serverEnt2 = default;
        EntityUid serverEnt3 = default;
        NetEntity serverNet1 = default;
        NetEntity serverNet2 = default;
        NetEntity serverNet3 = default;

        await server.WaitPost(() =>
        {
            // Attach player.
            player = server.EntMan.SpawnAttachedTo(null, coords);
            var session = server.PlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            server.PlayerMan.JoinGame(session);

            // Spawn test entities.
            serverEnt1 = server.EntMan.SpawnAttachedTo(null, coords);
            serverEnt2 = server.EntMan.SpawnAttachedTo(null, coords);
            serverEnt3 = server.EntMan.SpawnAttachedTo(null, coords);
            serverNet1 = server.EntMan.GetNetEntity(serverEnt1);
            serverNet2 = server.EntMan.GetNetEntity(serverEnt2);
            serverNet3 = server.EntMan.GetNetEntity(serverEnt3);

            // Setup components
            server.EntMan.EnsureComponent<AutoNetworkingTestComponent>(serverEnt1);
            server.EntMan.EnsureComponent<AutoNetworkingTestChildComponent>(serverEnt2);
            server.EntMan.EnsureComponent<AutoNetworkingTestEmptyChildComponent>(serverEnt3);
        });

        await RunTicks();

        // check client
        await client.WaitPost(() =>
        {
            // Get the client-side entities
            cPlayer = client.EntMan.GetEntity(server.EntMan.GetNetEntity(player));
            var clientEnt1 = client.EntMan.GetEntity(serverNet1);
            var clientEnt2 = client.EntMan.GetEntity(serverNet2);
            var clientEnt3 = client.EntMan.GetEntity(serverNet3);

            // Check player got properly attached
            Assert.That(client.AttachedEntity, Is.EqualTo(cPlayer));
            Assert.That(client.EntMan.EntityExists(cPlayer));

            // Get the client-side components
            Assert.That(client.EntMan.TryGetComponent(clientEnt1, out AutoNetworkingTestComponent? cmpClient1));
            Assert.That(client.EntMan.TryGetComponent(clientEnt2, out AutoNetworkingTestChildComponent? cmpClient2));
            Assert.That(client.EntMan.TryGetComponent(clientEnt3, out AutoNetworkingTestEmptyChildComponent? cmpClient3));

            // All datafields should be the default value
            Assert.That(cmpClient1?.IsNetworked, Is.EqualTo(1));
            Assert.That(cmpClient1?.NotNetworked, Is.EqualTo(2));

            Assert.That(cmpClient2?.ChildNetworked, Is.EqualTo(1));
            Assert.That(cmpClient2?.Child, Is.EqualTo(2));
            Assert.That(cmpClient2?.ParentNetworked, Is.EqualTo(3));
            Assert.That(cmpClient2?.Parent, Is.EqualTo(4));

            Assert.That(cmpClient3?.ParentNetworked, Is.EqualTo(3));
            Assert.That(cmpClient3?.Parent, Is.EqualTo(4));
        });

        // make changes on the server
        await server.WaitPost(() =>
        {
            // Get the server-side components
            var cmpServer1 = server.EntMan.GetComponent<AutoNetworkingTestComponent>(serverEnt1);
            var cmpServer2 = server.EntMan.GetComponent<AutoNetworkingTestChildComponent>(serverEnt2);
            var cmpServer3 = server.EntMan.GetComponent<AutoNetworkingTestEmptyChildComponent>(serverEnt3);

            // All datafields should be the default value
            Assert.That(cmpServer1.IsNetworked, Is.EqualTo(1));
            Assert.That(cmpServer1.NotNetworked, Is.EqualTo(2));

            Assert.That(cmpServer2.ChildNetworked, Is.EqualTo(1));
            Assert.That(cmpServer2.Child, Is.EqualTo(2));
            Assert.That(cmpServer2.ParentNetworked, Is.EqualTo(3));
            Assert.That(cmpServer2.Parent, Is.EqualTo(4));

            Assert.That(cmpServer3.ParentNetworked, Is.EqualTo(3));
            Assert.That(cmpServer3.Parent, Is.EqualTo(4));

            // change the datafields and dirty them
            cmpServer1.IsNetworked = 101;
            cmpServer1.NotNetworked = 102;
            cmpServer2.ChildNetworked = 101;
            cmpServer2.Child = 102;
            cmpServer2.ParentNetworked = 103;
            cmpServer2.Parent = 104;
            cmpServer3.ParentNetworked = 103;
            cmpServer3.Parent = 104;

            server.EntMan.Dirty(serverEnt1, cmpServer1);
            server.EntMan.Dirty(serverEnt2, cmpServer2);
            server.EntMan.Dirty(serverEnt3, cmpServer3);
        });

        await RunTicks();

        // check the client again
        await client.WaitPost(() =>
        {
            // Get the client-side entities
            cPlayer = client.EntMan.GetEntity(server.EntMan.GetNetEntity(player));
            var clientEnt1 = client.EntMan.GetEntity(serverNet1);
            var clientEnt2 = client.EntMan.GetEntity(serverNet2);
            var clientEnt3 = client.EntMan.GetEntity(serverNet3);

            // Get the client-side components
            Assert.That(client.EntMan.TryGetComponent(clientEnt1, out AutoNetworkingTestComponent? cmpClient1));
            Assert.That(client.EntMan.TryGetComponent(clientEnt2, out AutoNetworkingTestChildComponent? cmpClient2));
            Assert.That(client.EntMan.TryGetComponent(clientEnt3, out AutoNetworkingTestEmptyChildComponent? cmpClient3));

            // All datafields should be the default value
            Assert.That(cmpClient1?.IsNetworked, Is.EqualTo(101));
            Assert.That(cmpClient1?.NotNetworked, Is.EqualTo(2)); // unchanged

            Assert.That(cmpClient2?.ChildNetworked, Is.EqualTo(101));
            Assert.That(cmpClient2?.Child, Is.EqualTo(2)); // unchanged
            Assert.That(cmpClient2?.ParentNetworked, Is.EqualTo(103));
            Assert.That(cmpClient2?.Parent, Is.EqualTo(4)); // unchanged

            Assert.That(cmpClient3?.ParentNetworked, Is.EqualTo(103));
            Assert.That(cmpClient3?.Parent, Is.EqualTo(4)); // unchanged
        });

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
public sealed partial class AutoNetworkingTestComponent : Component
{
    [DataField, AutoNetworkedField]
    public int IsNetworked = 1;

    [DataField]
    public int NotNetworked = 2;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoNetworkingTestChildComponent : AutoNetworkingTestParentComponent
{
    [DataField, AutoNetworkedField]
    public int ChildNetworked = 1;

    [DataField]
    public int Child = 2;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoNetworkingTestEmptyChildComponent : AutoNetworkingTestParentComponent
{
}

public abstract partial class AutoNetworkingTestParentComponent : Component
{
    [DataField, AutoNetworkedField]
    public int ParentNetworked = 3;

    [DataField]
    public int Parent = 4;
}
