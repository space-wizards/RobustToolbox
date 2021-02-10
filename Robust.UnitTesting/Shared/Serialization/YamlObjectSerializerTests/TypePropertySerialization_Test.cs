using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [TestFixture]
    [TestOf(typeof(YamlObjectSerializer))]
    public class TypePropertySerialization_Test : RobustUnitTest
    {
        [Test]
        public void SerializeTypePropertiesTest()
        {
            var type = new TestTypeTwo
            {
                TestPropertyOne = "B",
                TestPropertyTwo = 10
            };
            var mapping = new YamlMappingNode();
            var writer = YamlObjectSerializer.NewWriter(mapping);

            writer.DataField(ref type, "test", null);

            Assert.IsNotEmpty(mapping.Children);

            var testPropertyOne = (YamlScalarNode) mapping["test"]["testPropertyOne"];
            var testPropertyTwo = (YamlScalarNode) mapping["test"]["testPropertyTwo"];

            Assert.That(testPropertyOne.Value, Is.EqualTo("B"));
            Assert.That(testPropertyTwo.Value, Is.EqualTo("10"));
        }

        [Test]
        public void DeserializeTypePropertiesTest()
        {
            ITestType? type = null;
            var yaml = @"
- test:
    type: test type two
    testPropertyOne: A
    testPropertyTwo: 5
";

            using var stream = new MemoryStream();

            var writer = new StreamWriter(stream);
            writer.Write(yaml);
            writer.Flush();
            stream.Position = 0;

            var streamReader = new StreamReader(stream);
            var yamlStream = new YamlStream();
            yamlStream.Load(streamReader);

            var mapping = (YamlMappingNode) yamlStream.Documents[0].RootNode[0];

            var reader = YamlObjectSerializer.NewReader(mapping);
            reader.DataField(ref type, "test", null);

            Assert.NotNull(type);
            Assert.IsInstanceOf<TestTypeTwo>(type);

            var testTypeTwo = (TestTypeTwo) type!;

            Assert.That(testTypeTwo.TestPropertyOne, Is.EqualTo("A"));
            Assert.That(testTypeTwo.TestPropertyTwo, Is.EqualTo(5));
        }
    }

    [SerializedType("test type two")]
    public class TestTypeTwo : ITestType
    {
        public string? TestPropertyOne { get; set; }

        public int TestPropertyTwo { get; set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => TestPropertyOne, "testPropertyOne", null);
            serializer.DataField(this, x => TestPropertyTwo, "testPropertyTwo", 0);
        }
    }

    [RegisterComponent]
    public class TestComponent : Component
    {
        public override string Name => "Test";

        public ITestType TestType { get; } = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.TestType, "testType", null);
        }
    }
}
