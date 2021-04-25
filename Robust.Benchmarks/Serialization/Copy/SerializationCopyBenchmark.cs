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

namespace Robust.Benchmarks.Serialization.Copy
{
    public class SerializationCopyBenchmark
    {
        public SerializationCopyBenchmark()
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

            DataDefinitionWithString = new DataDefinitionWithString {StringField = "ABC"};

            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(SeedDataDefinition.Prototype));

            var seedMapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>().Cast<MappingDataNode>(0);

            Seed = SerializationManager.ReadValueOrThrow<SeedDataDefinition>(seedMapping);
        }

        private ISerializationManager SerializationManager { get; }

        private const string String = "ABC";

        private const int Integer = 1;

        private DataDefinitionWithString DataDefinitionWithString { get; }

        private SeedDataDefinition Seed { get; }

        [Benchmark]
        public string? CreateCopyString()
        {
            return SerializationManager.CreateCopy(String);
        }

        [Benchmark]
        public int? CreateCopyInteger()
        {
            return SerializationManager.CreateCopy(Integer);
        }

        [Benchmark]
        public DataDefinitionWithString? CreateCopyDataDefinitionWithString()
        {
            return SerializationManager.CreateCopy(DataDefinitionWithString);
        }

        [Benchmark]
        public SeedDataDefinition? CreateCopySeedDataDefinition()
        {
            return SerializationManager.CreateCopy(Seed);
        }
    }
}
