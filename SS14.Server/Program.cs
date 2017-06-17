using SS14.Server.Interfaces;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Shared.Prototypes;

namespace SS14.Server
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite

            RegisterIoC();
            LoadContentAssemblies();

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

            // TODO: Move this to an interface.
            SignalHander.InstallSignals();

            server.MainLoop();

            Logger.Info("Goodbye.");

            // Used to dispose of systems that want to be disposed.
            // Such as the log manager.
            IoCManager.Clear();
        }

        /// <summary>
        /// Registers all the types into the <see cref="IoCManager"/> with <see cref="IoCManager.Register{TInterface, TImplementation}"/>
        /// </summary>
        private static void RegisterIoC()
        {
            // Shared stuff.
            IoCManager.Register<IComponentManager, ComponentManager>();
            IoCManager.Register<IPrototypeManager, PrototypeManager>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<IComponentFactory, ComponentFactory>();
            IoCManager.Register<ILogManager, LogManager>();

            // Server stuff.
        }

        // TODO: Move to the main server so we can have proper logging and stuff.
        private static void LoadContentAssemblies()
        {
            var assemblies = new List<Assembly>(2);

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

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);
        }
    }
}
