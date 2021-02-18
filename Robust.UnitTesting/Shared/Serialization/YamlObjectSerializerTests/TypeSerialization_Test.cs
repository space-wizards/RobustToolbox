using System.IO;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.YAML;
using YamlDotNet.RepresentationModel;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [TestFixture]
    public class TypeSerialization_Test : RobustUnitTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<IServ3Manager>().Initialize();
        }

        [Test]
        public void SerializeTypeTest()
        {
            ITestType type = new TestTypeOne();
            var factory = new YamlDataNodeFactory();
            var serMan = IoCManager.Resolve<IServ3Manager>();
            var mapping = (MappingDataNode) serMan.WriteValue(type, factory);

            Assert.IsNotEmpty(mapping.Children);
            Assert.IsInstanceOf<YamlScalarNode>(mapping[0]);

            var scalar = (ValueDataNode) mapping[0].Key;

            Assert.That(scalar.Value, Is.EqualTo("type"));
        }

        [Test]
        public void DeserializeTypeTest()
        {
            var yaml = @"
test:
  !type:testtype1";

            using var stream = new MemoryStream();

            var writer = new StreamWriter(stream);
            writer.Write(yaml);
            writer.Flush();
            stream.Position = 0;

            var streamReader = new StreamReader(stream);
            var yamlStream = new YamlStream();
            yamlStream.Load(streamReader);

            var mapping = (YamlMappingNode) yamlStream.Documents[0].RootNode;
            var serMan = IoCManager.Resolve<IServ3Manager>();
            var type = serMan.ReadValue<ITestType>(new YamlMappingDataNode(mapping));

            Assert.NotNull(type);
            Assert.IsInstanceOf<TestTypeOne>(type);
        }
    }

    public interface ITestType { }

    [SerializedType("testtype1")]
    [DataDefinition]
    public class TestTypeOne : ITestType
    {
    }
}
