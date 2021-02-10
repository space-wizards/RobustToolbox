using System.IO;
using NUnit.Framework;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [TestFixture]
    [TestOf(typeof(YamlObjectSerializer))]
    public class TypeSerialization_Test : RobustUnitTest
    {
        [Test]
        public void SerializeTypeTest()
        {
            ITestType? type = new TestTypeOne();
            var mapping = new YamlMappingNode();
            var writer = YamlObjectSerializer.NewWriter(mapping);

            writer.DataField(ref type, "type", null);

            Assert.IsNotEmpty(mapping.Children);
            Assert.IsInstanceOf<YamlScalarNode>(mapping.Children[0].Key);

            var scalar = (YamlScalarNode) mapping.Children[0].Key;

            Assert.That(scalar.Value, Is.EqualTo("type"));
        }

        [Test]
        public void DeserializeTypeTest()
        {
            ITestType? type = null;
            var yaml = "type: test type one";

            using var stream = new MemoryStream();

            var writer = new StreamWriter(stream);
            writer.Write(yaml);
            writer.Flush();
            stream.Position = 0;

            var streamReader = new StreamReader(stream);
            var yamlStream = new YamlStream();
            yamlStream.Load(streamReader);

            var mapping = (YamlMappingNode) yamlStream.Documents[0].RootNode;

            var reader = YamlObjectSerializer.NewReader(mapping);
            reader.DataField(ref type, "type", null);

            Assert.NotNull(type);
            Assert.IsInstanceOf<TestTypeOne>(type);
        }
    }

    public interface ITestType : IExposeData { }

    [SerializedType("test type one")]
    public class TestTypeOne : ITestType
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) { }
    }
}
