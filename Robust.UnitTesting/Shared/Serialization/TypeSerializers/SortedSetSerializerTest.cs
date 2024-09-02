using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(SortedSetSerializer<>))]
public sealed class SortedSetSerializerTest : SerializationTest
{
    [Test]
    public void SerializationTest()
    {
        var list = new SortedSet<string> {"A", "B", "C"};
        var node = Serialization.WriteValueAs<SequenceDataNode>(list);

        Assert.That(node.Cast<ValueDataNode>(0).Value, Is.EqualTo("A"));
        Assert.That(node.Cast<ValueDataNode>(1).Value, Is.EqualTo("B"));
        Assert.That(node.Cast<ValueDataNode>(2).Value, Is.EqualTo("C"));
    }

    [Test]
    public void DeserializationTest()
    {
        var list = new SortedSet<string> {"A", "B", "C"};
        var node = new SequenceDataNode("A", "B", "C");
        var deserializedList = Serialization.Read<SortedSet<string>>(node, notNullableOverride: true);

        Assert.That(deserializedList, Is.EqualTo(list));
    }
}
