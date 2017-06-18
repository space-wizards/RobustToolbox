using CommandLine;
using CommandLine.Text;
using SS14.Server.Interfaces;
using SS14.Shared.IoC;
using System.Reflection;

namespace SS14.Server
{
    class CommandLineArgs : ICommandLineArgs
    {
        [Option("config-file", Required = false, DefaultValue = "./server_config.xml", HelpText = "Config file to read from.")]
        public string ConfigFile { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var help = new HelpText
            {
                Heading = new HeadingInfo("Space Station 14 Server", strVersion),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("Usage: SpaceStation14_Server.exe [OPTIONS]");
            help.AddOptions(this);

            return help;
        }
    }
}
