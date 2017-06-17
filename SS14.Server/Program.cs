using SS14.Shared.Log;
using SS14.Shared.ServerEnums;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Server.Interfaces;
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
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite

            LoadAssemblies();

            var server = IoCManager.Resolve<ISS14Server>();

            Logger.Log("Server -> Starting");

            if (server.Start())
            {
                Logger.Log("Server -> Can not start server", LogLevel.Fatal);
                //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.Log("Server Version " + strVersion + " -> Ready");

            SignalHander.InstallSignals();

            server.MainLoop();

            Logger.Info("Goodbye.");
            IoCManager.Clear();
        }

        private static void LoadAssemblies()
        {
            var assemblies = new List<Assembly>(4)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                Assembly.GetExecutingAssembly()
            };
            IoCManager.AddAssemblies(assemblies);


            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Shared.Content.dll");
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
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);
        }
    }
}
