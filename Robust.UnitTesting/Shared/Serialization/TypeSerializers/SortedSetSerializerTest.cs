using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(SortedSetSerializer<>))]
public sealed class SortedSetSerializerTest : SerializationTest
{
    [Test]
    public void SerializationTest()
    {
        var list = new SortedSet<string> {"A", "E", "B"};
        var node = Serialization.WriteValueAs<SequenceDataNode>(list);

        Assert.That(node.Cast<ValueDataNode>(0).Value, Is.EqualTo("A"));
        Assert.That(node.Cast<ValueDataNode>(1).Value, Is.EqualTo("E"));
        Assert.That(node.Cast<ValueDataNode>(2).Value, Is.EqualTo("B"));
    }

    [Test]
    public void DeserializationTest()
    {
        var list = new SortedSet<string> {"A", "E", "B"};
        var node = new SequenceDataNode("A", "E", "B");
        var deserializedList = Serialization.Read<SortedSet<string>>(node, notNullableOverride: true);

        Assert.That(deserializedList, Is.EqualTo(list));
    }
}
