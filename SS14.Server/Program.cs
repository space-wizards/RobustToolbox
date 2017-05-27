using SS14.Shared.Log;
using SS14.Shared.ServerEnums;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace SS14.Server
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite

            var assemblies = new List<Assembly>();
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"));
            assemblies.Add(Assembly.GetExecutingAssembly());
            IoCManager.AddAssemblies(assemblies);

            var server = IoCManager.Resolve<ISS14Server>();

            LogManager.Log("Server -> Starting");

            if (server.Start())
            {
                LogManager.Log("Server -> Can not start server", LogLevel.Fatal);
                //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogManager.Log("Server Version " + strVersion + " -> Ready");

            SignalHander.InstallSignals();

            server.MainLoop();
        }

        private static CommandLineArgs processArgs(string[] args)
        {
            var options = new CommandLineArgs();
            bool result = CommandLine.Parser.Default.ParseArguments(args, options);
            if (!result)
            {
                Environment.Exit(0);
            }

            return options;
        }
    }
}
