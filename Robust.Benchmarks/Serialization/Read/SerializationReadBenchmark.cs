using System.IO;
using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization.Definitions;
using Robust.Shared.Analyzers;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using YamlDotNet.RepresentationModel;

namespace Robust.Benchmarks.Serialization.Read
{
    [MemoryDiagnoser]
    [Virtual]
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

        private ValueDataNode FlagZero { get; } = new("Zero");

        private ValueDataNode FlagThirtyOne { get; } = new("ThirtyOne");

        [Benchmark]
        public string ReadString()
        {
            return SerializationManager.Read<string>(StringNode);
        }

        [Benchmark]
        public int ReadInteger()
        {
            return SerializationManager.Read<int>(IntNode);
        }

        [Benchmark]
        public DataDefinitionWithString ReadDataDefinitionWithString()
        {
            return SerializationManager.Read<DataDefinitionWithString>(StringDataDefNode);
        }

        [Benchmark]
        public SeedDataDefinition ReadSeedDataDefinition()
        {
            return SerializationManager.Read<SeedDataDefinition>(SeedNode);
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public object? ReadFlagZero()
        {
            return SerializationManager.ReadWithTypeSerializer(
                typeof(int),
                typeof(FlagSerializer<BenchmarkFlags>),
                FlagZero);
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public object? ReadThirtyOne()
        {
            return SerializationManager.ReadWithTypeSerializer(
                typeof(int),
                typeof(FlagSerializer<BenchmarkFlags>),
                FlagThirtyOne);
        }

        [Benchmark]
        [BenchmarkCategory("customTypeSerializer")]
        public object? ReadIntegerCustomSerializer()
        {
            return SerializationManager.ReadWithTypeSerializer(
                typeof(int),
                typeof(BenchmarkIntSerializer),
                IntNode);
        }
    }
}
