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

    for each [write|read|copyto|copycreate]
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
                ISelfSerialize (only read/write)
                DataDefinitionSameType (struct, class)
                TypeSerializer (struct, reference)
                ContextTypeSerializer (struct, reference)
                enum

                todo DataDefinitionBaseType (struct, class) //means: should use !type
                todo returnsource (copy, createcopy)
                todo test the generic variants too

    //todo figure out a way to tell which serializer we used for (create)copy

    //todo datarecords
    //todo validate & pushcomposition
    //todo prioritytests ([copybyref] <> null <> custom <> regular <> array/enum <> datadef)
    */

    #region DataSources

    public static IEnumerable<object[]> ReadWriteTypesClass => new[]
    {
        new []
        {
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
                new MappingDataNode(new Dictionary<string, DataNode>
                {
                    { "one", new ValueDataNode("valueOne") },
                    { "two", new SequenceDataNode("2", "3") },
                }){Tag = $"!type:{nameof(DataDefClass)}"},
                () => (IDataDefBaseInterface)new DataDefClass
                {
                    OneValue = "valueOne",
                    TwoValue = new List<int> { 2, 3 }
                },
                () => (IDataDefBaseInterface)new DataDefClass
                {
                    OneValue = "anotherValueOne",
                    TwoValue = new List<int> { 3, 4 }
                },
                false,
                new Func<IDataDefBaseInterface, object>[]
                {
                    x => ((DataDefClass)x).OneValue,
                    x => ((DataDefClass)x).TwoValue,
                }
            }, //DataDefinitionBaseType
        },
        TestableTypesClass
    }.SelectMany(x => x);

    public static IEnumerable<object[]> TestableTypesClass => new[]
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
            new MappingDataNode(new Dictionary<string, DataNode>
            {
                { "one", new ValueDataNode("valueOne") },
                { "two", new SequenceDataNode("2", "3") },
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
            (object)SerializerClass.SerializerReturn,
            (object)SerializerClass.SerializerReturnAlt,
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
            (object)SerializerClass.SerializerCustomReturn,
            (object)SerializerClass.SerializerCustomReturnAlt,
            true,
            new Func<SerializerClass, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //CustomTypeSerializer
    };

    public static IEnumerable<object[]> ReadWriteTypesStruct => new[]
    {
        new object[][]
        {
            new object[]
            {
                new ValueDataNode("testValue"),
                () => new SelfSerializeStruct { TestValue = "testValue" },
                () => new SelfSerializeStruct { TestValue = "anotherTestValue" },
                false,
                new Func<SelfSerializeStruct, object>[] { x => x.TestValue }
            }, //ISelfSerialize
            /*new object[]
            {
                new MappingDataNode(new Dictionary<DataNode, DataNode>
                {
                    { new ValueDataNode("one"), new ValueDataNode("valueOne") },
                    { new ValueDataNode("two"), new SequenceDataNode("2", "3") },
                }){Tag = $"!type:{nameof(DataDefStruct)}"},
                () => new DataDefStruct
                {
                    OneValue = "valueOne",
                    TwoValue = new List<int> { 2, 3 }
                } as IDataDefBaseInterface,
                () => new DataDefStruct
                {
                    OneValue = "anotherValueOne",
                    TwoValue = new List<int> { 3, 4 }
                } as IDataDefBaseInterface,
                false,
                new Func<IDataDefBaseInterface, object>[]
                {
                    x => ((DataDefStruct)x).OneValue,
                    x => ((DataDefStruct)x).TwoValue,
                }
            }, //DataDefinitionBaseType*/
        },
        TestableTypesStruct
    }.SelectMany(x => x!);

    public static IEnumerable<object[]> TestableTypesStruct => new object[][]
    {
        new object[]
        {
            new MappingDataNode(new Dictionary<string, DataNode>()
            {
                { "one", new ValueDataNode("valueOne") },
                { "two", new SequenceDataNode("2", "3") },
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
            (object)SerializerStruct.SerializerReturn,
            (object)SerializerStruct.SerializerReturnAlt,
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
            (object)SerializerStruct.SerializerCustomReturn,
            (object)SerializerStruct.SerializerCustomReturnAlt,
            true,
            new Func<SerializerStruct, object>[]
            {
                x => x.OneValue,
                x => x.TwoValue,
            }
        }, //CustomTypeSerializer
        new object[]
        {
            new ValueDataNode("C"),
            () => TestEnum.C,
            () => TestEnum.B,
            false,
            new Func<TestEnum, object>[]{ x => x }
        }
    };

    public static IEnumerable<object[]> TestableTypesAll =>
        new[] { TestableTypesClass, TestableTypesStruct }.SelectMany(x => x);

    public static IEnumerable<object[]> ReadWriteTypesAll =>
        new[] { ReadWriteTypesClass, ReadWriteTypesStruct }.SelectMany(x => x);

    #endregion

    #region Write

    [TestCaseSource(nameof(ReadWriteTypesStruct))]
    public void Write_NT_NV_Struct<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : struct
    {
        var resNode = Serialization.WriteValue<T?>(null, context: Context(useContext));
        Assert.That(resNode.IsNull);
    }

    [TestCaseSource(nameof(ReadWriteTypesClass))]
    public void Write_NT_NV_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : class
    {
        var resNode = Serialization.WriteValue<T?>(null, context: Context(useContext));
        Assert.That(resNode.IsNull);
    }

    [TestCaseSource(nameof(ReadWriteTypesStruct))]
    public void Write_NT_RV_Struct<T>(DataNode intendedNode, Func<T> value, Func<T> _, bool useContext, object[] __) where T : struct
    {
        var resNode = Serialization.WriteValue<T?>(value(), context: Context(useContext));
        Assert.That(resNode.Equals(intendedNode));
    }

    [TestCaseSource(nameof(ReadWriteTypesClass))]
    public void Write_NT_RV_Class<T>(DataNode intendedNode, Func<T> value, Func<T> _, bool useContext, object[] __) where T : class
    {
        var resNode = Serialization.WriteValue<T?>(value(), context: Context(useContext));
        Assert.That(resNode.Equals(intendedNode));
    }

    [TestCaseSource(nameof(ReadWriteTypesClass))]
    public void Write_RT_NV_Class<T>(DataNode intendedNode, Func<T> value, Func<T> altValue, bool useContext, object[] _) where T : class
    {
        Assert.That(() => Serialization.WriteValue<T>(null!, context: Context(useContext), notNullableOverride: true), Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(ReadWriteTypesAll))]
    public void Write_RT_RV<T>(DataNode intendedNode, Func<T> value, Func<T> altValue, bool useContext, object[] _)
    {
        var resNode = Serialization.WriteValue<T>(value(), context: Context(useContext));
        Assert.That(resNode.Equals(intendedNode));
    }

    #endregion

    #region Read

    [TestCaseSource(nameof(ReadWriteTypesStruct))]
    public void Read_NT_NV_Struct<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____) where T : struct
    {
        var val = Serialization.Read<T?>(ValueDataNode.Null(), context: Context(useContext));
        Assert.That(val, Is.Null);
    }

    [TestCaseSource(nameof(ReadWriteTypesClass))]
    public void Read_NT_NV_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____) where T : class
    {
        var val = Serialization.Read<T?>(ValueDataNode.Null(), context: Context(useContext));
        Assert.That(val, Is.Null);
    }

    [TestCaseSource(nameof(ReadWriteTypesStruct))]
    public void Read_NT_RV_Struct<T>(DataNode node, Func<T> value, Func<T> _, bool useContext, object[] valueExtractors) where T : struct
    {
        var val = Serialization.Read<T?>(node, context: Context(useContext));
        Assert.That(val, Is.Not.Null);
        AssertEqual(val!.Value, value(), valueExtractors);
    }

    [TestCaseSource(nameof(ReadWriteTypesClass))]
    public void Read_NT_RV_Class<T>(DataNode node, Func<T> value, Func<T> _, bool useContext, object[] valueExtractors) where T : class
    {
        var val = Serialization.Read<T?>(node, context: Context(useContext));
        Assert.That(val, Is.Not.Null);
        AssertEqual(val!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(ReadWriteTypesAll))]
    public void Read_RT_NV<T>(DataNode node, Func<T> _, Func<T> __, bool useContext, object[] ___)
    {
        Assert.That(() => Serialization.Read<T>(ValueDataNode.Null(), context: Context(useContext), notNullableOverride: true),
            Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(ReadWriteTypesAll))]
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
        Assert.That(target, Is.Null);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_NS_NT_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : class
    {
        T? target = null;
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.That(target, Is.Null);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_NS_RT_Struct<T>(DataNode _, Func<T> value, Func<T> __, bool useContext, object[] ___)
        where T : struct
    {
        T? target = value();
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.That(target, Is.Null);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_NS_RT_Class<T>(DataNode _, Func<T> value, Func<T> __, bool useContext, object[] ___)
        where T : class
    {
        T? target = value();
        Serialization.CopyTo<T?>(null, ref target, context: Context(useContext));
        Assert.That(target, Is.Null);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_RS_NT_Struct<T>(DataNode _, Func<T> value, Func<T> __, bool useContext,
        object[] valueExtractors) where T : struct
    {
        T? target = null;
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.That(target, Is.Not.Null);
        AssertEqual(target!.Value, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_RS_NT_Class<T>(DataNode _, Func<T> value, Func<T> __, bool useContext,
        object[] valueExtractors) where T : class
    {
        T? target = null;
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.That(target, Is.Not.Null);
        AssertEqual(target!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesStruct))]
    public void CopyTo_NT_RS_RT_Struct<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : struct
    {
        T? target = altValue();
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.That(target, Is.Not.Null);
        AssertEqual(target!.Value, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_NT_RS_RT_Class<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : class
    {
        T? target = altValue();
        Serialization.CopyTo<T?>(value(), ref target, context: Context(useContext));
        Assert.That(target, Is.Not.Null);
        AssertEqual(target!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_RT_NS_NT_Class<T>(DataNode _, Func<T> __, Func<T> ___, bool useContext, object[] ____)
        where T : class
    {
        T target = null!;
        Assert.That(() => { Serialization.CopyTo(null!, ref target, context: Context(useContext), notNullableOverride: true); },
            Throws.TypeOf<NullNotAllowedException>());
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_RT_NS_RT_Class<T>(DataNode _, Func<T> value, Func<T> __, bool useContext, object[] ___)
        where T : class
    {
        T? target = value();
        Assert.That(() => { Serialization.CopyTo<T>(null!, ref target, context: Context(useContext), notNullableOverride: true); },
            Throws.TypeOf<NullNotAllowedException>());
    }


    [TestCaseSource(nameof(TestableTypesClass))]
    public void CopyTo_RT_RS_NT_Class<T>(DataNode _, Func<T> value, Func<T> ___, bool useContext,
        object[] valueExtractors) where T : class
    {
        T target = null!;
        Serialization.CopyTo(value(), ref target, context: Context(useContext));
        Assert.That(target, Is.Not.Null);
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
        Assert.That(copy, Is.Null);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CreateCopy_NT_NV_Class<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : class
    {
        var copy = Serialization.CreateCopy<T?>(null, context: Context(useContext));
        Assert.That(copy, Is.Null);
    }

    [TestCaseSource(nameof(TestableTypesAll))]
    public void CreateCopy_NT_RV<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors)
    {
        var copy = Serialization.CreateCopy<T?>(value(), context: Context(useContext));
        Assert.That(copy, Is.Not.Null);
        AssertEqual(copy!, value(), valueExtractors);
    }

    [TestCaseSource(nameof(TestableTypesClass))]
    public void CreateCopy_RT_NV_Class<T>(DataNode _, Func<T> value, Func<T> altValue, bool useContext,
        object[] valueExtractors) where T : class
    {
        Assert.That(() =>
        {
            var __ = Serialization.CreateCopy<T>(null!, context: Context(useContext), notNullableOverride: true);
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

    #region CopyByRef

    [Test]
    public void CopyTo_CopyByRef_Class()
    {
        CopyByRefTestClass target = null!;
        var source = new CopyByRefTestClass();
        Serialization.CopyTo(source, ref target, notNullableOverride: true);
        Assert.That(target, Is.Not.Null);
        Assert.That(target!, Is.SameAs(source));
    }

    [Test]
    public void CopyTo_CopyByRef_NV_Class()
    {
        CopyByRefTestClass? target = null;
        Serialization.CopyTo(null, ref target);
        Assert.That(target, Is.Null);
    }

    [Test]
    public void CreateCopy_CopyByRef_Class()
    {
        var source = new CopyByRefTestClass();
        var copy = Serialization.CreateCopy(source, notNullableOverride: true);
        Assert.That(copy, Is.SameAs(source));
    }

    [Test]
    public void CreateCopy_CopyByRef_NV_Class()
    {
        var copy = Serialization.CreateCopy<CopyByRefTestClass?>(null);
        Assert.That(copy, Is.Null);
    }

    [Test]
    public void CopyTo_CopyByRef_Struct()
    {
        CopyByRefTestStruct? target = null;
        var source = new CopyByRefTestStruct{ID = 5};
        Serialization.CopyTo(source, ref target);
        Assert.That(target, Is.Not.Null);
        Assert.That(target!.Value.ID, Is.EqualTo(source.ID));
    }

    [Test]
    public void CopyTo_CopyByRef_NV_Struct()
    {
        CopyByRefTestStruct? target = null;
        Serialization.CopyTo(null, ref target);
        Assert.That(target, Is.Null);
    }

    [Test]
    public void CreateCopy_CopyByRef_Struct()
    {
        var source = new CopyByRefTestStruct{ID = 5};
        var copy = Serialization.CreateCopy(source);
        Assert.That(copy.ID, Is.EqualTo(source.ID));
    }

    [Test]
    public void CreateCopy_CopyByRef_NV_Struct()
    {
        var copy = Serialization.CreateCopy<CopyByRefTestStruct?>(null);
        Assert.That(copy, Is.Null);
    }

    #endregion

    private void AssertEqual<T>(T a, T b, object[] valueExtractors)
    {
        Assert.Multiple(() =>
        {
            foreach (var extractor in valueExtractors.Cast<Func<T, object>>())
            {
                var objA = extractor(a);
                var objB = extractor(b);

                Assert.That(objA.GetType(), Is.EqualTo(objB.GetType()));

                if (objA is not List<int> listA)
                {
                    Assert.That(extractor(a).Equals(extractor(b)));
                    continue;
                }

                var listB = (List<int>)objB;

                Assert.That(listA, Has.Count.EqualTo(listB.Count));

                for (int i = 0; i < listA.Count; i++)
                {
                    Assert.That(listA[i], Is.EqualTo(listB[i]));
                }
            }
        });
    }

    private ISerializationContext? Context(bool useContext) => useContext ? this : null;
}
