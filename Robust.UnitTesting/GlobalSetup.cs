using NUnit.Framework;

namespace Robust.UnitTesting
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeTearDown]
        public void TearDown()
        {
            foreach (var client in RobustIntegrationTest.ClientsReady)
            {
                client.Dispose();
            }

            RobustIntegrationTest.ClientsReady.Clear();

            foreach (var server in RobustIntegrationTest.ServersReady)
            {
                server.Dispose();
            }

            RobustIntegrationTest.ServersReady.Clear();
        }
    }
}
