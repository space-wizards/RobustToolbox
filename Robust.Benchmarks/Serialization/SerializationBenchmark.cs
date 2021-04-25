using System;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;

namespace Robust.Benchmarks.Serialization
{
    public abstract class SerializationBenchmark
    {
        public SerializationBenchmark()
        {
            IoCManager.InitThread();
            ServerIoC.RegisterIoC();
            IoCManager.BuildGraph();

            var assemblies = new[]
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Benchmarks")
            };

            foreach (var assembly in assemblies)
            {
                IoCManager.Resolve<IConfigurationManagerInternal>().LoadCVarsFromAssembly(assembly);
            }

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            SerializationManager = IoCManager.Resolve<ISerializationManager>();
        }

        protected ISerializationManager SerializationManager { get; }

        public void InitializeSerialization()
        {
            SerializationManager.Initialize();
        }
    }
}
