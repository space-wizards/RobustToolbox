using SS14.Shared.Log;
using SS14.Shared.ServerEnums;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Server.Interfaces;
using SS14.Shared.ContentLoader;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;


namespace SS14.Server
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            //Process command-line args
            var parsedArgs = processArgs(args);
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite

            LoadAssemblies();

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

        private static void LoadAssemblies()
        {
            var assemblies = new List<Assembly>(2);
            assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"));
            assemblies.Add(Assembly.GetExecutingAssembly());
            IoCManager.AddAssemblies(assemblies);

            assemblies.Clear();

            // So we can't actually access this until IoC has loaded the initial assemblies. Yay.
            var loader = IoCManager.Resolve<IContentLoader>();
            assemblies.Clear();

            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Shared.Content.dll");
                loader.LoadAssembly(contentAssembly);
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("**ERROR: Unable to load the shared content assembly (SS14.Shared.Content.dll): {0}", e);
                Console.ResetColor();
            }

            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Server.Content.dll");
                loader.LoadAssembly(contentAssembly);
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("**ERROR: Unable to load the server content assembly (SS14.Server.Content.dll): {0}", e);
                Console.ResetColor();
            }

            IoCManager.AddAssemblies(assemblies);
        }
    }
}
