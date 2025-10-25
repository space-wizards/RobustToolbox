using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Shared.EntitySerialization;

public sealed partial class WeakEntityReferenceTest : RobustIntegrationTest
{
    [Test]
    public async Task TestWeakEntityReference()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(server.WaitIdleAsync(), client.WaitIdleAsync());

        var sEntMan = server.EntMan;
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var cEntMan = client.EntMan;
        var cNetMan = client.ResolveDependency<IClientNetManager>();

        NetEntity netEntA = default;
        NetEntity netEntB = default;

        // Set up entities
        await server.WaitPost(() =>
        {
            var entA = sEntMan.Spawn();
            var entB = sEntMan.Spawn();
            netEntA = sEntMan.GetNetEntity(entA);
            netEntB = sEntMan.GetNetEntity(entB);

            // Give A weak references to B
            var comp = sEntMan.AddComponent<WeakEntityReferenceTestComponent>(entA);
            comp.Entity = entB;
            comp.EntityList = [entB];
            comp.EntitySet = [entB];
            sEntMan.Dirty(entA, comp);
        });

        // Connect client.
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() => cNetMan.ClientConnect(null!, 0, null!));
        // Disable PVS so everything gets networked
        server.Post(() => server.CfgMan.SetCVar(CVars.NetPVS, false));

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }
        await RunTicks();

        // Put the player into the game so they get entity data
        await server.WaitAssertion(() =>
        {
            var session = sPlayerMan.Sessions.First();
            sPlayerMan.JoinGame(session);
        });

        await RunTicks();

        // Make sure the client got entity data
        await client.WaitAssertion(() =>
        {
            Assert.That(cNetMan.IsConnected);
            Assert.That(cEntMan.TryGetEntity(netEntA, out var entA));
            Assert.That(cEntMan.TryGetEntity(netEntB, out var entB));

            Assert.That(cEntMan.TryGetComponent<WeakEntityReferenceTestComponent>(entA, out var comp));
            Assert.That(cEntMan.TryGetEntity(comp!.Entity, out var referencedEnt));
            Assert.That(referencedEnt, Is.EqualTo(entB));
            Assert.That(comp.EntityList, Contains.Item((WeakEntityReference) entB!.Value));
            Assert.That(comp.EntitySet, Contains.Item((WeakEntityReference) entB.Value));
        });

        // Delete the referenced entity on the server
        // Make sure that the sending the new state does not cause errors
        await server.WaitAssertion(() =>
        {
            var entA = sEntMan.GetEntity(netEntA);
            var comp = sEntMan.GetComponent<WeakEntityReferenceTestComponent>(entA);
            var entB = sEntMan.GetEntity(netEntB);
            sEntMan.DeleteEntity(entB);
            sEntMan.Dirty(entA, comp);
        });

        await RunTicks();

        // Disconnect client
        await client.WaitPost(() => cNetMan.ClientDisconnect(string.Empty));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);

        // Reset cvar
        // I love engine tests
        server.Post(() => server.CfgMan.SetCVar(CVars.NetPVS, true));
    }
}
