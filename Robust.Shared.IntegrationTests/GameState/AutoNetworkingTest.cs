using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.GameState;

internal sealed partial class AutoNetworkingTests : RobustIntegrationTest
{
    /// <summary>
    /// Does basic testing for AutoNetworkedFieldAttribute and AutoGenerateComponentStateAttribute
    /// to make sure the datafields are correctly networked to the client when dirtied.
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

        // Spawn entities.
        var coords = new EntityCoordinates(map, default);
        EntityUid player = default;
        EntityUid cPlayer = default;
        EntityUid clientEnt1 = default;
        EntityUid clientEnt2 = default;
        EntityUid clientEnt3 = default;
        EntityUid clientEnt4 = default;
        EntityUid clientEnt5 = default;
        EntityUid clientEnt6 = default;
        EntityUid serverEnt1 = default;
        EntityUid serverEnt2 = default;
        EntityUid serverEnt3 = default;
        EntityUid serverEnt4 = default;
        EntityUid serverEnt5 = default;
        EntityUid serverEnt6 = default;
        NetEntity netEnt1 = default;
        NetEntity netEnt2 = default;
        NetEntity netEnt3 = default;
        NetEntity netEnt4 = default;
        NetEntity netEnt5 = default;
        NetEntity netEnt6 = default;

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
            serverEnt4 = server.EntMan.SpawnAttachedTo(null, coords);
            serverEnt5 = server.EntMan.SpawnAttachedTo(null, coords);
            serverEnt6 = server.EntMan.SpawnAttachedTo(null, coords);
            netEnt1 = server.EntMan.GetNetEntity(serverEnt1);
            netEnt2 = server.EntMan.GetNetEntity(serverEnt2);
            netEnt3 = server.EntMan.GetNetEntity(serverEnt3);
            netEnt4 = server.EntMan.GetNetEntity(serverEnt4);
            netEnt5 = server.EntMan.GetNetEntity(serverEnt5);
            netEnt6 = server.EntMan.GetNetEntity(serverEnt6);

