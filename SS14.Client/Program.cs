using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using SS14.Shared.Log;
using SS14.Client.Interfaces;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Prototypes;
using SS14.Shared.GameObjects;

namespace SS14.Client
{
    public class Program
    {
        /************************************************************************/
        /* program starts here                                                  */
        /************************************************************************/

        [STAThread]
        private static void Main()
        {
            LoadAssemblies();

            var controller = IoCManager.Resolve<IGameController>();
            controller.Run();

            Logger.Info("Goodbye.");
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

            // Client stuff.
        }

        private static void LoadAssemblies()
        {
            var assemblies = new List<Assembly>(2);

            // TODO this should be done on connect.
            // The issue is that due to our giant trucks of shit code.
            // It'd be extremely hard to integrate correctly.
            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Shared.Content.dll");
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("**ERROR: Unable to load the shared content assembly (SS14.Shared.Content.dll): {0}", e);
                System.Console.ResetColor();
            }

            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Server.Content.dll");
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("**ERROR: Unable to load the server content assembly (SS14.Server.Content.dll): {0}", e);
                System.Console.ResetColor();
            }

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);
        }
    }
}
