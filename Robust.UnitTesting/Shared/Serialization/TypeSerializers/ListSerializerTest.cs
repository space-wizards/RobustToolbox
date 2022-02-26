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
    [TestOf(typeof(ListSerializers<>))]
    public sealed class ListSerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            var list = new List<string> {"A", "E"};
            var node = Serialization.WriteValueAs<SequenceDataNode>(list);

            Assert.That(node.Cast<ValueDataNode>(0).Value, Is.EqualTo("A"));
            Assert.That(node.Cast<ValueDataNode>(1).Value, Is.EqualTo("E"));
        }

        [Test]
        public void DeserializationTest()
        {
            var list = new List<string> {"A", "E"};
            var node = new SequenceDataNode("A", "E");
            var deserializedList = Serialization.Read<List<string>>(node);

            Assert.That(deserializedList, Is.EqualTo(list));
        }

        [Test]
        public void CustomReadTest()
        {
            var node = new SequenceDataNode("A", "E");

            var result = Serialization.ReadWithTypeSerializer(typeof(List<string>), typeof(ListSerializers<string>), node);
            var list = (List<string>?) result.RawValue;

            Assert.NotNull(list);
            Assert.IsNotEmpty(list!);
            Assert.That(list, Has.Count.EqualTo(2));
            Assert.That(list, Does.Contain("A"));
            Assert.That(list, Does.Contain("E"));
        }

        [Test]
        public void CustomWriteTest()
        {
            var list = new List<string> {"A", "E"};

            var node = (SequenceDataNode) Serialization.WriteWithTypeSerializer(typeof(List<string>), typeof(ListSerializers<string>), list);

            Assert.That(node.Sequence.Count, Is.EqualTo(2));
            Assert.That(node.Cast<ValueDataNode>(0).Value, Is.EqualTo("A"));
            Assert.That(node.Cast<ValueDataNode>(1).Value, Is.EqualTo("E"));
        }

        [Test]
        public void CustomCopyTest()
        {
            var source = new List<string> {"A", "E"};
            var target = new List<string>();

            Assert.IsNotEmpty(source);
            Assert.IsEmpty(target);

            var copy = (List<string>?) Serialization.CopyWithTypeSerializer(typeof(ListSerializers<string>), source, target);

            Assert.NotNull(copy);

            Assert.IsNotEmpty(copy!);
            Assert.IsNotEmpty(target);

            Assert.That(copy, Is.EqualTo(target));

            Assert.That(copy!.Count, Is.EqualTo(2));
            Assert.That(target.Count, Is.EqualTo(2));

            Assert.That(copy, Does.Contain("A"));
            Assert.That(copy, Does.Contain("E"));

            Assert.That(target, Does.Contain("A"));
            Assert.That(target, Does.Contain("E"));
        }
    }
}
