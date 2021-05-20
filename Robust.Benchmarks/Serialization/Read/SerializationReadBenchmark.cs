using System.IO;
using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization.Definitions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.Benchmarks.Serialization.Read
{
    public class SerializationReadBenchmark : SerializationBenchmark
    {
        public SerializationReadBenchmark()
        {
            InitializeSerialization();

            StringDataDefNode = new MappingDataNode();
            StringDataDefNode.Add(new ValueDataNode("string"), new ValueDataNode("ABC"));

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(SeedDataDefinition.Prototype));

            SeedNode = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);
        }

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
