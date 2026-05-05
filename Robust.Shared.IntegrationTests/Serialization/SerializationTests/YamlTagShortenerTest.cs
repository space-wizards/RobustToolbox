using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

[TestFixture]
internal sealed class YamlTagShortenerTest : OurSerializationTest
{
    protected override Type[]? ExtraComponents => [typeof(YamlTagShortenerTestComponent)];


    private YamlTagShortenerTestComponent ReadYaml(string str)
    {
        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(str));

        var mapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<MappingDataNode>();
        return Serialization.Read<YamlTagShortenerTestComponent>(mapping, notNullableOverride: true);
    }

    [Test]
    public void DeserializationTest()
    {
        const string str = @"
            type: YamlTagShortenerTest
            firstTestType: !ConcreteA
            secondTestType: !ConcreteA
            fourthTestType: !ConcreteD
            testTypeArray:
                - !ConcreteA
                  someInt: 42
                - !type:TestTypeConcreteB
                - !ConcreteB
                  someBool: true
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<YamlTagShortenerTestComponent>());
        Assert.That(testComp.FirstTestType, Is.InstanceOf<TestTypeConcreteA>());
        Assert.That(testComp.SecondTestType, Is.InstanceOf<AnotherBaseTestTypeConcreteA>());
        Assert.That(testComp.FourthTestType, Is.InstanceOf<WronglyNamedTestTypeD>());
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
    public void ValidationTest()
    {
        const string str = @"
            - type: YamlTagShortenerTest
              firstTestType: !ConcreteA
              secondTestType: !ConcreteA
              fourthTestType: !ConcreteD
              testTypeArray:
                  - !ConcreteA
                    someInt: 42
                  - !type:TestTypeConcreteB
                  - !ConcreteB
                    someBool: true";

        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(str));
        var mapping = yamlStream.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>();
        var validationNode = Serialization.ValidateNode<ComponentRegistry>(mapping);

        Assert.That(validationNode.Valid, Is.True);
    }

    [Test]
    public void SerializationTest()
    {
        var component = new YamlTagShortenerTestComponent
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

        Assert.That(node["firstTestType"].Tag, Is.EqualTo("!type:TestTypeConcreteA"));
        Assert.That(node["secondTestType"].Tag, Is.EqualTo("!type:AnotherBaseTestTypeConcreteA"));
        Assert.That(arrayNode, Is.InstanceOf<SequenceDataNode>());
        Assert.That(arrayNode, Has.Count.EqualTo(3));
        Assert.That(arrayNode[0].Tag, Is.EqualTo("!type:TestTypeConcreteA"));
        Assert.That(arrayNode[1].Tag, Is.EqualTo("!type:TestTypeConcreteB"));
        Assert.That(arrayNode[2].Tag, Is.EqualTo("!type:TestTypeConcreteB"));
    }


    [Test]
    public void LongFormTagCompatibilityTest()
    {
        const string str = @"
            type: YamlTagShortenerTest
            firstTestType: !type:TestTypeConcreteA
            secondTestType: !type:AnotherBaseTestTypeConcreteA
            thirdTestType: !type:TestBaseTestConcrete
            fourthTestType: !type:WronglyNamedTestTypeD
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<YamlTagShortenerTestComponent>());
        Assert.That(testComp.FirstTestType, Is.InstanceOf<TestTypeConcreteA>());
        Assert.That(testComp.SecondTestType, Is.InstanceOf<AnotherBaseTestTypeConcreteA>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestConcrete>());
        Assert.That(testComp.FourthTestType, Is.InstanceOf<WronglyNamedTestTypeD>());
    }

    [Test]
    public void CustomChildTagDeserializationTest()
    {
        const string str = @"
            type: YamlTagShortenerTest
            testTypeArray:
                - !ConcreteC
                - !AnotherConcreteC
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<YamlTagShortenerTestComponent>());
        Assert.That(testComp.TestTypeArray, Has.Length.EqualTo(2));
        Assert.That(testComp.TestTypeArray[0], Is.InstanceOf<WronglyNamedTestTypeC>());
        Assert.That(testComp.TestTypeArray[1], Is.InstanceOf<WronglyNamedTestTypeC>());
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildEndingConcreteDeserializationTest()
    {
        const string str = @"
            type: YamlTagShortenerTest
            thirdTestType: !Concrete
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<YamlTagShortenerTestComponent>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestConcrete>());
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildEndingBaseDeserializationTest()
    {
        const string str = @"
            type: YamlTagShortenerTest
            thirdTestType: !ConcreteBase
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<YamlTagShortenerTestComponent>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestConcreteBase>());
    }

    [Test]
    public void BaseTypeDuplicatesNameWithChildUsingBaseNameDeserializationTest()
    {
        const string str = @"
            type: YamlTagShortenerTest
            thirdTestType: !TestBase
            ";

        var testComp = ReadYaml(str);

        Assert.That(testComp, Is.InstanceOf<YamlTagShortenerTestComponent>());
        Assert.That(testComp.ThirdTestType, Is.InstanceOf<TestBaseTestTestBase>());
    }
}

#region TestTypes
[RegisterComponent]
internal sealed partial class YamlTagShortenerTestComponent : Component
{
    [DataField]
    public TestTypeBase[] TestTypeArray;

    [DataField]
    public TestTypeBase FirstTestType;

    [DataField]
    public AnotherBaseTestTypeBase SecondTestType;

    [DataField]
    public TestBaseTestBase ThirdTestType;

    [DataField]
    public WronglyNamedTestBaseType FourthTestType;
}

[ImplicitDataDefinitionForInheritors]
[YamlTagShortener]
[CustomChildTag<WronglyNamedTestTypeC>("ConcreteC")]
[CustomChildTag<WronglyNamedTestTypeC>("AnotherConcreteC")]
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

internal sealed partial class WronglyNamedTestTypeC : TestTypeBase;

[ImplicitDataDefinitionForInheritors]
[YamlTagShortener]
internal abstract partial class AnotherBaseTestTypeBase;
internal sealed partial class AnotherBaseTestTypeConcreteA : AnotherBaseTestTypeBase;

[ImplicitDataDefinitionForInheritors]
[YamlTagShortener]
internal abstract partial class TestBaseTestBase;
internal sealed partial class TestBaseTestConcrete : TestBaseTestBase;
internal sealed partial class TestBaseTestConcreteBase : TestBaseTestBase;
internal sealed partial class TestBaseTestTestBase : TestBaseTestBase;

[ImplicitDataDefinitionForInheritors]
[CustomChildTag<WronglyNamedTestTypeD>("ConcreteD")]
internal abstract partial class WronglyNamedTestBaseType;
internal sealed partial class WronglyNamedTestTypeD : WronglyNamedTestBaseType;

#endregion
