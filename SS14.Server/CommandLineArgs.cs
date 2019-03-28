using CommandLine;
using CommandLine.Text;
using SS14.Server.Interfaces;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Reflection;
using SS14.Shared.ContentPack;

namespace SS14.Server
{
    public class CommandLineArgs : ICommandLineArgs
    {
        [Option("config-file", Required = false, HelpText = "Config file to read from.")]
        public string configFile { get; set; }
        public string ConfigFile
        {
            get
            {
                // If a config file path was passed, use it literally.
                // This ensures it's working-directory relative (for people passing config file through the terminal or something).
                // Otherwise use the one next to the executable.
                return configFile ?? PathHelpers.ExecutableRelativeFile("server_config.toml");
            }
        }

        [Option("data-dir", Required = false, HelpText = "Data directory to read/write to.")]
        public string dataDir { get; set; }

        public string DataDir
        {
            get
            {
                return dataDir ?? PathHelpers.ExecutableRelativeFile("data");
            }
        }

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

        public bool Parse()
        {
            return CommandLine.Parser.Default.ParseArguments(Environment.GetCommandLineArgs(), this);
        }
    }
}
