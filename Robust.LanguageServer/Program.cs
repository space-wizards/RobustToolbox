using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Robust.Shared.IoC;
using EmmyLua.LanguageServer.Framework;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.ShowMessage;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Configuration;
using Robust.Server;
using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;
namespace Robust.LanguageServer;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var deps = IoCManager.InitThread();
        IoCManager.Register<Loader>();
        IoCManager.Register<Validator>();
        IoCManager.Register<LanguageServer>();
        ServerIoC.RegisterIoC(deps);
        // ClientIoC.RegisterIoC(GameController.DisplayMode.Headless, deps);
        deps.BuildGraph();

        var port = 8182;
        var tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        EndPoint endPoint = new IPEndPoint(ipAddress, port);

        tcpServer.Bind(endPoint);
        Console.WriteLine($"Listening on port {port}.");
        tcpServer.Listen(1);

        var languageClientSocket = tcpServer.Accept();
        var networkStream = new NetworkStream(languageClientSocket);
        var input = networkStream;
        var output = networkStream;

        var ls = ELLanguageServer.From(input, output);
        ls.OnInitialize((c, s) =>
        {
            IoCManager.InitThread(deps);

            // Here we should be trying to load data based on the client.rootUri
            Console.Error.WriteLine("Starting loader…");
            ls.Client.ShowMessage(new() { Message = "Loading prototypes…", Type = MessageType.Info });
            deps.Resolve<Loader>().Init(deps);

            ls.Client.ShowMessage(new() { Message = "Prototypes loaded.", Type = MessageType.Info });
            Console.Error.WriteLine("Loaded");

            var validator = IoCManager.Resolve<Validator>();
            validator.ValidateSingleFile(
                "/Users/ciaran/code/ss14/space-station-14/Resources/Prototypes/Reagents/medicine.yml");

            s.Name = "SS14 LSP";
            s.Version = "0.0.1";
            Console.Error.WriteLine("initialize");
            return Task.CompletedTask;
        });
        ls.OnInitialized(async (c) =>
        {
            Console.Error.WriteLine("initialized");
            var config = await ls.Client.GetConfiguration(new ConfigurationParams()
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

        // var textDocumentHandler = new TextDocumentHandler(ls);
        // IoCManager.InjectDependencies(textDocumentHandler);
        //
        // Console.Error.WriteLine($"textDocumentHandler Timing: {textDocumentHandler._protoMan}");
        //
        // ls.AddHandler(textDocumentHandler);

        await ls.Run();
    }
}
