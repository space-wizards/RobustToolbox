using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed class DataRecordTest : SerializationTest
{
    [DataRecord]
    public record TwoIntRecord(ulong aTest, int AnotherTest);

    [DataRecord]
    public record TwoIntOneDefaultRecord(byte A, int B = 5);

    [Test]
    public void ReadWrite()
    {
        var mapping = new MappingDataNode
        {
            {"aTest", "1"},
            {"anotherTest", "2"}
        };

        var val = Serialization.Read<TwoIntRecord>(mapping);

        Assert.That(val.aTest, Is.EqualTo(1));
        Assert.That(val.AnotherTest, Is.EqualTo(2));

        var newMapping = Serialization.WriteValueAs<MappingDataNode>(val);

        Assert.That(newMapping.Count, Is.EqualTo(2));
        Assert.That(newMapping.TryGet<ValueDataNode>("aTest", out var node1));
        Assert.That(node1!.Value, Is.EqualTo("1"));
        Assert.That(newMapping.TryGet<ValueDataNode>("anotherTest", out var node2));
        Assert.That(node2!.Value.ToLower(), Is.EqualTo("2"));
    }

    [Test]
    public void DefaultValues()
    {
        var mapping = new MappingDataNode {{"a", "1"}};

        var val = Serialization.Read<TwoIntOneDefaultRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(1));
        Assert.That(val.B, Is.EqualTo(5));
    }
}
