using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared;
using Robust.Shared.Utility;
using C = System.Console;

namespace Robust.Client
{
    internal sealed class CommandLineArgs
    {
        public MountOptions MountOptions { get; }
        public bool Headless { get; }
        public bool SelfContained { get; }
        public bool Connect { get; }
        public string ConnectAddress { get; }
        public string? Ss14Address { get; }
        public bool Launcher { get; }
        public string? Username { get; }
        public IReadOnlyCollection<(string key, string value)> CVars { get; }
        public IReadOnlyCollection<(string key, string value)> LogLevels { get; }

        // Manual parser because C# has no good command line parsing libraries. Also dependencies bad.
        // Also I don't like spending 100ms parsing command line args. Do you?
        public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsed)
        {
            parsed = null;
            var headless = false;
            var selfContained = false;
            var connect = false;
            var connectAddress = "localhost";
            string? ss14Address = null;
            var launcher = false;
            string? username = null;
            var cvars = new List<(string, string)>();
            var logLevels = new List<(string, string)>();
            var mountOptions = new MountOptions();

            using var enumerator = args.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var arg = enumerator.Current;
                if (arg == "--connect")
                {
                    connect = true;
                }
                else if (arg == "--connect-address")
                {
                    if (!enumerator.MoveNext())
                    {
                        C.WriteLine("Missing connection address.");
                        return false;
                    }

                    connectAddress = enumerator.Current;
                }
                else if (arg == "--ss14-address")
                {
                    if (!enumerator.MoveNext())
                    {
                        C.WriteLine("Missing SS14 address.");
                        return false;
                    }

                    ss14Address = enumerator.Current;
                }
                else if (arg == "--self-contained")
                {
                    selfContained = true;
                }
                else if (arg == "--launcher")
                {
                    launcher = true;
                }
                else if (arg == "--headless")
                {
                    headless = true;
                }
                else if (arg == "--username")
                {
                    if (!enumerator.MoveNext())
                    {
                        C.WriteLine("Missing username.");
                        return false;
                    }

                    username = enumerator.Current;
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
                else if (arg == "--help")
                {
                    PrintHelp();
                    return false;
                }
                else
                {
                    C.WriteLine("Unknown argument: {0}", arg);
                }
            }

            parsed = new CommandLineArgs(
                headless,
                selfContained,
                connect,
                launcher,
                username,
                cvars,
                logLevels,
                connectAddress,
                ss14Address,
                mountOptions);

            return true;
        }

        private static void PrintHelp()
        {
            C.WriteLine(@"
Options:
  --headless          Run without graphics/audio/input.
  --self-contained    Store data relative to executable instead of user-global locations.
  --connect           Automatically connect to connect-address.
  --connect-address   Address to automatically connect to.
                        Default: localhost
  --ss14-address      Address that was actually entered into the launcher.
  --launcher          Run in launcher mode (no main menu, auto connect).
  --username          Override username.
  --cvar              Specifies an additional cvar overriding the config file. Syntax is <key>=<value>
  --loglevel          Specifies an additional sawmill log level overriding the default values. Syntax is <key>=<value>
  --mount-dir         Resource directory to mount.
  --mount-zip         Resource zip to mount.
  --help              Display this help text and exit.
");
        }

        private CommandLineArgs(
            bool headless,
            bool selfContained,
            bool connect,
            bool launcher,
            string? username,
            IReadOnlyCollection<(string key, string value)> cVars,
            IReadOnlyCollection<(string key, string value)> logLevels,
            string connectAddress, string? ss14Address,
            MountOptions mountOptions)
        {
            Headless = headless;
            SelfContained = selfContained;
            Connect = connect;
            Launcher = launcher;
            Username = username;
            CVars = cVars;
            LogLevels = logLevels;
            ConnectAddress = connectAddress;
            Ss14Address = ss14Address;
            MountOptions = mountOptions;
        }
    }
}
