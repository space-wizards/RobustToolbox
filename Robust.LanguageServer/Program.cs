using System.IO.Pipes;
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
using Serilog;

if (!CommandLineArgs.TryParse(args, out var cliArgs))
    return 1;

Stream outStream;
Stream inStream;

if (cliArgs.CommunicationPipe is { } pipe)
{
    // Using pipe means we can log to stdout without ruining everything.
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .MinimumLevel.Verbose()
        .CreateLogger();

    Log.Debug("Communicating using pipe: {PipeName}", pipe);

    var stream = new NamedPipeServerStream(
        pipe,
        PipeDirection.InOut,
        1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous);

    outStream = stream;
    inStream = stream;

    stream.WaitForConnection();

    Log.Debug("Received pipe connection!");
}
else
{
    outStream = Console.OpenStandardOutput();
    inStream = Console.OpenStandardInput();
}

var server = await LanguageServer.From(options => options
    .WithInput(inStream)
    .WithOutput(outStream)
    .ConfigureLogging(x => x
        .AddLanguageProtocolLogging()
        .AddSerilog()
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
        var prototypes = languageServer.Services.GetRequiredService<IPrototypeManager>();

        const string prototypeDir = @"C:\Users\Pieter-Jan Briers\Projects\ss14\space-station-14\Resources\Prototypes";

        languageServer.LogInfo("started");

        return Task.CompletedTask;
    }));

await server.WaitForExit;

return 0;

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
sealed class FlushWrapStream : Stream
{
    private readonly Stream _baseStream;

    public FlushWrapStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override void Flush()
    {
        _baseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _baseStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _baseStream.Write(buffer, offset, count);
        _baseStream.Flush();
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
