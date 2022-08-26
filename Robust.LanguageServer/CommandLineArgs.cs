using System.Diagnostics.CodeAnalysis;
using C = System.Console;

namespace Robust.LanguageServer;

internal sealed record CommandLineArgs(string? CommunicationPipe)
{
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsedArgs)
    {
        string? commPipe = null;
        parsedArgs = null;

        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = args.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var arg = enumerator.Current;
            if (arg == "--pipe")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Expected pipe name");
                    return false;
                }

                commPipe = enumerator.Current;
            }
            else
            {
                C.WriteLine("Unknown argument");
                return false;
            }
        }

        parsedArgs = new CommandLineArgs(commPipe);
        return true;
    }
}
