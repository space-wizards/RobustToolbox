using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

[TestFixture]
public sealed partial class ManagerTests : SerializationTest
{
    /*
    legend: NT => Nullable Type
            RT => Not Nullable Type
            NV => Null Value
            RV => Regular Value
            NS => Null source (copyto)
            NT => Null target (copyto)

    specialcase validate & pushcomposition

    for each [write|read|copy|copycreate]
    for each testcase except copyto, do:    NT_NV
                                            NT_RV
                                            RT_NV //only possible for class
                                            RT_RV
    for copyto, do: NT_NS_NT
                    NT_NS_RT
                    NT_RS_NT
                    NT_RS_RT
                    RT_NS_NT //only possible for class
                    RT_NS_RT //only possible for class
                    RT_RS_NT //only possible for class
                    RT_RS_RT
    testcases:  array
                ISelfSerialize
                DataDefinitionSameType (struct, class)
                TypeSerializer (primitive, reference)
                ContextTypeSerializer (primitive, reference)

                DataDefinitionBaseType (struct, class) //means: should use !type
                enum (write, read)
                returnsource (copy, createcopy)

    //todo datadef emitter testing
    //todo datarecords
    //todo test that no references are wrongfully copied
    //todo test read valueprovider
    */

    public static IEnumerable<object[]> TestableTypesClass = new[]
    {
        new object[]
        {
            new SequenceDataNode("1", "2", "3"),
            () => new[] { 1, 2, 3 },
            () => new[] { 3, 4, 5 },
            false,
            new Func<int[], object>[]
            {
                x => x[0],
                x => x[1],
                x => x[2],
            }
        }, //array
        new object[]
        {
            new ValueDataNode("testValue"),
            () => new SelfSerializeClass { TestValue = "testValue" },
            () => new SelfSerializeClass { TestValue = "anotherTestValue" },
            false,
            new Func<SelfSerializeClass, object>[] { x => x.TestValue }
        }, //ISelfSerialize
        new object[]
        {
            new MappingDataNode(new Dictionary<DataNode, DataNode>
            {
                { new ValueDataNode("one"), new ValueDataNode("valueOne") },
                { new ValueDataNode("two"), new SequenceDataNode("2", "3") },
            }),
            () => new DataDefClass
            {
                OneValue = "valueOne",
                TwoValue = new List<int> { 2, 3 }
            },
            () => new DataDefClass
            {
                OneValue = "anotherValueOne",
                TwoValue = new List<int> { 3, 4 }
            },
            false,
            new Func<DataDefClass, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //DataDefSameType
        new object[]
        {
            SerializerRanDataNode,
            SerializerClass.SerializerReturn,
            SerializerClass.SerializerReturnAlt,
            false,
            new Func<SerializerClass, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //TypeSerializer
        new object[]
        {
            SerializerRanCustomDataNode,
            SerializerClass.SerializerCustomReturn,
            SerializerClass.SerializerCustomReturnAlt,
            true,
            new Func<SerializerClass, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //CustomTypeSerializer
    };

    public static IEnumerable<object[]> TestableTypesStruct = new object[][]
    {
        new object[]
        {
            new ValueDataNode("testValue"),
            () => new SelfSerializeStruct { TestValue = "testValue" },
            () => new SelfSerializeStruct { TestValue = "anotherTestValue" },
            false,
            new Func<SelfSerializeStruct, object>[] { x => x.TestValue }
        }, //ISelfSerialize
        new object[]
        {
            new MappingDataNode(new Dictionary<DataNode, DataNode>()
            {
                { new ValueDataNode("one"), new ValueDataNode("valueOne") },
                { new ValueDataNode("two"), new SequenceDataNode("2", "3") },
            }),
            () => new DataDefStruct
            {
                OneValue = "valueOne",
                TwoValue = new List<int> { 2, 3 }
            },
            () => new DataDefStruct
            {
                OneValue = "anotherValueOne",
                TwoValue = new List<int> { 3, 4 }
            },
            false,
            new Func<DataDefStruct, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //DataDefSameType
        new object[]
        {
            SerializerRanDataNode,
            SerializerStruct.SerializerReturn,
            SerializerStruct.SerializerReturnAlt,
            false,
            new Func<SerializerStruct, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //TypeSerializer
        new object[]
        {
            SerializerRanCustomDataNode,
            SerializerStruct.SerializerCustomReturn,
            SerializerStruct.SerializerCustomReturnAlt,
            true,
            new Func<SerializerStruct, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //CustomTypeSerializer
    };

    public static IEnumerable<object[]> TestableTypesAll =>
        new[] { TestableTypesClass, TestableTypesStruct }.SelectMany(x => x);

    #region Write

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void Write_NT_NV_Struct<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : struct
    {
        var resNode = Serialization.WriteValue<T?>(null, context: Context(useContext));
        Assert.That(resNode.IsNull);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void Write_NT_NV_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : class
    {
        var resNode = Serialization.WriteValue<T?>(null, context: Context(useContext));
        Assert.That(resNode.IsNull);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void Write_NT_RV_Struct<T>(DataNode intendedNode, Func<T> value, Func<T> _, bool useContext, object[] __) where T : struct
    {
        var resNode = Serialization.WriteValue<T?>(value(), context: Context(useContext));
        Assert.That(resNode.Equals(intendedNode));
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void Write_NT_RV_Class<T>(DataNode intendedNode, Func<T> value, Func<T> _, bool useContext, object[] __) where T : class
    {
        var resNode = Serialization.WriteValue<T?>(value(), context: Context(useContext));
        Assert.That(resNode.Equals(intendedNode));
    }

/*
//todo paul uncomment when serv3 nullability for reference types is sane again
[TestCaseSource(nameof(TestableTypesClass))]
private void Write_RT_NV_Class<T>(DataNode intendedNode, Func<T> value, Func<T> _, bool useContext) where T : class
{
    Assert.That(() => Serialization.WriteValue<T>(null!, context: Context(useContext)), Throws.TypeOf<NullNotAllowedException>());
}
*/

    [TestCaseSource(nameof(TestableTypesAll))]
    public void Write_RT_RV<T>(DataNode intendedNode, Func<T> value, Func<T> altValue, bool useContext, object[] _)
    {
        var resNode = Serialization.WriteValue<T>(value(), context: Context(useContext));
        Assert.That(resNode.Equals(intendedNode));
    }

    #endregion


    #region Read

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void Read_NT_NV_Struct<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____) where T : struct
    {
        var val = Serialization.Read<T?>(ValueDataNode.Null(), context: Context(useContext));
        Assert.Null(val);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void Read_NT_NV_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____) where T : class
    {
        var val = Serialization.Read<T?>(ValueDataNode.Null(), context: Context(useContext));
        Assert.Null(val);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void Read_NT_RV_Struct<T>(DataNode node, Func<T> value, Func<T> _, bool useContext, object[] valueExtractors) where T : struct
    {
        var val = Serialization.Read<T?>(node, context: Context(useContext));
        Assert.NotNull(val);
        AssertEqual(val!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void Read_NT_RV_Class<T>(DataNode node, Func<T> value, Func<T> _, bool useContext, object[] valueExtractors) where T : class
    {
        var val = Serialization.Read<T?>(node, context: Context(useContext));
        Assert.NotNull(val);
        AssertEqual(val!, value(), valueExtractors);
    }

    //todo paul uncomment when serv3 nullability for reference types is sane again
    //[TestCaseSource(nameof(TestableTypesAll))]
    [TestCaseSource(nameof(TestableTypesStruct))]
    public void Read_RT_NV<T>(DataNode node, Func<T> _, Func<T> __, bool useContext, object[] ___)
    {
        Assert.That(() => Serialization.Read<T>(ValueDataNode.Null(), context: Context(useContext)),
            Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(TestableTypesAll))]
    public void Read_RT_RV<T>(DataNode node, Func<T> value, Func<T> __, bool useContext, object[] valueExtractors)
    {
        var val = Serialization.Read<T>(node, context: Context(useContext));
        AssertEqual(val!, value(), valueExtractors);
    }

    #endregion

    #region CopyTo

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_NS_NT_Struct<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : struct
    {
        T? target = null;
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.Null(target);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_NS_NT_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : class
    {
        T? target = null;
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.Null(target);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_NS_RT_Struct<T>(DataNode _, Func<T> value, Func<T> __, bool useContext, object[] ___)
        where T : struct
    {
        T? target = value();
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.Null(target);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_NS_RT_Class<T>(DataNode _, Func<T> value, Func<T> __, bool useContext, object[] ___)
        where T : class
    {
        T? target = value();
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.Null(target);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_RS_NT_Struct<T>(DataNode _, Func<T> value, Func<T> __, bool useContext,
        object[] valueExtractors) where T : struct
    {
        T? target = null;
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.NotNull(target);
        AssertEqual(target!.Value, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_RS_NT_Class<T>(DataNode _, Func<T> value, Func<T> __, bool useContext,
        object[] valueExtractors) where T : class
    {
        T? target = null;
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.NotNull(target);
        AssertEqual(target!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_RS_RT<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : struct
    {
        T? target = altValue();
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.NotNull(target);
        AssertEqual(target!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_RS_RT_Class<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : class
    {
        T? target = altValue();
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.NotNull(target);
        AssertEqual(target!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_RT_NS_NT_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : class
    {
        T? target = null;
        Assert.That(() => { Serialization.CopyTo<T>(null!, ref target, context: Context(useContext)); },
            Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_RT_NS_RT_Class<T>(DataNode _, Func<T> value, Func<T> __, bool useContext, object[] ___)
        where T : class
    {
        T? target = value();
        Assert.That(() => { Serialization.CopyTo<T>(null!, ref target, context: Context(useContext)); },
            Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_RT_RS_NT_Class<T>(DataNode _, Func<T> value, Func<T> ___, bool useContext,
        object[] valueExtractors) where T : class
    {
        T? target = null;
        Serialization.CopyTo<T>(value(), ref target, context: Context(useContext));
        Assert.NotNull(target);
        AssertEqual(target!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesAll))]
    public void CopyTo_RT_RS_RT<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors)
    {
        var target = altValue();
        Serialization.CopyTo(value(), ref target, context: Context(useContext));
        AssertEqual(target!, value(), valueExtractors);
    }

    #endregion


    #region CreateCopy

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CreateCopy_NT_NV_Struct<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : struct
    {
        var copy = Serialization.CreateCopy<T?>(null, context: Context(useContext));
        Assert.Null(copy);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CreateCopy_NT_NV_Class<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : class
    {
        var copy = Serialization.CreateCopy<T?>(null, context: Context(useContext));
        Assert.Null(copy);
    }

    [TestCaseSource(nameof(TestableTypesAll))]
    public void CreateCopy_NT_RV<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors)
    {
        var copy = Serialization.CreateCopy<T?>(value(), context: Context(useContext));
        Assert.NotNull(copy);
        AssertEqual(copy!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CreateCopy_RT_NV_Class<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : class
    {
        Assert.That(() =>
        {
            var __ = Serialization.CreateCopy<T>(null!, context: Context(useContext));
        }, Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(TestableTypesAll))]
    public void CreateCopy_RT_RV<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors)
    {
        var copy = Serialization.CreateCopy<T>(value(), context: Context(useContext));
        AssertEqual(copy, value(), valueExtractors);
    }

    #endregion

    private void AssertEqual<T>(T a, T b, object[] valueExtractors)
    {
        Assert.Multiple(() =>
        {
            foreach (var extractor in valueExtractors.Cast<Func<T, object>>())
            {
                Assert.That(extractor(a).Equals(extractor(b)));
            }
        });
    }

    private ISerializationContext? Context(bool useContext) => useContext ? this : null;
}
