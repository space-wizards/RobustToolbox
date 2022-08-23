using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared;
using Robust.Shared.Utility;
using C = System.Console;

namespace Robust.Server;

internal sealed class CommandLineArgs
{
    public MountOptions MountOptions { get; }
    public string? ConfigFile { get; }
    public string? DataDir { get; }
    public IReadOnlyCollection<(string key, string value)> CVars { get; }
    public IReadOnlyCollection<(string key, string value)> LogLevels { get; }
    public IReadOnlyList<string> ExecCommands { get; set; }

    // Manual parser because C# has no good command line parsing libraries. Also dependencies bad.
    // Also I don't like spending 100ms parsing command line args. Do you?
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsed)
    {
        parsed = null;
        string? configFile = null;
        string? dataDir = null;
        var cvars = new List<(string, string)>();
        var logLevels = new List<(string, string)>();
        var mountOptions = new MountOptions();
        var execCommands = new List<string>();

        using var enumerator = args.GetEnumerator();

        while (enumerator.MoveNext())
        {
            var arg = enumerator.Current;
            if (arg == "--config-file")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Missing config file.");
                    return false;
                }

                configFile = enumerator.Current;
            }
            else if (arg == "--data-dir")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Missing data directory.");
                    return false;
                }

                dataDir = enumerator.Current;
            }
            else if (arg == "--cvar")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Missing cvar value.");
                    return false;
                }

                var cvar = enumerator.Current;
                DebugTools.AssertNotNull(cvar);
                var pos = cvar.IndexOf('=');

                if (pos == -1)
                {
                    C.WriteLine("Expected = in cvar.");
                    return false;
                }

                cvars.Add((cvar[..pos], cvar[(pos + 1)..]));
            }
            else if (arg == "--help")
            {
                PrintHelp();
                return false;
            }
            else if (arg == "--mount-zip")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Missing mount path");
                    return false;
                }

                mountOptions.ZipMounts.Add(enumerator.Current);
            }
            else if (arg == "--mount-dir")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Missing mount path");
                    return false;
                }

                mountOptions.DirMounts.Add(enumerator.Current);
            }
            else if (arg == "--loglevel")
            {
                if (!enumerator.MoveNext())
                {
                    C.WriteLine("Missing loglevel sawmill.");
                    return false;
                }

                var loglevel = enumerator.Current;
                DebugTools.AssertNotNull(loglevel);
                var pos = loglevel.IndexOf('=');

                if (pos == -1)
                {
                    C.WriteLine("Expected = in loglevel.");
                    return false;
                }

                logLevels.Add((loglevel[..pos], loglevel[(pos + 1)..]));
            }
            else if (arg.StartsWith("+"))
            {
                execCommands.Add(arg[1..]);
            }
            else
            {
                C.WriteLine("Unknown argument: {0}", arg);
            }
        }

        parsed = new CommandLineArgs(configFile, dataDir, cvars, logLevels, mountOptions, execCommands);
        return true;
    }

    private static void PrintHelp()
    {
        C.WriteLine(@"
Usage: Robust.Server [options] [+command [+command]]

Options:
  --config-file     Path to the config file to read from.
  --data-dir        Path to the data directory to read/write from/to.
  --cvar            Specifies an additional cvar overriding the config file. Syntax is <key>=<value>
  --loglevel        Specifies an additional sawmill log level overriding the default values. Syntax is <key>=<value>
  --mount-dir       Resource directory to mount.
  --mount-zip       Resource zip to mount.
  --help            Display this help text and exit.

+command:             You can pass a set of commands, prefixed by +,
                      to be executed in the console in order after the game has finished initializing.
");
    }

    private CommandLineArgs(
        string? configFile,
        string? dataDir,
        IReadOnlyCollection<(string, string)> cVars,
        IReadOnlyCollection<(string, string)> logLevels,
        MountOptions mountOptions,
        IReadOnlyList<string> execCommands)
    {
        ConfigFile = configFile;
        DataDir = dataDir;
        CVars = cVars;
        LogLevels = logLevels;
        MountOptions = mountOptions;
        ExecCommands = execCommands;
    }
}
