using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.GameObjects;

public sealed partial class SingletonEntitySystem_Tests : RobustIntegrationTest
{
    // It would be nice to have reflection disable for this class, but if we do
    // then MapLoaderSystem won't be able to find it when saving.
    //[Reflect(false)]
    [RegisterComponent]
    [NetworkedComponent]
    private sealed partial class SingletonEntitySystemTestComponent : Component
    {
        [DataField]
        public int IntValue;
    }

    [Reflect(false)]
    private sealed class SingletonEntitySystemTestSystem : SingletonEntitySystem<SingletonEntitySystemTestComponent>
    {

        protected override void OnComponentInit(Entity<SingletonEntitySystemTestComponent> entity, ref ComponentInit args)
        {
            base.OnComponentInit(entity, ref args);
        }

        public void SetIntValue(int value)
        {
            if (!TryGetInstance(out var instance))
                return;

            instance.Value.Comp.IntValue = value;
            Dirty(instance.Value);
        }

        public bool TryGetIntValue(out int? value)
        {
            value = null;
            if (!TryGetInstance(out var instance))
                return false;

            value = instance.Value.Comp.IntValue;
            return true;
        }
    }

    private const int TestIntValue = 5;

    [Test]
    public async Task BasicTest()
    {
        // Add our test classes to the server
        var serverOptions = new ServerIntegrationOptions();
        /* serverOptions.BeforeRegisterComponents += () =>
        {
            var compFact = IoCManager.Resolve<IComponentFactory>();
            compFact.RegisterClass<SingletonEntitySystemTestComponent>();
        }; */
        serverOptions.BeforeStart += () =>
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            sysMan.LoadExtraSystemType<SingletonEntitySystemTestSystem>();
        };

        // Add our test classes to the client
        var clientOptions = new ClientIntegrationOptions();
        /* clientOptions.BeforeRegisterComponents += () =>
        {
            var compFact = IoCManager.Resolve<IComponentFactory>();
            compFact.RegisterClass<SingletonEntitySystemTestComponent>();
        }; */
        clientOptions.BeforeStart += () =>
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            sysMan.LoadExtraSystemType<SingletonEntitySystemTestSystem>();
        };

        // Start server and client, wait until they're ready
        var server = StartServer(serverOptions);
        var client = StartClient(clientOptions);
        await Task.WhenAll(server.WaitIdleAsync(), client.WaitIdleAsync());

        var confMan = server.ResolveDependency<IConfigurationManager>();
        await server.WaitPost(() => confMan.SetCVar(CVars.NetPVS, false));

        // Connect client to the server
        Assert.DoesNotThrow(() => client.SetConnectTarget(server), "Exception while setting client connect target");
        var cNetMan = client.ResolveDependency<IClientNetManager>();
        await client.WaitPost(() => cNetMan.ClientConnect(null!, 0, null!));

        // Wait for the connection
        await RunTicks();
        Assert.That(cNetMan.IsConnected, Is.True, "Client did not connect to server.");

        // Have the client join the game so they get entity data
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var session = playerMan.Sessions.First();
        await server.WaitPost(() => playerMan.JoinGame(session));

        var sEntMan = server.EntMan;
        var cEntMan = client.EntMan;
        var sTestSys = server.System<SingletonEntitySystemTestSystem>();
        var cTestSys = client.System<SingletonEntitySystemTestSystem>();

        await client.WaitAssertion(() =>
        {
            // Client should not spawn the instance, it gets the entity sent from the server
            Assert.That(!cTestSys.TryGetInstance(out _), "Client found singleton instance before it was sent from server.");
        });

        await server.WaitAssertion(() =>
        {
            // Access the system on the server, forcing the instance to be spawned
            sTestSys.SetIntValue(TestIntValue);
            // Make sure that it worked
            Assert.That(sTestSys.TryGetInstance(out _), "Singleton instance was not created.");
            Assert.That(sTestSys.TryGetIntValue(out var intValue) && intValue == TestIntValue, "Singleton instance value was not assigned.");
        });

        // Wait for entity data to sync to the client
        await RunTicks();

        await client.WaitAssertion(() =>
        {
            // The client should have the instance now
            Assert.That(cTestSys.TryGetIntValue(out var intValue), "Client did not receive singleton instance.");
        });

        // Make sure that client and server agree there is exactly one entity
        Assert.That(sEntMan.EntityCount, Is.EqualTo(1), "Extra entities found on server.");
        Assert.That(cEntMan.EntityCount, Is.EqualTo(1), "Extra entities found on client.");

        var path = new ResPath($"{nameof(SingletonEntitySystem_Tests)}.yml");
        var loader = server.System<MapLoaderSystem>();

        // Save world data
        var ents = sEntMan.GetEntities().ToHashSet();
        Assert.That(loader.TrySaveGeneric(ents, path, out _), "Failed to save entity data.");

        // Delete all entities
        await server.WaitPost(() =>
        {
            foreach (var ent in ents)
            {
                sEntMan.DeleteEntity(ent);
            }
        });

        // Make sure there are no entities on the server
        Assert.That(sEntMan.EntityCount, Is.Zero, "Not all entities were deleted on server.");
        Assert.That(sTestSys.IsReady, Is.False, "Server system did not clear its cached entity reference when deleted.");

        // Wait for the client to receive updates
        await RunTicks();
        Assert.That(cEntMan.EntityCount, Is.Zero, "Not all entities were deleted on client.");
        Assert.That(cTestSys.IsReady, Is.False, "Client system did not clear its cached entity reference when deleted.");

        await server.WaitAssertion(() =>
        {
            Assert.That(loader.TryLoadGeneric(path, out var result), "Failed to load entity data.");
        });

        // Make sure the entity is back
        Assert.That(sEntMan.EntityCount, Is.EqualTo(1), "Invalid entity data loaded on server.");
        Assert.That(sTestSys.IsReady, "Server system did not restore cached entity reference.");
        Assert.That(sTestSys.TryGetIntValue(out var sIntValue) && sIntValue == TestIntValue, "Server system did not restore component data.");

        await RunTicks();

        Assert.That(cEntMan.EntityCount, Is.EqualTo(1), "Invalid entity data found on client.");
        Assert.That(cTestSys.IsReady, "Client system did not restore cached entity reference.");
        Assert.That(sTestSys.TryGetIntValue(out var cIntValue) && cIntValue == TestIntValue, "Client system did not receive component data.");

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }
    }
}
