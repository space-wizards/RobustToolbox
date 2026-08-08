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
    public partial record struct TwoIntRecordStruct(int aTest, int AnotherTest);

    [DataRecord]
    public partial record PrimitiveRecord(
        bool Bool,
        byte Byte,
        sbyte Sbyte,
        char Char,
        //decimal Decimal,
        double Double,
        float Float,
        int Int,
        uint Uint,
        nint Nint,
        nuint Nuint,
        long Long,
        ulong Ulong,
        short Short,
        ushort UShort);

    [DataRecord]
    public partial record struct PrimitiveRecordStruct(
        bool Bool,
        byte Byte,
        sbyte Sbyte,
        char Char,
        //decimal Decimal,
        double Double,
        float Float,
        int Int,
        uint Uint,
        nint Nint,
        nuint Nuint,
        long Long,
        ulong Ulong,
        short Short,
        ushort UShort);

    [DataRecord]
    public partial record PrimitiveDefaultsRecord(
        bool Bool = true,
        byte Byte = byte.MaxValue,
        sbyte Sbyte = sbyte.MinValue,
        char Char = 'A',
        //decimal Decimal = -1,
        double Double = -1d,
        float Float = -1f,
        int Int = int.MinValue,
        uint Uint = uint.MaxValue,
        nint Nint = int.MinValue,
        nuint Nuint = uint.MaxValue,
        long Long = long.MinValue,
        ulong Ulong = ulong.MaxValue,
        short Short = short.MinValue,
        ushort UShort = ushort.MaxValue);

    [DataRecord]
    public partial record struct PrimitiveDefaultsRecordStruct(
        bool Bool = true,
        byte Byte = byte.MaxValue,
        sbyte Sbyte = sbyte.MinValue,
        char Char = 'A',
        //decimal Decimal = -1,
        double Double = -1d,
        float Float = -1f,
        int Int = int.MinValue,
        uint Uint = uint.MaxValue,
        nint Nint = int.MinValue,
        nuint Nuint = uint.MaxValue,
        long Long = long.MinValue,
        ulong Ulong = ulong.MaxValue,
        short Short = short.MinValue,
        ushort UShort = ushort.MaxValue);

    [PrototypeRecord("emptyTestPrototypeRecord")]
    public partial record PrototypeRecord([field: IdDataField] string ID) : IPrototype;

    [DataRecord]
    public partial record IntStructHolder(IntStruct Struct);

    [DataRecord]
    public partial record struct IntStructHolderStruct(IntStruct Struct);

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
    public partial record struct TwoIntStructHolderStruct(IntStruct Struct1, IntStruct Struct2);

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
    public partial record struct DataRecordWithDefaultFields()
    {
        public int A = 1;
    }

    [DataRecord]
    public partial record struct DataRecordWithDefaultFields2(int A = 1)
    {
        public int B = 2;
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
    public void TwoIntRecordStructTest()
    {
        var mapping = new MappingDataNode
        {
            {"aTest", "1"},
            {"anotherTest", "2"}
        };

        var val = Serialization.Read<TwoIntRecordStruct>(mapping);

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
    public void PrimitiveRecordTest()
    {
        var mapping = new MappingDataNode();
        var val1 = Serialization.Read<PrimitiveRecord>(mapping, notNullableOverride: true);
        var val2 = Serialization.Read<PrimitiveRecordStruct>(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(val1.Bool, Is.EqualTo(false));
            Assert.That(val2.Bool, Is.EqualTo(false));
            Assert.That(val1.Byte, Is.EqualTo(0));
            Assert.That(val2.Byte, Is.EqualTo(0));
            Assert.That(val1.Sbyte, Is.EqualTo(0));
            Assert.That(val2.Sbyte, Is.EqualTo(0));
            Assert.That(val1.Char, Is.EqualTo(default(char)));
            Assert.That(val2.Char, Is.EqualTo(default(char)));
            //Assert.That(val1.Decimal, Is.EqualTo(0));
            //Assert.That(val2.Decimal, Is.EqualTo(0));
            Assert.That(val1.Double, Is.EqualTo(0));
            Assert.That(val2.Double, Is.EqualTo(0));
            Assert.That(val1.Float, Is.EqualTo(0));
            Assert.That(val2.Float, Is.EqualTo(0));
            Assert.That(val1.Int, Is.EqualTo(0));
            Assert.That(val2.Int, Is.EqualTo(0));
            Assert.That(val1.Uint, Is.EqualTo(0));
            Assert.That(val2.Uint, Is.EqualTo(0));
            Assert.That(val1.Nint, Is.EqualTo((nint) 0));
            Assert.That(val2.Nint, Is.EqualTo((nint) 0));
            Assert.That(val1.Nuint, Is.EqualTo((nuint) 0));
            Assert.That(val2.Nuint, Is.EqualTo((nuint) 0));
            Assert.That(val1.Long, Is.EqualTo(0));
            Assert.That(val2.Long, Is.EqualTo(0));
            Assert.That(val1.Ulong, Is.EqualTo(0));
            Assert.That(val2.Ulong, Is.EqualTo(0));
            Assert.That(val1.Short, Is.EqualTo(0));
            Assert.That(val2.Short, Is.EqualTo(0));
            Assert.That(val1.UShort, Is.EqualTo(0));
            Assert.That(val2.UShort, Is.EqualTo(0));
        });
    }

    [Test]
    public void PrimitiveDefaultsRecordTest()
    {
        var mapping = new MappingDataNode();
        var val1 = Serialization.Read<PrimitiveDefaultsRecord>(mapping, notNullableOverride: true);
        var val2 = Serialization.Read<PrimitiveDefaultsRecordStruct>(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(val1.Bool, Is.EqualTo(true));
            Assert.That(val2.Bool, Is.EqualTo(true));
            Assert.That(val1.Byte, Is.EqualTo(byte.MaxValue));
            Assert.That(val2.Byte, Is.EqualTo(byte.MaxValue));
            Assert.That(val1.Sbyte, Is.EqualTo(sbyte.MinValue));
            Assert.That(val2.Sbyte, Is.EqualTo(sbyte.MinValue));
            Assert.That(val1.Char, Is.EqualTo('A'));
            Assert.That(val2.Char, Is.EqualTo('A'));
            //Assert.That(val1.Decimal, Is.EqualTo(-1));
            //Assert.That(val2.Decimal, Is.EqualTo(-1));
            Assert.That(val1.Double, Is.EqualTo(-1));
            Assert.That(val2.Double, Is.EqualTo(-1));
            Assert.That(val1.Float, Is.EqualTo(-1));
            Assert.That(val2.Float, Is.EqualTo(-1));
            Assert.That(val1.Int, Is.EqualTo(int.MinValue));
            Assert.That(val2.Int, Is.EqualTo(int.MinValue));
            Assert.That(val1.Uint, Is.EqualTo(uint.MaxValue));
            Assert.That(val2.Uint, Is.EqualTo(uint.MaxValue));
            Assert.That(val1.Nint, Is.EqualTo((nint) int.MinValue));
            Assert.That(val2.Nint, Is.EqualTo((nint) int.MinValue));
            Assert.That(val1.Nuint, Is.EqualTo((nuint) uint.MaxValue));
            Assert.That(val2.Nuint, Is.EqualTo((nuint) uint.MaxValue));
            Assert.That(val1.Long, Is.EqualTo(long.MinValue));
            Assert.That(val2.Long, Is.EqualTo(long.MinValue));
            Assert.That(val1.Ulong, Is.EqualTo(ulong.MaxValue));
            Assert.That(val2.Ulong, Is.EqualTo(ulong.MaxValue));
            Assert.That(val1.Short, Is.EqualTo(short.MinValue));
            Assert.That(val2.Short, Is.EqualTo(short.MinValue));
            Assert.That(val1.UShort, Is.EqualTo(ushort.MaxValue));
            Assert.That(val2.UShort, Is.EqualTo(ushort.MaxValue));
        });
    }

    [Test]
    public void PrimitiveRecordMinMaxValueTest()
    {
        var mapping = new MappingDataNode
        {
            {"bool", "true"},
            {"byte", byte.MaxValue.ToString()},
            {"sbyte", sbyte.MinValue.ToString()},
            {"char", "A"},
            //{"decimal", "-1"},
            {"double", "-1"},
            {"float", "-1"},
            {"int", int.MinValue.ToString()},
            {"uint", uint.MaxValue.ToString()},
            // TODO SERIALIZATION add nint yaml serializer?
            //{"nint", nint.MinValue.ToString()},
            //{"nuint", nuint.MinValue.ToString()},
            {"long", long.MinValue.ToString()},
            {"ulong", ulong.MaxValue.ToString()},
            {"short", short.MinValue.ToString()},
            {"ushort", ushort.MaxValue.ToString()},
        };
        var val1 = Serialization.Read<PrimitiveDefaultsRecord>(mapping, notNullableOverride: true);
        var val2 = Serialization.Read<PrimitiveDefaultsRecordStruct>(mapping);

        Assert.Multiple(() =>
        {
            Assert.That(val1.Bool, Is.EqualTo(true));
            Assert.That(val2.Bool, Is.EqualTo(true));
            Assert.That(val1.Byte, Is.EqualTo(byte.MaxValue));
            Assert.That(val2.Byte, Is.EqualTo(byte.MaxValue));
            Assert.That(val1.Sbyte, Is.EqualTo(sbyte.MinValue));
            Assert.That(val2.Sbyte, Is.EqualTo(sbyte.MinValue));
            Assert.That(val1.Char, Is.EqualTo('A'));
            Assert.That(val2.Char, Is.EqualTo('A'));
            //Assert.That(val1.Decimal, Is.EqualTo(-1));
            //Assert.That(val2.Decimal, Is.EqualTo(-1));
            Assert.That(val1.Double, Is.EqualTo(-1));
            Assert.That(val2.Double, Is.EqualTo(-1));
            Assert.That(val1.Float, Is.EqualTo(-1));
            Assert.That(val2.Float, Is.EqualTo(-1));
            Assert.That(val1.Int, Is.EqualTo(int.MinValue));
            Assert.That(val2.Int, Is.EqualTo(int.MinValue));
            Assert.That(val1.Uint, Is.EqualTo(uint.MaxValue));
            Assert.That(val2.Uint, Is.EqualTo(uint.MaxValue));
            //Assert.That(val1.Nint, Is.EqualTo(nint.MinValue));
            //Assert.That(val2.Nint, Is.EqualTo(nint.MinValue));
            //Assert.That(val1.Nuint, Is.EqualTo(nuint.MaxValue));
            //Assert.That(val2.Nuint, Is.EqualTo(nuint.MaxValue));
            Assert.That(val1.Long, Is.EqualTo(long.MinValue));
            Assert.That(val2.Long, Is.EqualTo(long.MinValue));
            Assert.That(val1.Ulong, Is.EqualTo(ulong.MaxValue));
            Assert.That(val2.Ulong, Is.EqualTo(ulong.MaxValue));
            Assert.That(val1.Short, Is.EqualTo(short.MinValue));
            Assert.That(val2.Short, Is.EqualTo(short.MinValue));
            Assert.That(val1.UShort, Is.EqualTo(ushort.MaxValue));
            Assert.That(val2.UShort, Is.EqualTo(ushort.MaxValue));
        });
    }

    [Test]
    public void PrototypeTest()
    {
        var mapping = new MappingDataNode {{"id", "ABC"}};
        var val = Serialization.Read<PrototypeRecord>(mapping, notNullableOverride: true);

        Assert.That(val.ID, Is.EqualTo("ABC"));
    }

    [Test]
    public void DataRecordWithDefaultFieldsTest()
    {
        var mapping = new MappingDataNode ();
        var val = Serialization.Read<DataRecordWithDefaultFields>(mapping);
        Assert.That(val.A, Is.EqualTo(1));

        var val2 = Serialization.Read<DataRecordWithDefaultFields2>(mapping);
        Assert.That(val2.A, Is.EqualTo(1));
        Assert.That(val2.B, Is.EqualTo(2));
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
        var structVal = Serialization.Read<IntStructHolderStruct>(mapping);

        Assert.That(val.Struct.Value, Is.EqualTo(42));
        Assert.That(structVal.Struct.Value, Is.EqualTo(42));
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
