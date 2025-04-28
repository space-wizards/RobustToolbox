using System.Net;
using System.Net.Sockets;
using Robust.LanguageServer.Parsing;
using Robust.LanguageServer.Provider;
using Robust.Shared.IoC;
using Robust.Server;
using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;
namespace Robust.LanguageServer;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var deps = IoCManager.InitThread();
        IoCManager.Register<DocumentCache>();
        IoCManager.Register<Loader>();
        IoCManager.Register<Validator>();
        IoCManager.Register<DocsManager>();
        IoCManager.Register<Parser>();

        IoCManager.Register<DiagnosticProvider>();

        // IoCManager.Register<LanguageServerContext>();
        ServerIoC.RegisterIoC(deps);
        // ClientIoC.RegisterIoC(GameController.DisplayMode.Headless, deps);

        var ls = CreateTcpLanguageServer();
        var ctx = new LanguageServerContext(ls);

        IoCManager.RegisterInstance<ELLanguageServer>(ls);
        IoCManager.RegisterInstance<LanguageServerContext>(ctx);

        deps.BuildGraph();

        ctx.Initialize();

        await ctx.Run();
    }

    private static ELLanguageServer CreateTcpLanguageServer()
    {
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

        return ELLanguageServer.From(input, output);
    }
}
