using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Server;
using Robust.LanguageServer;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

var server = await LanguageServer.From(options => options
    .WithInput(Console.OpenStandardInput())
    .WithOutput(Console.OpenStandardOutput())
    .ConfigureLogging(x => x
        .AddLanguageProtocolLogging()
        .SetMinimumLevel(LogLevel.Debug))
    .WithServices(x => x.AddSingleton<TextDocumentsCache>())
    .AddHandler<DocumentSymbolHandler>()
    .AddHandler<SemanticTokensHandler>()
    .AddHandler(x => x.GetRequiredService<TextDocumentsCache>())
    .WithServices(x => x.AddLogging(l => l.SetMinimumLevel(LogLevel.Trace)))
    .WithServices(InitIoC)
    .OnInitialize((languageServer, request, token) =>
    {
        languageServer.LogInfo("Initialize");

        return Task.CompletedTask;
    })
    .OnInitialized((languageServer, request, response, token) =>
    {
        languageServer.LogInfo("Initialized!");

        return Task.CompletedTask;
    })
    .OnStarted((languageServer, token) =>
    {
        languageServer.LogInfo("started");

        return Task.CompletedTask;
    }));

await server.WaitForExit;

void InitIoC(IServiceCollection services)
{
    var deps = new ServiceDependencyCollection(services);

    SharedIoC.RegisterIoC(deps, false);

    deps.Register<IReflectionManager, LangServerReflectionManager>();
    deps.Register<IGameTiming, GameTiming>();
    deps.Register<IResourceManager, ResourceManager>();
    deps.Register<IResourceManagerInternal, ResourceManager>();
    deps.Register<IPrototypeManager, PrototypeManager>();
    deps.Register<IAuthManager, AuthManager>();
    deps.Register<IComponentFactory, ComponentFactory>();

    // TODO: Get rid of entity dependencies.
    deps.Register<IEntityManager, EntityManager>();
    deps.Register<IMapManager, MapManager>();
    deps.Register<IEntitySystemManager, EntitySystemManager>();

    deps.BuildGraph();
}

