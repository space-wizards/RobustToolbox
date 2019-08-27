using CommandLine;
using CommandLine.Text;
using Robust.Server.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using System;
using System.Reflection;
using Robust.Shared.ContentPack;

namespace Robust.Server
{
    internal sealed class CommandLineArgs
    {
        [Option("config-file", Required = false, HelpText = "Config file to read from.")]
        public string ConfigFile { get; set; }

        [Option("data-dir", Required = false, HelpText = "Data directory to read/write to.")]
        public string DataDir { get; set; }
    }
}
