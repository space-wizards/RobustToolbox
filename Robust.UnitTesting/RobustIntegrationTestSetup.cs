using System.Collections.Concurrent;
using NUnit.Framework;
using static Robust.UnitTesting.RobustIntegrationTest;

[SetUpFixture]
public class RobustIntegrationTestSetup
{
    public void Shutdown()
    {
        foreach (var client in ClientsReady)
        {
            client.Dispose();
        }

        ClientsReady.Clear();

        foreach (var server in ServersReady)
        {
            server.Dispose();
        }

        ServersReady.Clear();
    }

    public void PrintTestPoolingInfo()
    {
        string QueueToString(ConcurrentQueue<string> queue)
        {
            return $"({queue.Count}):\n{string.Join("\n", queue)}\n\n";
        }

        TestContext.Out.WriteLine($"Clients created {QueueToString(ClientsCreated)}");
        TestContext.Out.WriteLine($"Clients pooled {QueueToString(ClientsPooled)}");
        TestContext.Out.WriteLine($"Clients not pooled {QueueToString(ClientsNotPooled)}");

        TestContext.Out.WriteLine($"Servers created {QueueToString(ServersCreated)}");
        TestContext.Out.WriteLine($"Servers pooled {QueueToString(ServersPooled)}");
        TestContext.Out.WriteLine($"Servers not pooled {QueueToString(ServersNotPooled)}");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Shutdown();
        PrintTestPoolingInfo();
    }
}

