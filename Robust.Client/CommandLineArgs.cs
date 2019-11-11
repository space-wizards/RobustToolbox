using CommandLine;

namespace Robust.Client
{
    internal sealed class CommandLineArgs
    {
        [Option("headless", Required = false, HelpText = "Run without graphics/audio.")]
        public bool Headless { get; set; }

        [Option("self-contained", Required = false,
            HelpText = "Store data relative to executable instead of user-global locations.")]
        public bool SelfContained { get; set; }

        [Option("connect", Required = false, HelpText = "Automatically connect to connect-address.")]
        public bool Connect { get; set; }

        [Option("connect-address", Required = false, HelpText = "Address to automatically connect to.")]
        public string ConnectAddress { get; set; } = "localhost";

        [Option("launcher", Required = false, HelpText = "Run in launcher mode (no main menu, auto connect).")]
        public bool Launcher { get; set; }

        [Option("username", Required = false, HelpText = "Override username.")]
        public string Username { get; set; }
    }
}
