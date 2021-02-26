using System.IO;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
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
            IoCManager.Resolve<ISerializationManager>().Initialize();
        }

        [Test]
        public void SerializeTypeTest()
        {
            ITestType type = new TestTypeOne();
            var serMan = IoCManager.Resolve<ISerializationManager>();
            var mapping = serMan.WriteValue(type);

            Assert.IsInstanceOf<ValueDataNode>(mapping);

            var scalar = (ValueDataNode) mapping;

            Assert.That(scalar.Value, Is.EqualTo(""));
            Assert.That(scalar.Tag, Is.EqualTo("!type:TestTypeOne"));
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
            var serMan = IoCManager.Resolve<ISerializationManager>();
            var type = serMan.Read<ITestType>(new MappingDataNode(mapping)["test"]);

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
