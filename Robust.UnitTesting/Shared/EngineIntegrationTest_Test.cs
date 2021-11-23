using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.UnitTesting.Shared
{
    [TestFixture]
    public class EngineIntegrationTest_Test : RobustIntegrationTest
    {
        [Test]
        public void ServerStartsCorrectlyTest()
        {
            ServerIntegrationInstance? server = null;
            Assert.DoesNotThrow(() => server = StartServer());
            Assert.That(server, Is.Not.Null);
        }

        [Test]
        public void ClientStartsCorrectlyTest()
        {
            ClientIntegrationInstance? client = null;
            Assert.DoesNotThrow(() => client = StartClient());
            Assert.That(client, Is.Not.Null);
        }

        [Test]
        public async Task ServerClientPairConnectCorrectlyTest()
        {
            var server = StartServer();
            var client = StartClient();

            Assert.That(server, Is.Not.Null);
            Assert.That(client, Is.Not.Null);

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            // Connect client to the server...
            Assert.DoesNotThrow(() => client.SetConnectTarget(server));
            client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));

            // Run 10 synced ticks...
            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }

            await server.WaitAssertion(() =>
            {
                var playerManager = IoCManager.Resolve<IPlayerManager>();

                // There must be a player connected.
                Assert.That(playerManager.PlayerCount, Is.EqualTo(1));

                // Get the only player...
                var player = playerManager.Sessions.Single();

                Assert.That(player.Status, Is.EqualTo(SessionStatus.Connected));
                Assert.That(player.ConnectedClient.IsConnected, Is.True);
            });

            await client.WaitAssertion(() =>
            {
                var netManager = IoCManager.Resolve<IClientNetManager>();

                Assert.That(netManager.IsConnected, Is.True);
                Assert.That(netManager.ServerChannel, Is.Not.Null);
                Assert.That(netManager.ServerChannel!.IsConnected, Is.True);
            });
        }
    }
}
