using System.Diagnostics.CodeAnalysis;

namespace Robust.LanguageServer;

internal sealed record CommandLineArgs(string? CommunicationPipe)
{
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsedArgs)
    {
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
            }
            else
            {
                Console.Error.WriteLine("Unknown argument");
                return false;
            }
        }

        parsedArgs = new CommandLineArgs(commPipe);
        return true;
    }
}
