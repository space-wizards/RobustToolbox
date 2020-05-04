using System;

using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Prototypes;

namespace Robust.Analyzer
{
    internal static class AnalyzerIoC
    {
        /// <summary>
        /// Registers some interfaces under <see cref="IoCManager"/>, to try to get to a working <see cref="IPrototypeManager"/>.
        /// </summary>
        /// <param name="reflectionTypePrefixes">The set of type prefixes the <see cref="IReflectionManager"/> implementation should use.</param>
        public static void RegisterIoC(string[] reflectionTypePrefixes)
        {
            IoCManager.InitThread();
            IoCManager.Clear();

            SharedIoC.RegisterIoC();
            // Try to set up the minimum required for the prototype manager
            IoCManager.Register<IComponentFactory, ComponentFactory>();
            IoCManager.Register<IEntityManager, AnalyzerEntityManager>();
            IoCManager.Register<IPrototypeManager, PrototypeManager>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.RegisterInstance<IReflectionManager>(new AnalyzerReflectionManager(reflectionTypePrefixes));

            IoCManager.BuildGraph();
        }

        public static T Resolve<T>()
        {
            return IoCManager.Resolve<T>();
        }
    }
}
