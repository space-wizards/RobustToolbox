using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed partial class DataRecordTest : SerializationTest
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

    [PrototypeRecord("emptyTestPrototypeRecord")]
    public record PrototypeRecord([field: IdDataField] string ID) : IPrototype;

    [DataRecord]
    public record IntStructHolder(IntStruct Struct);

    [DataDefinition]
    public partial struct IntStruct
    {
        [DataField("value")] public int Value;

        public IntStruct(int value)
        {
            Value = value;
        }
    }

    [DataRecord]
    public record TwoIntStructHolder(IntStruct Struct1, IntStruct Struct2);

    [DataRecord]
    public record struct DataRecordStruct(IntStruct Struct, string String, int Integer);

    [Test]
    public void TwoIntRecordTest()
    {
        var mapping = new MappingDataNode
        {
            {"aTest", "1"},
            {"anotherTest", "2"}
        };

        var val = Serialization.Read<TwoIntRecord>(mapping, notNullableOverride: true);

        Assert.Multiple(() =>
        {
            Assert.That(val.aTest, Is.EqualTo(1));
            Assert.That(val.AnotherTest, Is.EqualTo(2));
        });

        var newMapping = Serialization.WriteValueAs<MappingDataNode>(val);

        Assert.Multiple(() =>
        {
            Assert.That(newMapping, Has.Count.EqualTo(2));

            Assert.That(newMapping.TryGet<ValueDataNode>("aTest", out var aTestNode));
            Assert.That(aTestNode!.Value, Is.EqualTo("1"));

            Assert.That(newMapping.TryGet<ValueDataNode>("anotherTest", out var anotherTestNode));
            Assert.That(anotherTestNode!.Value, Is.EqualTo("2"));
        });
    }

    [Test]
    public void OneByteOneDefaultIntRecordTest()
    {
        var mapping = new MappingDataNode {{"a", "1"}};
        var val = Serialization.Read<OneByteOneDefaultIntRecord>(mapping, notNullableOverride: true);

        Assert.Multiple(() =>
        {
            Assert.That(val.A, Is.EqualTo(1));
            Assert.That(val.B, Is.EqualTo(5));
        });
    }

    [Test]
    public void OneLongRecordTest()
    {
        var mapping = new MappingDataNode {{"a", "1"}};
        var val = Serialization.Read<OneLongRecord>(mapping, notNullableOverride: true);

        Assert.That(val.A, Is.EqualTo(1));
    }

    [Test]
    public void OneLongMinValueRecordTest()
    {
        var mapping = new MappingDataNode {{"a", long.MinValue.ToString()}};
        var val = Serialization.Read<OneLongRecord>(mapping, notNullableOverride: true);

        Assert.That(val.A, Is.EqualTo(long.MinValue));
    }

    [Test]
    public void OneLongMaxValueRecordTest()
    {
        var mapping = new MappingDataNode {{"a", long.MaxValue.ToString()}};
        var val = Serialization.Read<OneLongRecord>(mapping, notNullableOverride: true);

        Assert.That(val.A, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void OneLongDefaultRecordTest()
    {
        var mapping = new MappingDataNode();
        var val = Serialization.Read<OneLongDefaultRecord>(mapping, notNullableOverride: true);

        Assert.That(val.A, Is.EqualTo(5));
    }

    [Test]
    public void OneULongRecordMaxValueTest()
    {
        var mapping = new MappingDataNode {{"a", ulong.MaxValue.ToString()}};
        var val = Serialization.Read<OneULongRecord>(mapping, notNullableOverride: true);

        Assert.That(val.A, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void PrototypeTest()
    {
        var mapping = new MappingDataNode {{"id", "ABC"}};
        var val = Serialization.Read<PrototypeRecord>(mapping, notNullableOverride: true);

        Assert.That(val.ID, Is.EqualTo("ABC"));
    }

    [Test]
    public void RegisterPrototypeTest()
    {
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        prototypes.Initialize();

        Assert.That(prototypes.HasKind("emptyTestPrototypeRecord"), Is.True);
    }

    [Test]
    public void IntStructHolderTest()
    {
        var mapping = new MappingDataNode
        {
            {
                "struct", new MappingDataNode
                {
                    {"value", "42"}
                }
            }
        };
        var val = Serialization.Read<IntStructHolder>(mapping, notNullableOverride: true);

        Assert.That(val.Struct.Value, Is.EqualTo(42));
    }

    [Test]
    public void TwoIntStructHolderTest()
    {
        var mapping = new MappingDataNode
        {
            {
                "struct1", new MappingDataNode
                {
                    {"value", "5"}
                }
            },
            {
                "struct2", new MappingDataNode
                {
                    {"value", "10"}
                }
            }
        };
        var val = Serialization.Read<TwoIntStructHolder>(mapping, notNullableOverride: true);

        Assert.Multiple(() =>
        {
            Assert.That(val.Struct1.Value, Is.EqualTo(5));
            Assert.That(val.Struct2.Value, Is.EqualTo(10));
        });
    }

    [Test]
    public void DataRecordStructTest()
    {
        var mapping = new MappingDataNode
        {
            {
                "struct", new MappingDataNode
                {
                    {"value", "1"}
                }
            },
            {
                "string", new ValueDataNode("A")
            },
            {
                "integer", new ValueDataNode("2")
            }
        };
        var val = Serialization.Read<DataRecordStruct>(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(val.Struct.Value, Is.EqualTo(1));
            Assert.That(val.String, Is.EqualTo("A"));
            Assert.That(val.Integer, Is.EqualTo(2));
        });

        var newMapping = Serialization.WriteValueAs<MappingDataNode>(val);

        Assert.Multiple(() =>
        {
            Assert.That(newMapping, Has.Count.EqualTo(3));

            Assert.That(newMapping.TryGet<MappingDataNode>("struct", out var structNode));
            Assert.That(structNode, Has.Count.EqualTo(1));
            Assert.That(structNode!.TryGet<ValueDataNode>("value", out var structValueNode));
            Assert.That(structValueNode!.Value, Is.EqualTo("1"));

            Assert.That(newMapping.TryGet<ValueDataNode>("string", out var stringNode));
            Assert.That(stringNode!.Value, Is.EqualTo("A"));

            Assert.That(newMapping.TryGet<ValueDataNode>("integer", out var integerNode));
            Assert.That(integerNode!.Value, Is.EqualTo("2"));
        });
    }
}
