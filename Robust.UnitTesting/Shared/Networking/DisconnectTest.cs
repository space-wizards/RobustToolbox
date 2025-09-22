using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using cIPlayerManager = Robust.Client.Player.IPlayerManager;
using sIPlayerManager = Robust.Server.Player.IPlayerManager;

namespace Robust.UnitTesting.Shared.Networking;

public sealed class DisconnectTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that client disconnection works as expected. This is effectively a test of
    /// <see cref="RobustIntegrationTest.IntegrationNetManager"/>, not the main net manager.
    /// </summary>
    [Test]
    [TestOf(typeof(IntegrationNetManager))]
    public async Task TestConnectDisconnect()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var cNetMan = client.ResolveDependency<IClientNetManager>();
        var cPlayerMan = client.ResolveDependency<cIPlayerManager>();
        var sPlayerMan = server.ResolveDependency<sIPlayerManager>();

        ICommonSession session = default!;
        AssertDisconnected();

        // Connect client.
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() => cNetMan.ClientConnect(null!, 0, null!));
        await RunTicks();
        AssertConnected();

        // Disconnect the client
        cNetMan.ClientDisconnect("test");
        await RunTicks();
        AssertDisconnected();

        // Reconnect again
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() => cNetMan.ClientConnect(null!, 0, null!));
        await RunTicks();
        AssertConnected();

        // Disconnect again, but using the server-channel
        session.Channel.Disconnect("test 2");
        await RunTicks();
        AssertDisconnected();

        // Reconnect again
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() => cNetMan.ClientConnect(null!, 0, null!));
        await RunTicks();
        AssertConnected();

        void AssertConnected()
        {
            Assert.That(cNetMan.IsConnected, Is.True);
            Assert.That(sPlayerMan.Sessions.Count(), Is.EqualTo(1));

            session = sPlayerMan.Sessions.Single();
            Assert.That(session.Status, Is.EqualTo(SessionStatus.Connected));
            Assert.That(session.UserId, Is.EqualTo(cPlayerMan.LocalUser));
            Assert.That(cPlayerMan.LocalSession, Is.Not.Null);
        }

        void AssertDisconnected()
        {
            Assert.That(cNetMan.IsConnected, Is.False);
            Assert.That(sPlayerMan.Sessions.Count(), Is.EqualTo(0));
            if (session != null)
                Assert.That(session.Status, Is.EqualTo(SessionStatus.Disconnected));
        }

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }

        await client.WaitPost(() => cNetMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}

