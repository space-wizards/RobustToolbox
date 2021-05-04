using System.IO;
using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization.Definitions;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.RepresentationModel;

namespace Robust.Benchmarks.Serialization.Write
{
    public class SerializationWriteBenchmark : SerializationBenchmark
    {
        public SerializationWriteBenchmark()
        {
            InitializeSerialization();

            DataDefinitionWithString = new DataDefinitionWithString {StringField = "ABC"};

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(SeedDataDefinition.Prototype));

            var seedMapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);

            Seed = SerializationManager.ReadValueOrThrow<SeedDataDefinition>(seedMapping);
        }

        private const string String = "ABC";

        private const int Integer = 1;

        private DataDefinitionWithString DataDefinitionWithString { get; }

        private SeedDataDefinition Seed { get; }

        [Benchmark]
        public DataNode WriteString()
        {
            return SerializationManager.WriteValue(String);
        }

        [Benchmark]
        public DataNode WriteInteger()
        {
            return SerializationManager.WriteValue(Integer);
        }

        [Benchmark]
        public DataNode WriteDataDefinitionWithString()
        {
            return SerializationManager.WriteValue(DataDefinitionWithString);
        }

        [Benchmark]
        public DataNode WriteSeedDataDefinition()
        {
            return SerializationManager.WriteValue(Seed);
        }

        // [Benchmark]
        // public DataNode BaselineWriteSeedDataDefinition()
        // {
        //     var mapping = new MappingDataNode();
        //
        //     mapping.AddNode("id", Seed.ID);
        //     mapping.AddNode("name", Seed.Name);
        //     mapping.AddNode("seedName", Seed.SeedName);
        //     mapping.AddNode("displayName", Seed.DisplayName);
        // }
    }
}
