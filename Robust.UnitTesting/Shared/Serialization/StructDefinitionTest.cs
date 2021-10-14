using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    public class StructDefinitionTest : SerializationTest
    {
        [Test]
        public void Test()
        {
            var mapping = new MappingDataNode();
            mapping.Add("A", new ValueDataNode("5"));
            mapping.Add("B", new ValueDataNode("honk"));

            var definition = Serialization.ReadValue<Struct>(mapping);

            Assert.That(definition.A, Is.EqualTo(5));
            Assert.That(definition.B, Is.EqualTo("honk"));
        }

        [DataDefinition]
        public struct Struct
        {
            [DataField("A")] public int A { get; set; }
            [DataField("B")] public string B { get; set; }
        }
    }
}
