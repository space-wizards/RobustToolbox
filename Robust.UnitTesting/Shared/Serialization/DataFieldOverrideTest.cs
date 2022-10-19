using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed class DataFieldOverrideTest : SerializationTest
{
    [Virtual, DataDefinition]
    private class TestBaseType
    {
        [DataField("fieldTest", readOnly: true)]
        public virtual int TestField { get; }
    }

    [DataDefinition]
    private sealed class TestInheritor : TestBaseType
    {
        [DataField("anotherName", readOnly: false)]
        public override int TestField { get; }
    }

    [Test]
    public void TwoIntRecordTest()
    {
        var mapping = new MappingDataNode
        {
            {"fieldTest", "1"},
            {"anotherName", "2"}
        };

        var val1 = Serialization.Read<TestBaseType>(mapping);

        Assert.That(val1.TestField, Is.EqualTo(1));

        var val2 = Serialization.Read<TestInheritor>(mapping);

        Assert.That(val2.TestField, Is.EqualTo(2));

        var node1 = Serialization.WriteValue(val1);

        Assert.That(((MappingDataNode)node1).Count, Is.EqualTo(0));

        var node2 = Serialization.WriteValue(val2);

        Assert.That(((MappingDataNode)node2).TryGet<ValueDataNode>("anotherName", out var writtenVal));
        Assert.That(writtenVal!.Value, Is.EqualTo("2"));
    }
}
