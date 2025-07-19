using System.Diagnostics.CodeAnalysis;

namespace Robust.LanguageServer;

internal sealed record CommandLineArgs(CommandLineArgs.Transport Mode, int Port, string? CommunicationPipe)
{
    internal enum Transport
    {
        StandardInOut,
        Tcp,
        Pipe,
    }

    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsedArgs)
    {
        int port = 8182;
        Transport mode = Transport.StandardInOut;
        string? commPipe = null;
        parsedArgs = null;

        using var enumerator = args.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var arg = enumerator.Current;

            if (arg.StartsWith("--pipe="))
            {
                var pipe = arg.Substring("--pipe=".Length);
                commPipe = pipe;
                mode = Transport.Pipe;
            }
            else if (arg.StartsWith("--port="))
            {
                port = int.Parse(arg.Substring("--port=".Length));
                mode = Transport.Tcp;
            }
            else
            {
                Console.Error.WriteLine("Unknown argument");
                return false;
            }
        }

        parsedArgs = new CommandLineArgs(mode, port, commPipe);
        return true;
    }
}
