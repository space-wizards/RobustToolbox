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

namespace Robust.Benchmarks.Serialization.Read
{
    public class SerializationReadBenchmark
    {
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

            SeedNode = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);
        }

        private ISerializationManager SerializationManager { get; }

        private ValueDataNode StringNode { get; } = new("ABC");

        private ValueDataNode IntNode { get; } = new("1");

        private MappingDataNode StringDataDefNode { get; }

        private MappingDataNode SeedNode { get; }

        [Benchmark]
        public string? ReadString()
        {
            return SerializationManager.ReadValue<string>(StringNode);
        }

        [Benchmark]
        public int? ReadInteger()
        {
            return SerializationManager.ReadValue<int>(IntNode);
        }

        [Benchmark]
        public DataDefinitionWithString? ReadDataDefinitionWithString()
        {
            return SerializationManager.ReadValue<DataDefinitionWithString>(StringDataDefNode);
        }

        [Benchmark]
        public SeedDataDefinition? ReadSeedDataDefinition()
        {
            return SerializationManager.ReadValue<SeedDataDefinition>(SeedNode);
        }
    }
}
