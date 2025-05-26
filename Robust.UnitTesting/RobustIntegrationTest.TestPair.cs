using System.Threading.Tasks;
using Robust.Client;
using Robust.Server;
using Robust.Shared.Log;
using Robust.UnitTesting.Pool;

namespace Robust.UnitTesting;

public partial class RobustIntegrationTest
{
    /// <summary>
    /// <see cref="TestPair{TServer,TClient}"/> implementation using <see cref="RobustIntegrationTest"/> instances.
    /// </summary>
    [Virtual]
    public class TestPair : TestPair<ServerIntegrationInstance, ClientIntegrationInstance>
    {
        protected override async Task<ClientIntegrationInstance> GenerateClient()
        {
            var client = new ClientIntegrationInstance(ClientOptions());
            await client.WaitIdleAsync();
            client.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            client.CfgMan.OnValueChanged(RTCVars.FailureLogLevel, value => ClientLogHandler.FailureLevel = value, true);
            await client.WaitIdleAsync();
            return client;
        }

        protected override async Task<ServerIntegrationInstance> GenerateServer()
        {
            var server = new ServerIntegrationInstance(ServerOptions());
            await server.WaitIdleAsync();
            server.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
            server.CfgMan.OnValueChanged(RTCVars.FailureLogLevel, value => ServerLogHandler.FailureLevel = value, true);
            return server;
        }

        protected virtual ClientIntegrationOptions ClientOptions()
        {
            var options = new ClientIntegrationOptions
            {
                ContentAssemblies = Manager.ClientAssemblies,
                OverrideLogHandler = () => ClientLogHandler
            };

            options.Options = new()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = false,
            };

            foreach (var (cvar, value) in Manager.DefaultCvars)
            {
                options.CVarOverrides[cvar] = value;
            }

            return options;
        }

        protected virtual ServerIntegrationOptions ServerOptions()
        {
            var options = new ServerIntegrationOptions
            {
                ContentAssemblies = Manager.ServerAssemblies,
                OverrideLogHandler = () => ServerLogHandler
            };

            options.Options = new()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = false,
            };

            foreach (var (cvar, value) in Manager.DefaultCvars)
            {
                options.CVarOverrides[cvar] = value;
            }

            return options;
        }
    }
}
