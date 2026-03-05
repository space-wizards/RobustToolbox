using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(CustomBaseTypeSerializer<>))]
internal sealed class CustomBaseTypeSerializerTest : OurSerializationTest
{
    private CustomBaseTypeSerializerTestComponent ReadYaml(string str)
    {
        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(str));

        var mapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
        return Serialization.Read<CustomBaseTypeSerializerTestComponent>(mapping, notNullableOverride: true);
    }

    [Test]
    public void SerializationTest()
    {
        var component = new CustomBaseTypeSerializerTestComponent
        {
            FirstTestType = new TestTypeConcreteA(),
            SecondTestType = new AnotherBaseTestTypeConcreteA(),
            TestTypeArray =
            [
                new TestTypeConcreteA() { SomeInt = 42 },
                new TestTypeConcreteB(),
                new TestTypeConcreteB() { SomeBool = true }
            ]
        };

        var node = Serialization.WriteValueAs<MappingDataNode>(component);
        var arrayNode = node["testTypeArray"] as SequenceDataNode;

        Assert.That(node["firstTestType"].Tag, Is.EqualTo("!ConcreteA"));
        Assert.That(node["secondTestType"].Tag, Is.EqualTo("!ConcreteA"));
        Assert.That(arrayNode, Is.InstanceOf<SequenceDataNode>());
        Assert.That(arrayNode, Has.Count.EqualTo(3));
        Assert.That(arrayNode[0].Tag, Is.EqualTo("!ConcreteA"));
        Assert.That(arrayNode[1].Tag, Is.EqualTo("!ConcreteB"));
        Assert.That(arrayNode[2].Tag, Is.EqualTo("!ConcreteB"));
    }


    [Test]
    public void DeserializationTest()
    {
        var str = @"
            type: CustomBaseTypeSerializerTest
            firstTestType: !ConcreteA
            secondTestType: !ConcreteA
            testTypeArray:
                - !ConcreteA
                  someInt: 42
                - !type:TestTypeConcreteB
                - !ConcreteB
                  someBool: true
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<CustomBaseTypeSerializerTestComponent>());
        Assert.That(testComp.FirstTestType, Is.InstanceOf<TestTypeConcreteA>());
        Assert.That(testComp.SecondTestType, Is.InstanceOf<AnotherBaseTestTypeConcreteA>());
        Assert.That(testComp.TestTypeArray, Has.Length.EqualTo(3));
        Assert.That(testComp.TestTypeArray[0], Is.InstanceOf<TestTypeConcreteA>());
        Assert.That(testComp.TestTypeArray[1], Is.InstanceOf<TestTypeConcreteB>());
        Assert.That(testComp.TestTypeArray[2], Is.InstanceOf<TestTypeConcreteB>());
        var a = (TestTypeConcreteA)testComp.TestTypeArray[0];
        var b1 = (TestTypeConcreteB)testComp.TestTypeArray[1];
        var b2 = (TestTypeConcreteB)testComp.TestTypeArray[2];
        Assert.That(a.SomeInt, Is.EqualTo(42));
        Assert.That(b1.SomeBool, Is.EqualTo(false));
        Assert.That(b2.SomeBool, Is.EqualTo(true));
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildEndingConcreteDeserializationTest()
    {
        var str = @"
            type: CustomBaseTypeSerializerTest
            thirdTestType: !Concrete
            ";
        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<CustomBaseTypeSerializerTestComponent>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestConcrete>());
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildEndingBaseDeserializationTest()
    {
        var str = @"
            type: CustomBaseTypeSerializerTest
            thirdTestType: !ConcreteBase
            ";
        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<CustomBaseTypeSerializerTestComponent>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestConcreteBase>());
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildUsingBaseNameDeserializationTest()
    {
        var str = @"
            type: CustomBaseTypeSerializerTest
            thirdTestType: !TestBase
            ";
        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<CustomBaseTypeSerializerTestComponent>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestTestBase>());
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildEndingConcreteSerializationTest()
    {
        var component = new CustomBaseTypeSerializerTestComponent
        {
            ThirdTestType = new TestBaseTestConcrete()
        };

        var node = Serialization.WriteValueAs<MappingDataNode>(component);

        Assert.That(node["thirdTestType"].Tag, Is.EqualTo("!Concrete"));
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildEndingBaseSerializationTest()
    {
        var component = new CustomBaseTypeSerializerTestComponent
        {
            ThirdTestType = new TestBaseTestConcreteBase()
        };

        var node = Serialization.WriteValueAs<MappingDataNode>(component);

        Assert.That(node["thirdTestType"].Tag, Is.EqualTo("!ConcreteBase"));
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildUsingBaseNameSerializationTest()
    {
        var component = new CustomBaseTypeSerializerTestComponent
        {
            ThirdTestType = new TestBaseTestTestBase()
        };

        var node = Serialization.WriteValueAs<MappingDataNode>(component);

        Assert.That(node["thirdTestType"].Tag, Is.EqualTo("!TestBase"));
    }
}

#region TestTypes
[RegisterComponent]
internal sealed partial class CustomBaseTypeSerializerTestComponent : Component
{
    [DataField(customTypeSerializer: typeof(CustomBaseTypeSerializer<TestTypeBase>))]
    public TestTypeBase[] TestTypeArray;

    [DataField(customTypeSerializer: typeof(CustomBaseTypeSerializer<TestTypeBase>))]
    public TestTypeBase FirstTestType;

    [DataField(customTypeSerializer: typeof(CustomBaseTypeSerializer<AnotherBaseTestTypeBase>))]
    public AnotherBaseTestTypeBase SecondTestType;

    [DataField(customTypeSerializer: typeof(CustomBaseTypeSerializer<TestBaseTestBase>))]
    public TestBaseTestBase ThirdTestType;
}

[ImplicitDataDefinitionForInheritors]
internal abstract partial class TestTypeBase;
internal sealed partial class TestTypeConcreteA : TestTypeBase
{
    [DataField]
    public int SomeInt;
}

internal sealed partial class TestTypeConcreteB : TestTypeBase
{
    [DataField]
    public bool SomeBool;
}

[ImplicitDataDefinitionForInheritors]
internal abstract partial class AnotherBaseTestTypeBase;
internal sealed partial class AnotherBaseTestTypeConcreteA : AnotherBaseTestTypeBase;

[ImplicitDataDefinitionForInheritors]
internal abstract partial class TestBaseTestBase;
internal sealed partial class TestBaseTestConcrete : TestBaseTestBase;
internal sealed partial class TestBaseTestConcreteBase : TestBaseTestBase;
internal sealed partial class TestBaseTestTestBase : TestBaseTestBase;

#endregion
