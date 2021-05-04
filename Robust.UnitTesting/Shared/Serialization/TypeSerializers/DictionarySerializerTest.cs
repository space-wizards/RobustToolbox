using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(DictionarySerializer<,>))]
    public class DictionarySerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            var dictionary = new Dictionary<int, string>
            {
                [1] = "A",
                [2] = "B",
                [3] = "C"
            };
            var node = Serialization.WriteValueAs<MappingDataNode>(dictionary);

            Assert.That(node.Cast<ValueDataNode>("1").Value, Is.EqualTo("A"));
            Assert.That(node.Cast<ValueDataNode>("2").Value, Is.EqualTo("B"));
            Assert.That(node.Cast<ValueDataNode>("3").Value, Is.EqualTo("C"));
        }

        [Test]
        public void DeserializationTest()
        {
            var dictionary = new Dictionary<int, string>
            {
                [1] = "A",
                [2] = "B",
                [3] = "C"
            };
            var node = new MappingDataNode();

            node.AddNode("1", new ValueDataNode("A"));
            node.AddNode("2", new ValueDataNode("B"));
            node.AddNode("3", new ValueDataNode("C"));

            var deserializedDictionary = Serialization.ReadValue<Dictionary<int, string>>(node);

            Assert.That(deserializedDictionary, Is.EqualTo(dictionary));
        }
    }
}
