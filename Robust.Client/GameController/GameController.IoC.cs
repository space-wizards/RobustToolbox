using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client
{
    // Partial of GameController to initialize IoC and some other low-level systems like it.
    internal sealed partial class GameController
    {
        private static void InitIoC(DisplayMode mode)
        {
            ClientIoC.RegisterIoC(mode);
            IoCManager.BuildGraph();
            RegisterReflection();
        }


        internal static void RegisterReflection()
        {
            // Gets a handle to the shared and the current (client) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                // Do NOT register Robust.Client.Godot.
                // At least not for now.
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }
    }
}
