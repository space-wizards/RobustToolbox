using System.Text.Json;
using System.Text.Json.Serialization;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.ShowMessage;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Configuration;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Progress;
using EmmyLua.LanguageServer.Framework.Protocol.Model.WorkDoneProgress;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.LanguageServer.Handler;
using Robust.LanguageServer.Notifications;
using Robust.LanguageServer.Parsing;
using Robust.Shared.IoC;

namespace Robust.LanguageServer;

using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

public sealed class LanguageServerContext
{
    [Dependency] private readonly DocumentCache _cache = null!;

    private readonly ELLanguageServer _languageServer;

    public Uri? RootDirectory { get; private set; }

    private bool _initialized = false;

    public LanguageServerContext(ELLanguageServer languageServer)
    {
        var deps = IoCManager.Instance;
        if (deps == null)
            throw new NullReferenceException(nameof(deps));

        _languageServer = languageServer;
        _languageServer.AddJsonSerializeContext(JsonGenerateContext.Default);

        _languageServer.OnInitialize((c, s) =>
        {
            IoCManager.InitThread(deps);

            if (c.RootUri is { } rootUri)
                RootDirectory = rootUri.Uri;

            s.Name = "SS14 LSP";
            s.Version = "0.0.1";
            Console.Error.WriteLine("initialize");
            return Task.CompletedTask;
        });

        _languageServer.OnInitialized(async (c) =>
        {
            Console.Error.WriteLine("initialized");

            // Here we should be trying to load data based on the client.rootUri
            Console.Error.WriteLine("Starting loader…");

            ShowProgress("Loading Prototypes…");

            deps.Resolve<Loader>().Init(deps);

            HideProgress();

            Console.Error.WriteLine("Loaded");
        });
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        AddHandler(new TextDocumentHandler());
        AddHandler(new SemanticTokensHandler());
        AddHandler(new DocumentColorHandler());
        AddHandler(new HoverHandler());
        AddHandler(new DocumentSymbolHandler());

        _cache.DocumentChanged += (uri, version) => { Console.Error.WriteLine($"Document changed! Uri: {uri} ({version})"); };
    }

    public Task Run()
    {
        Initialize();

        return _languageServer.Run();
    }

    private void AddHandler(IJsonHandler handler)
    {
        _languageServer.AddHandler(IoCManager.InjectDependencies(handler));
    }

    private void ShowProgress(string text)
    {
        _languageServer.SendNotification(new("rt/showProgress",
                JsonSerializer.SerializeToDocument(
                    new ProgressInfo()
                    {
                        Text = text,
                    },
                    _languageServer.JsonSerializerOptions)
            ))
            .Wait();
    }

    private void HideProgress()
    {
        _languageServer.SendNotification(new("rt/hideProgress", null)).Wait();
    }
}
