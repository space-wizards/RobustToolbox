using System.Text.Json;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.ShowMessage;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Configuration;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Robust.LanguageServer.Handler;
using Robust.Shared.IoC;

namespace Robust.LanguageServer;
using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

public sealed class LanguageServerContext
{
    private ELLanguageServer _languageServer;

    public LanguageServerContext(ELLanguageServer languageServer)
    {
        var deps = IoCManager.Instance;
        if (deps == null)
            throw new NullReferenceException(nameof(deps));

        _languageServer = languageServer;

        _languageServer.OnInitialize((c, s) =>
        {
            IoCManager.InitThread(deps);

            // Here we should be trying to load data based on the client.rootUri
            Console.Error.WriteLine("Starting loader…");
            _languageServer.Client.ShowMessage(new() { Message = "Loading prototypes…", Type = MessageType.Info });
            deps.Resolve<Loader>().Init(deps);

            _languageServer.Client.ShowMessage(new() { Message = "Prototypes loaded.", Type = MessageType.Info });
            Console.Error.WriteLine("Loaded");

            var validator = IoCManager.Resolve<Validator>();
            validator.ValidateSingleFile(
                "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Reagents/medicine.yml");

            s.Name = "SS14 LSP";
            s.Version = "0.0.1";
            Console.Error.WriteLine("initialize");
            return Task.CompletedTask;
        });
        _languageServer.OnInitialized(async (c) =>
        {
            Console.Error.WriteLine("initialized");
            var config = await _languageServer.Client.GetConfiguration(new ConfigurationParams()
                {
                    Items =
                    [
                        new ConfigurationItem()
                        {
                            Section = "files"
                        }
                    ]
                },
                CancellationToken.None);

            Console.Error.WriteLine("Config:");
            foreach (var item in config)
            {
                Console.Error.WriteLine($"Item: {item.Value}");
                if (item.Value is JsonDocument doc)
                {
                    Console.Error.WriteLine($"Doc: {doc.RootElement.GetRawText()}");
                }
            }
        });

    }

    public void Initialize()
    {
        AddHandler(new TextDocumentHandler());
    }

    public Task Run()
    {
        return _languageServer.Run();
    }

    private void AddHandler(IJsonHandler handler)
    {
        _languageServer.AddHandler(IoCManager.InjectDependencies(handler));
    }
}
