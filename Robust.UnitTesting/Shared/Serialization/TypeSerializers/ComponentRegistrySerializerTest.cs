using System.IO;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using YamlDotNet.RepresentationModel;
using static Robust.Shared.Prototypes.EntityPrototype;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(ComponentRegistrySerializer))]
    public class ComponentRegistrySerializerTest : SerializationTest
    {
        [OneTimeSetUp]
        public new void OneTimeSetup()
        {
            var componentFactory = IoCManager.Resolve<IComponentFactory>();
            componentFactory.RegisterClass<TestComponent>();
        }

        [Test]
        public void SerializationTest()
        {
            var component = new TestComponent();
            var registry = new ComponentRegistry {{"Test", component}};
            var node = Serialization.WriteValueAs<SequenceDataNode>(registry);

            Assert.That(node.Sequence.Count, Is.EqualTo(1));
            Assert.IsInstanceOf<MappingDataNode>(node[0]);

            var mapping = node.Cast<MappingDataNode>(0);
            Assert.That(mapping.Cast<ValueDataNode>("type").Value, Is.EqualTo("Test"));
        }

        [Test]
        public void DeserializationTest()
        {
            var str = "- type: Test";
            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(str));

            var mapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>();

            var deserializedRegistry = Serialization.ReadValueOrThrow<ComponentRegistry>(mapping);

            Assert.That(deserializedRegistry.Count, Is.EqualTo(1));
            Assert.That(deserializedRegistry.ContainsKey("Test"));
            Assert.IsInstanceOf<TestComponent>(deserializedRegistry["Test"]);
        }
    }

    public class TestComponent : Component
    {
        public override string Name => "Test";
    }
}
