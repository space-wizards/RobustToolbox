using System;
using BenchmarkDotNet.Attributes;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Benchmarks.Serialization
{
    public class SerializationReadBenchmark
    {
        private const int N = 1000;

        public SerializationReadBenchmark()
        {
            IoCManager.InitThread();
            ServerIoC.RegisterIoC();
            IoCManager.BuildGraph();

            var assemblies = new[]
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server")
            };

            foreach (var assembly in assemblies)
            {
                IoCManager.Resolve<IConfigurationManagerInternal>().LoadCVarsFromAssembly(assembly);
            }

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            SerializationManager = IoCManager.Resolve<ISerializationManager>();
            SerializationManager.Initialize();

            Node = new ValueDataNode("ABC");
        }

        private ISerializationManager SerializationManager { get; }

        private ValueDataNode Node { get; }

        [Benchmark]
        public void ReadString()
        {
            SerializationManager.ReadValue<string>(Node);
        }
    }
}
