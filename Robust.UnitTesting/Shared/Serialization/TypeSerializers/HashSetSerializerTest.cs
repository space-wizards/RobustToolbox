using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(HashSetSerializer<>))]
    public sealed class HashSetSerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            var list = new HashSet<string> {"A", "E"};
            var node = Serialization.WriteValueAs<SequenceDataNode>(list);

            Assert.That(node.Cast<ValueDataNode>(0).Value, Is.EqualTo("A"));
            Assert.That(node.Cast<ValueDataNode>(1).Value, Is.EqualTo("E"));
        }

        [Test]
        public void DeserializationTest()
        {
            var list = new HashSet<string> {"A", "E"};
            var node = new SequenceDataNode("A", "E");
            var deserializedList = Serialization.ReadValue<HashSet<string>>(node);

            Assert.That(deserializedList, Is.EqualTo(list));
        }
    }
}
