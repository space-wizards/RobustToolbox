using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Log;
using SS14.Client.Reflection;
using SS14.Client.ResourceManagement;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SS14.Client
{
    // Partial of GameController to initialize IoC and some other low-level systems like it.
    public partial class GameController
    {
        private void InitIoC()
        {
            RegisterIoC();
            RegisterReflection();
            Logger.Debug("IoC Initialized!");

            Logger.Error(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared").CodeBase);

            // We are not IoC-managed (Godot spawns us), but we still want the dependencies.
            IoCManager.InjectDependencies(this);
        }

        private static void RegisterIoC()
        {
            // Shared stuff.
            IoCManager.Register<ILogManager, GodotLogManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<IResourceManager, ResourceManager>();

            // Client stuff.
            IoCManager.Register<IReflectionManager, ClientReflectionManager>();
            IoCManager.Register<IResourceCache, ResourceCache>();

            IoCManager.BuildGraph();
        }

        private static void RegisterReflection()
        {
            // Gets a handle to the shared and the current (client) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                // Do NOT register SS14.Client.Godot.
                // At least not for now.
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }
    }
}
