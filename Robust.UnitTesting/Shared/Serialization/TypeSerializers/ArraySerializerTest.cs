using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Generic;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(ListSerializers<>))]
    public class ArraySerializerTest : TypeSerializerTest
    {
        [Test]
        public void SerializationTest()
        {
            var list = new[] {"A", "E"};
            var node = Serialization.WriteValueAs<SequenceDataNode>(list);

            Assert.That(node.Cast<ValueDataNode>(0).Value, Is.EqualTo("A"));
            Assert.That(node.Cast<ValueDataNode>(1).Value, Is.EqualTo("E"));
        }

        [Test]
        public void DeserializationTest()
        {
            var list = new[] {"A", "E"};
            var node = new SequenceDataNode("A", "E");
            var deserializedList = Serialization.ReadValue<string[]>(node);

            Assert.That(deserializedList, Is.EqualTo(list));
        }
    }
}
