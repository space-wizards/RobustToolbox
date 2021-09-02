using NUnit.Framework;

namespace Robust.UnitTesting
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var server in RobustIntegrationTest.ServersReady)
            {
                server.Dispose();
            }

            RobustIntegrationTest.ServersReady.Clear();
        }
    }
}