            // Setup components.
            server.EntMan.EnsureComponent<AutoNetworkingTestComponent>(serverEnt1);
            server.EntMan.EnsureComponent<AutoNetworkingTestChildComponent>(serverEnt2);
            server.EntMan.EnsureComponent<AutoNetworkingTestEmptyChildComponent>(serverEnt3);
            server.EntMan.EnsureComponent<AutoNetworkingTestFieldDeltaComponent>(serverEnt4);
            server.EntMan.EnsureComponent<AutoNetworkingTestFieldDeltaComponent>(serverEnt5);
            server.EntMan.EnsureComponent<AutoNetworkingTestFieldDeltaComponent>(serverEnt6);
        });

        await RunTicks();

        // Check client.
        await client.WaitPost(() =>
        {
            // Get the client-side entities.
            cPlayer = client.EntMan.GetEntity(server.EntMan.GetNetEntity(player));
            clientEnt1 = client.EntMan.GetEntity(netEnt1);
            clientEnt2 = client.EntMan.GetEntity(netEnt2);
            clientEnt3 = client.EntMan.GetEntity(netEnt3);
            clientEnt4 = client.EntMan.GetEntity(netEnt4);
            clientEnt5 = client.EntMan.GetEntity(netEnt5);
            clientEnt6 = client.EntMan.GetEntity(netEnt5);

            // Check that the player got properly attached.
            Assert.That(client.AttachedEntity, Is.EqualTo(cPlayer));
            Assert.That(client.EntMan.EntityExists(cPlayer));

            // Get the client-side components.
            Assert.That(client.EntMan.TryGetComponent(clientEnt1, out AutoNetworkingTestComponent? clientComp1));
            Assert.That(client.EntMan.TryGetComponent(clientEnt2, out AutoNetworkingTestChildComponent? clientComp2));
            Assert.That(client.EntMan.TryGetComponent(clientEnt3, out AutoNetworkingTestEmptyChildComponent? clientComp3));
            Assert.That(client.EntMan.TryGetComponent(clientEnt4, out AutoNetworkingTestFieldDeltaComponent? clientComp4));
            Assert.That(client.EntMan.TryGetComponent(clientEnt5, out AutoNetworkingTestFieldDeltaComponent? clientComp5));
            Assert.That(client.EntMan.TryGetComponent(clientEnt6, out AutoNetworkingTestFieldDeltaComponent? clientComp6));

            // All datafields should be the default value.
            Assert.That(clientComp1?.IsNetworked, Is.EqualTo(1));
            Assert.That(clientComp1?.NotNetworked, Is.EqualTo(2));

            Assert.That(clientComp2?.ChildNetworked, Is.EqualTo(1));
            Assert.That(clientComp2?.Child, Is.EqualTo(2));
            Assert.That(clientComp2?.ParentNetworked, Is.EqualTo(3));
            Assert.That(clientComp2?.Parent, Is.EqualTo(4));

            Assert.That(clientComp3?.ParentNetworked, Is.EqualTo(3));
            Assert.That(clientComp3?.Parent, Is.EqualTo(4));

            Assert.That(clientComp4?.Field1, Is.EqualTo(1));
            Assert.That(clientComp4?.Field2, Is.EqualTo(2));
            Assert.That(clientComp4?.Field3, Is.EqualTo(3));

            Assert.That(clientComp5?.Field1, Is.EqualTo(1));
            Assert.That(clientComp5?.Field2, Is.EqualTo(2));
            Assert.That(clientComp5?.Field3, Is.EqualTo(3));

            Assert.That(clientComp6?.Field1, Is.EqualTo(1));
            Assert.That(clientComp6?.Field2, Is.EqualTo(2));
            Assert.That(clientComp6?.Field3, Is.EqualTo(3));
        });

        // Make changes on the server.
        await server.WaitPost(() =>
        {
            // Get the server-side components.
            var serverComp1 = server.EntMan.GetComponent<AutoNetworkingTestComponent>(serverEnt1);
            var serverComp2 = server.EntMan.GetComponent<AutoNetworkingTestChildComponent>(serverEnt2);
            var serverComp3 = server.EntMan.GetComponent<AutoNetworkingTestEmptyChildComponent>(serverEnt3);
            var serverComp4 = server.EntMan.GetComponent<AutoNetworkingTestFieldDeltaComponent>(serverEnt4);
            var serverComp5 = server.EntMan.GetComponent<AutoNetworkingTestFieldDeltaComponent>(serverEnt5);
            var serverComp6 = server.EntMan.GetComponent<AutoNetworkingTestFieldDeltaComponent>(serverEnt6);

            // All datafields should be the default value
            Assert.That(serverComp1.IsNetworked, Is.EqualTo(1));
            Assert.That(serverComp1.NotNetworked, Is.EqualTo(2));

            Assert.That(serverComp2.ChildNetworked, Is.EqualTo(1));
            Assert.That(serverComp2.Child, Is.EqualTo(2));
            Assert.That(serverComp2.ParentNetworked, Is.EqualTo(3));
            Assert.That(serverComp2.Parent, Is.EqualTo(4));

            Assert.That(serverComp3.ParentNetworked, Is.EqualTo(3));
            Assert.That(serverComp3.Parent, Is.EqualTo(4));

            Assert.That(serverComp4?.Field1, Is.EqualTo(1));
            Assert.That(serverComp4?.Field2, Is.EqualTo(2));
            Assert.That(serverComp4?.Field3, Is.EqualTo(3));

            Assert.That(serverComp5?.Field1, Is.EqualTo(1));
            Assert.That(serverComp5?.Field2, Is.EqualTo(2));
            Assert.That(serverComp5?.Field3, Is.EqualTo(3));

            Assert.That(serverComp6?.Field1, Is.EqualTo(1));
            Assert.That(serverComp6?.Field2, Is.EqualTo(2));
            Assert.That(serverComp6?.Field3, Is.EqualTo(3));

            // Test that a field with AutoNetworkedField gets networked.
            serverComp1.IsNetworked = 101;
            // Test that a field without AutoNetworkedField does not get networked.
            serverComp1.NotNetworked = 102;
            server.EntMan.Dirty(serverEnt1, serverComp1);
            // Test that inherited autonetworked fields get networked.
            serverComp2.ChildNetworked = 101;
            serverComp2.Child = 102;
            serverComp2.ParentNetworked = 103;
            serverComp2.Parent = 104;
            server.EntMan.Dirty(serverEnt2, serverComp2);
            serverComp3.ParentNetworked = 103;
            serverComp3.Parent = 104;
            server.EntMan.Dirty(serverEnt3, serverComp3);

            // Use an autogenerated delta state to only network a single field.
            serverComp4.Field1 = 101;
            serverComp4.Field2 = 102;
            serverComp4.Field3 = 103;
            server.EntMan.DirtyField(serverEnt4, serverComp4, nameof(AutoNetworkingTestFieldDeltaComponent.Field3));

            // Check that calling both Dirty and then DirtyField will send a full state.
            serverComp5.Field1 = 101;
            serverComp5.Field2 = 102;
            serverComp5.Field3 = 103;
            server.EntMan.DirtyField(serverEnt5, serverComp5, nameof(AutoNetworkingTestFieldDeltaComponent.Field3));
            server.EntMan.Dirty(serverEnt5, serverComp5);

            // Check that calling both DirtyField and then Dirty will send a full state.
            serverComp6.Field1 = 101;
            serverComp6.Field2 = 102;
            serverComp6.Field3 = 103;
            server.EntMan.Dirty(serverEnt6, serverComp6);
            server.EntMan.DirtyField(serverEnt6, serverComp6, nameof(AutoNetworkingTestFieldDeltaComponent.Field3));
        });

        await RunTicks();

        // Check the client again.
        await client.WaitPost(() =>
        {
            // Get the client-side components.
            Assert.That(client.EntMan.TryGetComponent(clientEnt1, out AutoNetworkingTestComponent? cmpClient1));
            Assert.That(client.EntMan.TryGetComponent(clientEnt2, out AutoNetworkingTestChildComponent? cmpClient2));
            Assert.That(client.EntMan.TryGetComponent(clientEnt3, out AutoNetworkingTestEmptyChildComponent? cmpClient3));
            Assert.That(client.EntMan.TryGetComponent(clientEnt4, out AutoNetworkingTestFieldDeltaComponent? cmpClient4));
            Assert.That(client.EntMan.TryGetComponent(clientEnt5, out AutoNetworkingTestFieldDeltaComponent? cmpClient5));
            Assert.That(client.EntMan.TryGetComponent(clientEnt6, out AutoNetworkingTestFieldDeltaComponent? cmpClient6));

            // Check that the networked datafields have changed.
            Assert.That(cmpClient1?.IsNetworked, Is.EqualTo(101));
            Assert.That(cmpClient1?.NotNetworked, Is.EqualTo(2)); // unchanged

            Assert.That(cmpClient2?.ChildNetworked, Is.EqualTo(101));
            Assert.That(cmpClient2?.Child, Is.EqualTo(2)); // unchanged
            Assert.That(cmpClient2?.ParentNetworked, Is.EqualTo(103));
            Assert.That(cmpClient2?.Parent, Is.EqualTo(4)); // unchanged

            Assert.That(cmpClient3?.ParentNetworked, Is.EqualTo(103));
            Assert.That(cmpClient3?.Parent, Is.EqualTo(4)); // unchanged

            Assert.That(cmpClient4?.Field1, Is.EqualTo(1)); // not dirtied
            Assert.That(cmpClient4?.Field2, Is.EqualTo(2)); // not networked
            Assert.That(cmpClient4?.Field3, Is.EqualTo(103)); // changed from delta state

            Assert.That(cmpClient5?.Field1, Is.EqualTo(101)); // changed from full state
            Assert.That(cmpClient5?.Field2, Is.EqualTo(2)); // not networked
            Assert.That(cmpClient5?.Field3, Is.EqualTo(103)); // changed from full state

            Assert.That(cmpClient6?.Field1, Is.EqualTo(101)); // changed from full state
            Assert.That(cmpClient6?.Field2, Is.EqualTo(2)); // not networked
            Assert.That(cmpClient6?.Field3, Is.EqualTo(103)); // changed from full state
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

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class AutoNetworkingTestFieldDeltaComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Field1 = 1;

    [DataField] // Add one field that is not networked to see if the next one get indexed correctly.
    public int Field2 = 2;

    [DataField, AutoNetworkedField]
    public int Field3 = 3;
}
