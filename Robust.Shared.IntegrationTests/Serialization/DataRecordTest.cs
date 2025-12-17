using System.Numerics;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed partial class DataRecordTest : OurSerializationTest
{
    [DataRecord]
    public partial record TwoIntRecord(int aTest, int AnotherTest);

    [DataRecord]
    public partial record OneByteOneDefaultIntRecord(byte A, int B = 5);

    [DataRecord]
    public partial record OneLongRecord(long A);

    [DataRecord]
    public partial record OneLongDefaultRecord(long A = 5);

    [DataRecord]
    public partial record OneULongRecord(ulong A);

    [PrototypeRecord("emptyTestPrototypeRecord")]
    public partial record PrototypeRecord([field: IdDataField] string ID) : IPrototype;

    [DataRecord]
    public partial record IntStructHolder(IntStruct Struct);

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
    public partial record TwoIntStructHolder(IntStruct Struct1, IntStruct Struct2);

    [DataRecord]
    public partial record struct DataRecordStruct(IntStruct Struct, string String, int Integer);

    [DataRecord]
    public partial record struct DataRecordWithProperties
    {
        public Vector2 Position;
        public int Foo { get; }
        public int Bar { get; set; }
        public float X => Position.X;
    }

    [DataRecord]
    public readonly partial record struct ReadonlyDataRecord
    {
        public readonly Vector2 Position;
        public int Foo { get; }
        public float X => Position.X;
    }

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

    [Test]
    public void DataRecordWithPropertiesTest()
    {
        var mapping = new MappingDataNode
        {
            ["foo"] = new ValueDataNode("1"),
            ["bar"] = new ValueDataNode("2"),
            ["position"] = new ValueDataNode("3, .4"),
        };
        var val = Serialization.Read<DataRecordWithProperties>(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(val.Foo, Is.EqualTo(1));
            Assert.That(val.Bar, Is.EqualTo(2));
            Assert.That(val.Position, Is.EqualTo(new Vector2(3, .4f)));
        });

        var newMapping = Serialization.WriteValueAs<MappingDataNode>(val);

        Assert.Multiple(() =>
        {
            Assert.That(newMapping, Has.Count.EqualTo(3));
            Assert.That(newMapping.TryGet<ValueDataNode>("foo", out var node));
            Assert.That(node!.Value, Is.EqualTo("1"));

            Assert.That(newMapping.TryGet<ValueDataNode>("bar", out node));
            Assert.That(node!.Value, Is.EqualTo("2"));

            Assert.That(newMapping.TryGet<ValueDataNode>("position", out node));
            Assert.That(node!.Value, Is.EqualTo("3,0.4"));
        });
    }

    [Test]
    public void ReadonlyDataRecordTest()
    {
        var mapping = new MappingDataNode
        {
            ["foo"] = new ValueDataNode("1"),
            ["position"] = new ValueDataNode("3, .4"),
        };
        var val = Serialization.Read<ReadonlyDataRecord>(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(val.Foo, Is.EqualTo(1));
            Assert.That(val.Position, Is.EqualTo(new Vector2(3, .4f)));
        });

        var newMapping = Serialization.WriteValueAs<MappingDataNode>(val);

        Assert.Multiple(() =>
        {
            Assert.That(newMapping, Has.Count.EqualTo(2));
            Assert.That(newMapping.TryGet<ValueDataNode>("foo", out var node));
            Assert.That(node!.Value, Is.EqualTo("1"));

            Assert.That(newMapping.TryGet<ValueDataNode>("position", out node));
            Assert.That(node!.Value, Is.EqualTo("3,0.4"));
        });
    }
}
