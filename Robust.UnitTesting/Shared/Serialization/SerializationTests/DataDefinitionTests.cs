using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

public sealed class DataDefinitionTests : SerializationTest
{
    //todo test that no references are wrongfully copied

    //includedatafields get their own tests (cts/regular), null out of the question since they get the og mapping

    //todo do customtypeserializers

    //for datafields of type primitive, struct, class, do:
    //read: null <> cts(v/s/m)/regular
    //write: null <> cts/regular
    //copy: null <> cts(cc(+nt), c)/regular(c)

    [CopyByRef]
    public struct DataDummyStruct : ISelfSerialize, IEquatable<DataDummyStruct>
    {
        public string Value;
        public void Deserialize(string value)
        {
            Value = value;
        }

        public string Serialize()
        {
            return Value;
        }

        public bool Equals(DataDummyStruct other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is DataDummyStruct other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    [CopyByRef]
    public class DataDummyClass : ISelfSerialize, IEquatable<DataDummyClass>
    {
        public string Value = string.Empty;
        public void Deserialize(string value)
        {
            Value = value;
        }

        public string Serialize()
        {
            return Value;
        }

        public bool Equals(DataDummyClass? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((DataDummyClass) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    [DataDefinition]
    public class DataDefTestDummy
    {
        [DataField("a")] public int a = Int32.MaxValue;
        [DataField("b")] public DataDummyStruct b = new(){Value = "default"};
        [DataField("c")] public DataDummyClass c = new(){Value = "default"};
        [DataField("na")] public int? na = Int32.MaxValue;
        [DataField("nb")] public DataDummyStruct? nb = new(){Value = "default"};
        [DataField("nc")] public DataDummyClass? nc = new(){Value = "default"};

        [DataField("ac")] public int ac = Int32.MaxValue;
        [DataField("bc")] public DataDummyStruct bc = new(){Value = "default"};
        [DataField("cc")] public DataDummyClass cc = new(){Value = "default"};
        [DataField("nac")] public int? nac = Int32.MaxValue;
        [DataField("nbc")] public DataDummyStruct? nbc = new(){Value = "default"};
        [DataField("ncc")] public DataDummyClass? ncc = new(){Value = "default"};
    }

    private static IEnumerable<object?[]> BaseFieldData = new[]
    {
        new object?[]
        {
            "a",
            new ValueDataNode("1"),
            () => 1,
            () => 2,
        },
        new object?[]
        {
            "b",
            new ValueDataNode("someValue"),
            () => new DataDummyStruct{Value = "someValue"},
            () => new DataDummyStruct{Value = "anotherValue"},
        },
        new object?[]
        {
            "c",
            new ValueDataNode("someValue"),
            () => new DataDummyClass{Value = "someValue"},
            () => new DataDummyClass{Value = "anotherValue"},
        }
    };

    public static IEnumerable<object?[]> NullableFieldsData => BaseFieldData.SelectMany(x =>
    {
        //nullable
        var nul = (object?[])x.Clone();
        nul[0] = $"n{nul[0]}";

        //nullable cts
        var nulcts = (object?[])x.Clone();
        nulcts[0] = $"n{nulcts[0]}c";

        return new[] { nul, nulcts };
    });

    public static IEnumerable<object?[]> RegularFieldsData => BaseFieldData.SelectMany(x =>
    {
        //regular cts
        var cts = (object?[])x.Clone();
        cts[0] = $"{cts[0]}c";

        return new[] { x, cts };
    });

    public static IEnumerable<object?[]> AllFieldsData =>
        new[] { NullableFieldsData, RegularFieldsData }.SelectMany(x => x);

    private object GetValue(DataDefTestDummy obj, string field) => obj.GetType().GetField(field)!.GetValue(obj)!;

    private void SetValue(DataDefTestDummy obj, string field, object? val) => obj.GetType().GetField(field)!.SetValue(obj, val);

    [TestCaseSource(nameof(NullableFieldsData))]
    public void Read_NT_NV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var mapping = new MappingDataNode{ { fieldName, ValueDataNode.Null() } };
        var res = Serialization.Read<DataDefTestDummy>(mapping);
        Assert.Null(GetValue(res, fieldName));
    }

    [TestCaseSource(nameof(AllFieldsData))]
    public void Read_NT_RV_AND_RT_RV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var mapping = new MappingDataNode{ { fieldName, node } };
        var res = Serialization.Read<DataDefTestDummy>(mapping);
        Assert.That(GetValue(res, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void Read_RT_NV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var mapping = new MappingDataNode{ { fieldName, ValueDataNode.Null() } };
        Assert.That(() => Serialization.Read<DataDefTestDummy>(mapping), Throws.InstanceOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void Write_NT_NV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var dataDef = new DataDefTestDummy();
        SetValue(dataDef, fieldName, null);
        var mapping = Serialization.WriteValueAs<MappingDataNode>(dataDef);
        Assert.That(mapping, Has.Count.EqualTo(1));
        Assert.That(mapping.Has(fieldName));
        Assert.That(mapping[fieldName], Is.TypeOf<ValueDataNode>());
        Assert.That(mapping.Get<ValueDataNode>(fieldName).IsNull);
    }

    [TestCaseSource(nameof(AllFieldsData))]
    public void Write_NT_RV_AND_RT_RV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var dataDef = new DataDefTestDummy();
        SetValue(dataDef, fieldName, value());
        var mapping = Serialization.WriteValueAs<MappingDataNode>(dataDef);
        Assert.That(mapping, Has.Count.EqualTo(1));
        Assert.That(mapping.Has(fieldName));
        Assert.That(mapping[fieldName], Is.TypeOf<ValueDataNode>());
        Assert.That(mapping[fieldName], Is.EqualTo(node));
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void Write_RT_NV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        //quick hack to ignore the fields where this isnt even possible to begin with. say it with me. we hate null we hate null we hate null -<paul
        if(typeof(T).IsValueType) return;

        var dataDef = new DataDefTestDummy();
        SetValue(dataDef, fieldName, null);
        Assert.That(() => Serialization.WriteValueAs<MappingDataNode>(dataDef), Throws.InstanceOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_NS_NT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, null);
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, null);
        Serialization.CopyTo(source, ref target);
        Assert.NotNull(target);
        Assert.Null(GetValue(target!, fieldName));
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_NS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, null);
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Serialization.CopyTo(source, ref target);
        Assert.NotNull(target);
        Assert.Null(GetValue(target!, fieldName));
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_RS_NT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, null);
        Serialization.CopyTo(source, ref target);
        Assert.NotNull(target);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_RS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Serialization.CopyTo(source, ref target);
        Assert.NotNull(target);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_NS_NT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        if(typeof(T).IsValueType) return;

        var source = new DataDefTestDummy();
        SetValue(source, fieldName, null);
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, null);
        Assert.That(() => Serialization.CopyTo(source, ref target), Throws.InstanceOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_NS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        if(typeof(T).IsValueType) return;

        var source = new DataDefTestDummy();
        SetValue(source, fieldName, null);
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Assert.That(() => Serialization.CopyTo(source, ref target), Throws.InstanceOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_RS_NT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        if(typeof(T).IsValueType) return;

        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, null);
        Serialization.CopyTo(source, ref target);
        Assert.NotNull(target);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_RS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        if(typeof(T).IsValueType) return;

        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Serialization.CopyTo(source, ref target);
        Assert.NotNull(target);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }
}
