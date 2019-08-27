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

        [Option("connect", Required = false, HelpText = "Automatically connect to localhost.")]
        public bool Connect { get; set; }
    }
}
