using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization.Definitions;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Benchmarks.Serialization
{
    [MemoryDiagnoser]
    public class SerializationArrayBenchmark : SerializationBenchmark
    {
        public SerializationArrayBenchmark()
        {
            InitializeSerialization();

            OneStringDefNode = new SequenceDataNode();
            OneStringDefNode.Add(new MappingDataNode
            {
                ["string"] = new ValueDataNode("ABC")
            });

            TenStringDefsNode = new SequenceDataNode();

            for (var i = 0; i < 10; i++)
            {
                TenStringDefsNode.Add(new MappingDataNode
                {
                    ["string"] = new ValueDataNode("ABC")
                });
            }
        }

        private SequenceDataNode EmptyNode { get; } = new();

        private SequenceDataNode OneIntNode { get; } = new("1");

        private SequenceDataNode TenIntsNode { get; } = new("1", "2", "3", "4", "5", "6", "7", "8", "9", "10");

        private SequenceDataNode OneStringDefNode { get; }

        private SequenceDataNode TenStringDefsNode { get; }

        [Benchmark]
        [BenchmarkCategory("read")]
        public string[]? ReadEmptyString()
        {
            return SerializationManager.ReadValue<string[]>(EmptyNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public string[]? ReadOneString()
        {
            return SerializationManager.ReadValue<string[]>(OneIntNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public string[]? ReadTenStrings()
        {
            return SerializationManager.ReadValue<string[]>(TenIntsNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public int[]? ReadEmptyInt()
        {
            return SerializationManager.ReadValue<int[]>(EmptyNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public int[]? ReadOneInt()
        {
            return SerializationManager.ReadValue<int[]>(OneIntNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public int[]? ReadTenInts()
        {
            return SerializationManager.ReadValue<int[]>(TenIntsNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public DataDefinitionWithString[]? ReadEmptyStringDataDef()
        {
            return SerializationManager.ReadValue<DataDefinitionWithString[]>(EmptyNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public DataDefinitionWithString[]? ReadOneStringDataDef()
        {
            return SerializationManager.ReadValue<DataDefinitionWithString[]>(OneStringDefNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public DataDefinitionWithString[]? ReadTenStringDataDefs()
        {
            return SerializationManager.ReadValue<DataDefinitionWithString[]>(TenStringDefsNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public SealedDataDefinitionWithString[]? ReadEmptySealedStringDataDef()
        {
            return SerializationManager.ReadValue<SealedDataDefinitionWithString[]>(EmptyNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public SealedDataDefinitionWithString[]? ReadOneSealedStringDataDef()
        {
            return SerializationManager.ReadValue<SealedDataDefinitionWithString[]>(OneStringDefNode);
        }

        [Benchmark]
        [BenchmarkCategory("read")]
        public SealedDataDefinitionWithString[]? ReadTenSealedStringDataDefs()
        {
            return SerializationManager.ReadValue<SealedDataDefinitionWithString[]>(TenStringDefsNode);
        }
    }
}
