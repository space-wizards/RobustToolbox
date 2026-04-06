using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.IntegrationTests.Configuration;
using Robust.Shared.Log;

namespace Robust.UnitTesting.Shared.Configuration;

[Parallelizable(ParallelScope.All)]
[TestFixture]
[TestOf(typeof(ConfigurationManagerTest))]
internal sealed class ConfigurationIntegrationTest : RobustIntegrationTest
{
    [Test]
    public async Task TestSaveNoWarningServer()
    {
        using var server = StartServer(new ServerIntegrationOptions
        {
            FailureLogLevel = LogLevel.Warning
        });
        await server.WaitPost(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            var cfg = server.Resolve<IConfigurationManager>();
            cfg.SaveToFile();
        });
    }

    [Test]
    public async Task TestSaveNoWarningClient()
    {
        using var server = StartClient(new ClientIntegrationOptions
        {
            FailureLogLevel = LogLevel.Warning
        });
        await server.WaitPost(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            var cfg = server.Resolve<IConfigurationManager>();
            cfg.SaveToFile();
        });
    }
}
