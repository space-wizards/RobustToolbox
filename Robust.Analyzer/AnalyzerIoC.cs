using System;

using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Analyzer
{
    internal static class AnalyzerIoC
    {
        /// <summary>
        /// Registers some interfaces under <see cref="IoCManager"/>, to try to get to a working <see cref="IPrototypeManager"/>.
        /// </summary>
        public static void RegisterIoC()
        {
            IoCManager.InitThread();
            IoCManager.Clear();

            SharedIoC.RegisterIoC();
            // Try to set up the minimum required for the prototype manager
            IoCManager.Register<IComponentFactory, ComponentFactory>();
            IoCManager.Register<IEntityManager, AnalyzerEntityManager>();
            IoCManager.Register<IPrototypeManager, PrototypeManager>();
            IoCManager.Register<IReflectionManager, AnalyzerReflectionManager>();
            IoCManager.Register<IResourceManager, ResourceManager>();

            IoCManager.BuildGraph();
        }

        public static T Resolve<T>()
        {
            return IoCManager.Resolve<T>();
        }
    }
}
