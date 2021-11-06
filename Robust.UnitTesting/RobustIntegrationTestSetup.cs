using System.Collections.Concurrent;
using NUnit.Framework;
using static Robust.UnitTesting.RobustIntegrationTest;

[SetUpFixture]
// ReSharper disable once CheckNamespace
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
        string QueueToString(ConcurrentQueue<string> queue, int total)
        {
            return $"({queue.Count}/{total}):\n{string.Join("\n", queue)}\n\n";
        }

        var totalClients = ClientsPooled.Count + ClientsNotPooled.Count;
        TestContext.Out.WriteLine($"Clients created {QueueToString(ClientsCreated, totalClients)}");
        TestContext.Out.WriteLine($"Clients pooled {QueueToString(ClientsPooled, totalClients)}");
        TestContext.Out.WriteLine($"Clients not pooled {QueueToString(ClientsNotPooled, totalClients)}");

        var totalServers = ServersPooled.Count + ServersNotPooled.Count;
        TestContext.Out.WriteLine($"Servers created {QueueToString(ServersCreated, totalServers)}");
        TestContext.Out.WriteLine($"Servers pooled {QueueToString(ServersPooled, totalClients)}");
        TestContext.Out.WriteLine($"Servers not pooled {QueueToString(ServersNotPooled, totalClients)}");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Shutdown();
        PrintTestPoolingInfo();
    }
}

