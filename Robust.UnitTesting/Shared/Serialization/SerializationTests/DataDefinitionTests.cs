using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

public sealed partial class DataDefinitionTests : SerializationTest
{
    //todo test that no references are wrongfully copied

    //includedatafields get their own tests (cts/regular), null out of the question since they get the og mapping

    //for datafields of type primitive, struct, class, do:
    //read: null <> cts(v/s/m)/regular
    //write: null <> cts/regular
    //copy: null <> cts(cc(+nt), c)/regular(c)

    [DataDefinition]
    public sealed partial class DataDefTestDummy
    {
        [DataField("a")] public int a = Int32.MaxValue;
        [DataField("b")] public DataDummyStruct b = new(){Value = "default"};
        [DataField("c")] public DataDummyClass c = new(){Value = "default"};
        [DataField("na")] public int? na = Int32.MaxValue;
        [DataField("nb")] public DataDummyStruct? nb = new(){Value = "default"};
        [DataField("nc")] public DataDummyClass? nc = new(){Value = "default"};

        [DataField("acv", customTypeSerializer:typeof(DataDefinitionValueCustomTypeSerializer))] public int acv = Int32.MaxValue;
        [DataField("bcv", customTypeSerializer:typeof(DataDefinitionValueCustomTypeSerializer))] public DataDummyStruct bcv = new(){Value = "default"};
        [DataField("ccv", customTypeSerializer:typeof(DataDefinitionValueCustomTypeSerializer))] public DataDummyClass ccv = new(){Value = "default"};
        [DataField("nacv", customTypeSerializer:typeof(DataDefinitionValueCustomTypeSerializer))] public int? nacv = Int32.MaxValue;
        [DataField("nbcv", customTypeSerializer:typeof(DataDefinitionValueCustomTypeSerializer))] public DataDummyStruct? nbcv = new(){Value = "default"};
        [DataField("nccv", customTypeSerializer:typeof(DataDefinitionValueCustomTypeSerializer))] public DataDummyClass? nccv = new(){Value = "default"};

        [DataField("acs", customTypeSerializer:typeof(DataDefinitionSequenceCustomTypeSerializer))] public int acs = Int32.MaxValue;
        [DataField("bcs", customTypeSerializer:typeof(DataDefinitionSequenceCustomTypeSerializer))] public DataDummyStruct bcs = new(){Value = "default"};
        [DataField("ccs", customTypeSerializer:typeof(DataDefinitionSequenceCustomTypeSerializer))] public DataDummyClass ccs = new(){Value = "default"};
        [DataField("nacs", customTypeSerializer:typeof(DataDefinitionSequenceCustomTypeSerializer))] public int? nacs = Int32.MaxValue;
        [DataField("nbcs", customTypeSerializer:typeof(DataDefinitionSequenceCustomTypeSerializer))] public DataDummyStruct? nbcs = new(){Value = "default"};
        [DataField("nccs", customTypeSerializer:typeof(DataDefinitionSequenceCustomTypeSerializer))] public DataDummyClass? nccs = new(){Value = "default"};

        [DataField("acm", customTypeSerializer:typeof(DataDefinitionMappingCustomTypeSerializer))] public int acm = Int32.MaxValue;
        [DataField("bcm", customTypeSerializer:typeof(DataDefinitionMappingCustomTypeSerializer))] public DataDummyStruct bcm = new(){Value = "default"};
        [DataField("ccm", customTypeSerializer:typeof(DataDefinitionMappingCustomTypeSerializer))] public DataDummyClass ccm = new(){Value = "default"};
        [DataField("nacm", customTypeSerializer:typeof(DataDefinitionMappingCustomTypeSerializer))] public int? nacm = Int32.MaxValue;
        [DataField("nbcm", customTypeSerializer:typeof(DataDefinitionMappingCustomTypeSerializer))] public DataDummyStruct? nbcm = new(){Value = "default"};
        [DataField("nccm", customTypeSerializer:typeof(DataDefinitionMappingCustomTypeSerializer))] public DataDummyClass? nccm = new(){Value = "default"};
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

    private static Dictionary<Type, object> _returnMap => new()
    {
        { typeof(int), static () => SerializerReturnInt },
        { typeof(DataDummyStruct), static () => SerializerReturnStruct },
        { typeof(DataDummyClass), static () => SerializerReturnClass },
    };

