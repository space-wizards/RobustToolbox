using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.RepresentationModel;

namespace Robust.Benchmarks.Serialization
{
    internal class SerializationReadBenchmark
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
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Benchmarks")
            };

            foreach (var assembly in assemblies)
            {
                IoCManager.Resolve<IConfigurationManagerInternal>().LoadCVarsFromAssembly(assembly);
            }

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);

            SerializationManager = IoCManager.Resolve<ISerializationManager>();
            SerializationManager.Initialize();

            StringDataDefNode = new MappingDataNode();
            StringDataDefNode.AddNode(new ValueDataNode("string"), new ValueDataNode("ABC"));

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(SeedDataDefinition.Prototype));

            SeedNode = yamlStream.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
        }

        private ISerializationManager SerializationManager { get; }

        private ValueDataNode StringNode { get; } = new("ABC");

        private ValueDataNode IntNode { get; } = new("1");

        private MappingDataNode StringDataDefNode { get; }

        private MappingDataNode SeedNode { get; }

        // [Benchmark]
        public void ReadString()
        {
            SerializationManager.ReadValue<string>(StringNode);
        }

        // [Benchmark]
        public void ReadInteger()
        {
            SerializationManager.ReadValue<int>(IntNode);
        }

        // [Benchmark]
        public void ReadDataDefinitionWithString()
        {
            SerializationManager.ReadValue<DataDefinitionWithString>(StringDataDefNode);
        }

        [Benchmark]
        public void ReadSeedDataDefinition()
        {
            SerializationManager.ReadValue<SeedDataDefinition>(SeedNode);
        }
    }
}
