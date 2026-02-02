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
    [Test]
    public void SerializationTest()
    {
        var component = new CustomBaseTypeSerializerTestComponent
        {
            SingleTestType = new TestTypeConcreteA(),
            TestTypeArray =
            [
                new TestTypeConcreteA() { SomeInt = 42 },
                new TestTypeConcreteB(),
                new TestTypeConcreteB() { SomeBool = true }
            ]
        };

        var node = Serialization.WriteValueAs<MappingDataNode>(component);
        var arrayNode = node["testTypeArray"] as SequenceDataNode;

        Assert.That(node["singleTestType"].Tag, Is.EqualTo("!ConcreteA"));
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
singleTestType: !ConcreteA
testTypeArray:
    - !ConcreteA
      someInt: 42
    - !type:TestTypeConcreteB
    - !ConcreteB
      someBool: true
";
        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(str));

        var mapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
        var testComp =
            Serialization.Read<CustomBaseTypeSerializerTestComponent>(mapping, notNullableOverride: true);

        Assert.That(testComp, Is.InstanceOf<CustomBaseTypeSerializerTestComponent>());
        Assert.That(testComp.SingleTestType, Is.InstanceOf<TestTypeConcreteA>());
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
}

#region TestTypes
[RegisterComponent]
internal sealed partial class CustomBaseTypeSerializerTestComponent : Component
{
    [DataField(customTypeSerializer: typeof(CustomBaseTypeSerializer<TestTypeBase>))]
    public TestTypeBase SingleTestType;

    [DataField(customTypeSerializer: typeof(CustomBaseTypeSerializer<TestTypeBase>))]
    public TestTypeBase[] TestTypeArray;
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
#endregion