    public static IEnumerable<object?[]> NullableFieldsData => BaseFieldData.SelectMany(x =>
    {
        var type = x[2]!.GetType().GetGenericArguments()[0];

        //nullable
        var nul = (object?[])x.Clone();
        nul[0] = $"n{nul[0]}";

        //nullable cts
        var nulctsv = (object?[])x.Clone();
        nulctsv[0] = $"n{nulctsv[0]}cv";
        nulctsv[1] = SerializerValueDataNode;
        nulctsv[2] = _returnMap[type];

        var nulctss = (object?[])x.Clone();
        nulctss[0] = $"n{nulctss[0]}cs";
        nulctss[1] = SerializerSequenceDataNode;
        nulctss[2] = _returnMap[type];

        var nulctsm = (object?[])x.Clone();
        nulctsm[0] = $"n{nulctsm[0]}cm";
        nulctsm[1] = SerializerMappingDataNode;
        nulctsm[2] = _returnMap[type];

        return new[] { nul, nulctsv, nulctss, nulctsm };
    });

    public static IEnumerable<object?[]> RegularFieldsData => BaseFieldData.SelectMany(x =>
    {
        var type = x[2]!.GetType().GetGenericArguments()[0];

        //regular cts
        var ctsv = (object?[])x.Clone();
        ctsv[0] = $"{ctsv[0]}cv";
        ctsv[1] = SerializerValueDataNode;
        ctsv[2] = _returnMap[type];

        var ctss = (object?[])x.Clone();
        ctss[0] = $"{ctss[0]}cs";
        ctss[1] = SerializerSequenceDataNode;
        ctss[2] = _returnMap[type];

        var ctsm = (object?[])x.Clone();
        ctsm[0] = $"{ctsm[0]}cm";
        ctsm[1] = SerializerMappingDataNode;
        ctsm[2] = _returnMap[type];

        return new[] { x, ctsv, ctss, ctsm };
    });

    public static IEnumerable<object?[]> AllFieldsData =>
        new[] { NullableFieldsData, RegularFieldsData }.SelectMany(x => x);

    private object GetValue(DataDefTestDummy obj, string field) => obj.GetType().GetField(field)!.GetValue(obj)!;

    private void SetValue(DataDefTestDummy obj, string field, object? val) => obj.GetType().GetField(field)!.SetValue(obj, val);

    [TestCaseSource(nameof(NullableFieldsData))]
    public void Read_NT_NV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var mapping = new MappingDataNode{ { fieldName, ValueDataNode.Null() } };
        var res = Serialization.Read<DataDefTestDummy>(mapping, notNullableOverride: true);
        Assert.That(GetValue(res, fieldName), Is.Null);
    }

    [TestCaseSource(nameof(AllFieldsData))]
    public void Read_NT_RV_AND_RT_RV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var mapping = new MappingDataNode{ { fieldName, node } };
        var res = Serialization.Read<DataDefTestDummy>(mapping, notNullableOverride: true);
        Assert.That(GetValue(res, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void Read_RT_NV<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var mapping = new MappingDataNode{ { fieldName, ValueDataNode.Null() } };
        Assert.That(() => Serialization.Read<DataDefTestDummy>(mapping, notNullableOverride: true), Throws.InstanceOf<NullNotAllowedException>());
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
        Assert.That(mapping[fieldName], Is.TypeOf(node.GetType()));
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
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
        Assert.That(GetValue(target!, fieldName), Is.Null);
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_NS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, null);
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
        Assert.That(GetValue(target!, fieldName), Is.Null);
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_RS_NT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, null);
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(NullableFieldsData))]
    public void CopyTo_NT_RS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
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
        Assert.That(() => Serialization.CopyTo(source, ref target, notNullableOverride: true), Throws.InstanceOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_NS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        if(typeof(T).IsValueType) return;

        var source = new DataDefTestDummy();
        SetValue(source, fieldName, null);
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Assert.That(() => Serialization.CopyTo(source, ref target, notNullableOverride: true), Throws.InstanceOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_RS_NT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        if(typeof(T).IsValueType) return;

        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, null);
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }

    [TestCaseSource(nameof(RegularFieldsData))]
    public void CopyTo_RT_RS_RT<T>(string fieldName, DataNode node, Func<T> value, Func<T> altValue)
    {
        var source = new DataDefTestDummy();
        SetValue(source, fieldName, value());
        var target = new DataDefTestDummy();
        SetValue(target, fieldName, altValue());
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
        Assert.That(GetValue(target!, fieldName), Is.EqualTo(value()));
    }
}
