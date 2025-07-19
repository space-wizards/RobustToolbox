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
using Robust.Shared.Log;

namespace Robust.LanguageServer;

using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

public sealed class LanguageServerContext
{
    [Dependency] private readonly DocumentCache _cache = null!;

    private ISawmill _logger = default!;
    private readonly ELLanguageServer _languageServer;

    public Uri? RootDirectory { get; private set; }

    private bool _initialized = false;

    public LanguageServerContext(ELLanguageServer languageServer)
    {
        _languageServer = languageServer;
        _languageServer.AddJsonSerializeContext(JsonGenerateContext.Default);
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        _logger = Logger.GetSawmill("LanguageServerContext");

        InitializeLanguageServer();

        AddHandler(new TextDocumentHandler());
        AddHandler(new SemanticTokensHandler());
        AddHandler(new DocumentColorHandler());
        AddHandler(new HoverHandler());
        AddHandler(new DocumentSymbolHandler());
        AddHandler(new DefinitionHandler());

        _cache.DocumentChanged += (uri, version) => { _logger.Error($"Document changed! Uri: {uri} ({version})"); };
    }

    private void InitializeLanguageServer()
    {
        var deps = IoCManager.Instance;
        if (deps == null)
            throw new NullReferenceException(nameof(deps));

        _languageServer.OnInitialize((c, s) =>
        {
            IoCManager.InitThread(deps);

            if (c.RootUri is { } rootUri)
                RootDirectory = rootUri.Uri;

            s.Name = "SS14 LSP";
            s.Version = "0.0.1";
            _logger.Error("initialize");
            return Task.CompletedTask;
        });

        _languageServer.OnInitialized(async (c) =>
        {
            try
            {
                _logger.Error("initialized");

                // Here we should be trying to load data based on the client.rootUri
                _logger.Error("Starting loader…");

                ShowProgress("Loading Prototypes…");

                deps.Resolve<Loader>().Init(deps);

                HideProgress();

                _logger.Error("Loaded");
            }
            catch (Exception e)
            {
                _logger.Error($"Error while starting: {e}");
                _languageServer.Client.ShowMessage(new()
                    {
                        Message = $"Error during startup: {e.Message}",
                        Type = MessageType.Error,
                    })
                    .Wait();
                Environment.Exit(1);
            }
        });
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
