using System.Globalization;
using System.IO;
using BenchmarkDotNet.Attributes;
using Robust.Benchmarks.Serialization.Definitions;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
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

        private BenchmarkFlagsEnum FlagZero = BenchmarkFlagsEnum.Zero;

        private BenchmarkFlagsEnum FlagThirtyOne = BenchmarkFlagsEnum.ThirtyOne;

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

        [Benchmark]
        public DataNode BaselineWriteSeedDataDefinition()
        {
            var mapping = new MappingDataNode();

            mapping.Add("id", Seed.ID);
            mapping.Add("name", Seed.Name);
            mapping.Add("seedName", Seed.SeedName);
            mapping.Add("displayName", Seed.DisplayName);
            mapping.Add("productPrototypes", Seed.ProductPrototypes);
            mapping.Add("harvestRepeat", Seed.HarvestRepeat.ToString());
            mapping.Add("lifespan", Seed.Lifespan.ToString(CultureInfo.InvariantCulture));
            mapping.Add("maturation", Seed.Maturation.ToString(CultureInfo.InvariantCulture));
            mapping.Add("production", Seed.Production.ToString(CultureInfo.InvariantCulture));
            mapping.Add("yield", Seed.Yield.ToString(CultureInfo.InvariantCulture));
            mapping.Add("potency", Seed.Potency.ToString(CultureInfo.InvariantCulture));
            mapping.Add("growthStages", Seed.GrowthStages.ToString(CultureInfo.InvariantCulture));
            mapping.Add("idealLight", Seed.IdealLight.ToString(CultureInfo.InvariantCulture));
            mapping.Add("idealHeat", Seed.IdealHeat.ToString(CultureInfo.InvariantCulture));

            var chemicals = new MappingDataNode();
            foreach (var (name, quantity) in Seed.Chemicals)
            {
                chemicals.Add(name, new MappingDataNode
                {
                    ["Min"] = new ValueDataNode(quantity.Min.ToString(CultureInfo.InvariantCulture)),
                    ["Max"] = new ValueDataNode(quantity.Max.ToString(CultureInfo.InvariantCulture)),
                    ["PotencyDivisor"] = new ValueDataNode(quantity.PotencyDivisor.ToString(CultureInfo.InvariantCulture))
                });
            }

            mapping.Add("chemicals", chemicals);

            return mapping;
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public DataNode WriteFlagZero()
        {
            return SerializationManager.WriteWithTypeSerializer(
                typeof(int),
                typeof(FlagSerializer<BenchmarkFlags>),
                FlagZero);
        }

        [Benchmark]
        [BenchmarkCategory("flag")]
        public DataNode WriteThirtyOne()
        {
            return SerializationManager.WriteWithTypeSerializer(
                typeof(int),
                typeof(FlagSerializer<BenchmarkFlags>),
                FlagThirtyOne);
        }
    }
}
