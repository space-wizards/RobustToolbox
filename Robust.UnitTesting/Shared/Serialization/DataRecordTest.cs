using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed class DataRecordTest : SerializationTest
{
    [DataRecord]
    public record TwoIntRecord(int aTest, int AnotherTest);

    [DataRecord]
    public record OneByteOneDefaultIntRecord(byte A, int B = 5);

    [DataRecord]
    public record OneLongRecord(long A);

    [DataRecord]
    public record OneLongDefaultRecord(long A = 5);

    [DataRecord]
    public record OneULongRecord(ulong A);

    [Test]
    public void TwoIntRecordTest()
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
    public void OneByteOneDefaultIntRecordTest()
    {
        var mapping = new MappingDataNode {{"a", "1"}};

        var val = Serialization.Read<OneByteOneDefaultIntRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(1));
        Assert.That(val.B, Is.EqualTo(5));
    }

    [Test]
    public void OneLongRecordTest()
    {
        var mapping = new MappingDataNode {{"a", "1"}};

        var val = Serialization.Read<OneLongRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(1));
    }

    [Test]
    public void OneLongMinValueRecordTest()
    {
        var mapping = new MappingDataNode {{"a", long.MinValue.ToString()}};

        var val = Serialization.Read<OneLongRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(long.MinValue));
    }

    [Test]
    public void OneLongMaxValueRecordTest()
    {
        var mapping = new MappingDataNode {{"a", long.MaxValue.ToString()}};

        var val = Serialization.Read<OneLongRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void OneLongDefaultRecordTest()
    {
        var mapping = new MappingDataNode();

        var val = Serialization.Read<OneLongDefaultRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(5));
    }

    [Test]
    public void OneULongRecordMaxValueTest()
    {
        var mapping = new MappingDataNode {{"a", ulong.MaxValue.ToString()}};

        var val = Serialization.Read<OneULongRecord>(mapping);

        Assert.That(val.A, Is.EqualTo(ulong.MaxValue));
    }
}
