using System.IO;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [TestFixture]
    internal sealed class TypeSerialization_Test : OurRobustUnitTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
        }

        [Test]
        public void SerializeTypeTest()
        {
            ITestType type = new TestType1();
            var serMan = IoCManager.Resolve<ISerializationManager>();
            var mapping = serMan.WriteValue(type, notNullableOverride: true);

            Assert.That(mapping, Is.InstanceOf<MappingDataNode>());

            var scalar = (MappingDataNode) mapping;

            Assert.That(scalar.Children.Count, Is.EqualTo(0));
            Assert.That(scalar.Tag, Is.EqualTo("!type:TestType1"));
        }

        [Test]
        public void DeserializeTypeTest()
        {
            var yaml = @"
test:
  !type:TestType1
  {}";

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
            var type = serMan.Read<ITestType>(new MappingDataNode(mapping)["test"], notNullableOverride: true);

            Assert.That(type, Is.Not.Null);
            Assert.That(type, Is.InstanceOf<TestType1>());
        }
    }

    public interface ITestType { }

    [DataDefinition]
    public sealed partial class TestType1 : ITestType
    {
    }
}
