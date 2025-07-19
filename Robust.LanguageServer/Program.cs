using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using Robust.Client.Utility;
using Robust.LanguageServer.Parsing;
using Robust.LanguageServer.Provider;
using Robust.Shared.IoC;
using Robust.Server;
using Robust.Shared.Utility;
using ELLanguageServer = EmmyLua.LanguageServer.Framework.Server.LanguageServer;

namespace Robust.LanguageServer;

internal static class Program
{
    static async Task Main(string[] args)
    {
        if (!CommandLineArgs.TryParse(args, out var cliArgs))
            return;

        var deps = IoCManager.InitThread();
        deps.Register<DocumentCache>();
        deps.Register<Loader>();
        deps.Register<DocsManager>();
        deps.Register<Parser>();

        deps.Register<DiagnosticProvider>();

        // deps.Register<LanguageServerContext>();
        ServerIoC.RegisterIoC(deps);
        // ClientIoC.RegisterIoC(GameController.DisplayMode.Headless, deps);

        var ls = CreateLanguageServer(cliArgs);
        // var ls = CreateTcpLanguageServer();
        // var ls = CreateStdOutLanguageServer();
        // var ls = CreateNamedPipeLanguageServer(cliArgs);
        var ctx = new LanguageServerContext(ls);

        deps.RegisterInstance<ELLanguageServer>(ls);
        deps.RegisterInstance<LanguageServerContext>(ctx);

        deps.BuildGraph();

        ctx.Initialize();

        await ctx.Run();
    }

    private static ELLanguageServer CreateLanguageServer(CommandLineArgs args)
    {
        if (args.Mode == CommandLineArgs.Transport.Tcp)
            return CreateTcpLanguageServer(args.Port);

        if (args.Mode == CommandLineArgs.Transport.Pipe)
        {
            if (args.CommunicationPipe is not {} pipe)
                throw new Exception("Missing pipe");

            return CreateNamedPipeLanguageServer(pipe);
        }

        return CreateStdOutLanguageServer();
    }

    private static ELLanguageServer CreateNamedPipeLanguageServer(string pipe)
    {
        Console.Error.WriteLine("Communicating using pipe: {0}", pipe);

        var stream = new NamedPipeClientStream(pipe);
        stream.Connect();

        Console.Error.WriteLine("Pipe connected");

        return ELLanguageServer.From(stream, stream);
    }

    private static ELLanguageServer CreateStdOutLanguageServer()
    {
        Console.Error.WriteLine("Communicating using standard in/out");

        Stream inputStream = Console.OpenStandardInput();
        Stream outputStream = Console.OpenStandardOutput();

        return ELLanguageServer.From(inputStream, outputStream);
    }

    private static ELLanguageServer CreateTcpLanguageServer(int port)
    {
        var tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ipAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
        EndPoint endPoint = new IPEndPoint(ipAddress, port);

        tcpServer.Bind(endPoint);
        Console.Error.WriteLine($"Listening on port {port}.");
        tcpServer.Listen(1);

        var languageClientSocket = tcpServer.Accept();
        var networkStream = new NetworkStream(languageClientSocket);
        var input = networkStream;
        var output = networkStream;

        return ELLanguageServer.From(input, output);
    }
}
