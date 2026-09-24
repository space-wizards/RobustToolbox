using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [TestFixture]
    internal sealed class TypePropertySerialization_Test : OurRobustUnitTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
        }

        [Test]
        public void SerializeTypePropertiesTest()
        {
            ITestType? type = new TestType2
            {
                TestPropertyOne = "B",
                TestPropertyTwo = 10
            };
            var serMan = IoCManager.Resolve<ISerializationManager>();
            var mapping = (MappingDataNode) serMan.WriteValue(type, notNullableOverride: true);

            Assert.That(mapping.Children, Is.Not.Empty);

            var testPropertyOne = mapping.Get("testPropertyOne") as ValueDataNode;
            var testPropertyTwo = mapping.Get("testPropertyTwo") as ValueDataNode;

            Assert.That(testPropertyOne, Is.Not.Null);
            Assert.That(testPropertyTwo, Is.Not.Null);
            Assert.That(testPropertyOne!.Value, Is.EqualTo("B"));
            Assert.That(testPropertyTwo!.Value, Is.EqualTo("10"));
        }

        [Test]
        public void DeserializeTypePropertiesTest()
        {
            var yaml = @"
- test:
    !type:TestType2
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

            var serMan = IoCManager.Resolve<ISerializationManager>();
            var type = serMan.Read<ITestType>(mapping["test"].ToDataNode(), notNullableOverride: true);

            Assert.That(type, Is.Not.Null);
            Assert.That(type, Is.InstanceOf<TestType2>());

            var testTypeTwo = (TestType2) type!;

            Assert.That(testTypeTwo.TestPropertyOne, Is.EqualTo("A"));
            Assert.That(testTypeTwo.TestPropertyTwo, Is.EqualTo(5));
        }
    }

    [DataDefinition]
    public sealed partial class TestType2 : ITestType
    {
        [DataField("testPropertyOne")]
        public string? TestPropertyOne { get; set; }

        [DataField("testPropertyTwo")]
        public int TestPropertyTwo { get; set; }
    }

    [RegisterComponent]
    internal sealed partial class TestComponent : Component
    {
        [DataField("testType")] public ITestType? TestType { get; set; }
    }
}
