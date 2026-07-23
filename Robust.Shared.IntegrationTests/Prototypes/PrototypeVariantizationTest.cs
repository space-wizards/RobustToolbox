using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Prototypes;

[UsedImplicitly]
[TestFixture]
internal sealed partial class PrototypeVariantizationTest : OurRobustUnitTest
{
    private const string TestProtoId = "TestPrototype";
    private const string TestProtoVariantAId = "TestPrototypeVariantA";
    private const string TestProtoVariantBId = "TestPrototypeVariantB";

    private IPrototypeManager protoManager = default!;

    protected override Type[] ExtraComponents => new[] { typeof(PrototypeVariantizationTestComponent) };

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        protoManager = IoCManager.Resolve<IPrototypeManager>();
        protoManager.Initialize();
        protoManager.LoadString(DOCUMENT, changed: new());
        protoManager.ResolveResults();
    }

    /// <summary>
    /// Tests that the prototypes defined in the test YAML document, as well as their expected variants,
    /// are properly generated and can be resolved from the prototype manager.
    /// </summary>
    [Test]
    public void TestPrototypesExist()
    {
        Assert.Multiple(() =>
        {
            Assert.That(protoManager.Resolve<EntityPrototype>(TestProtoId, out var _));
            Assert.That(protoManager.Resolve<EntityPrototype>(TestProtoVariantAId, out _));
            Assert.That(protoManager.Resolve<EntityPrototype>(TestProtoVariantBId, out _));
        });
    }

    /// <summary>
    /// Tests that the value modifications defined in the test YAML document are properly applied to all variants.
    /// </summary>
    [Test]
    public void TestValueModification()
    {
        // Original
        Assert.Multiple(() =>
        {
            var testProto = protoManager.Index<EntityPrototype>(TestProtoId);
            Assert.That(testProto.Components, Contains.Key("PrototypeVariantizationTest"));

            var comp = testProto.Components["PrototypeVariantizationTest"].Component as PrototypeVariantizationTestComponent;
            Assert.That(comp, Is.Not.Null);
            Assert.That(comp!.NumValue, Is.EqualTo(1));
            Assert.That(comp!.EnumValue, Is.EqualTo(PrototypeVariantizationTestEnum.First));
            Assert.That(comp!.StringValue, Is.EqualTo("string-1"));
            Assert.That(comp!.StringArray.SequenceEqual(["el-1"]));

            Assert.That(comp!.StringDict.TryGetValue("key1", out var value1));
            Assert.That(value1, Is.EqualTo("string-1a"));
            Assert.That(comp!.StringDict.TryGetValue("key2", out var value2));
            Assert.That(value2, Is.EqualTo("string-1b"));

            Assert.That(comp!.RecursiveRecord?.Child?.Child?.Value, Is.EqualTo("child-1"));
        });

        // First variant
        Assert.Multiple(() =>
        {
            var testProtoVariantA = protoManager.Index<EntityPrototype>(TestProtoVariantAId);
            Assert.That(testProtoVariantA.Components, Contains.Key("PrototypeVariantizationTest"));

            var compVariantA = testProtoVariantA.Components["PrototypeVariantizationTest"].Component as PrototypeVariantizationTestComponent;
            Assert.That(compVariantA, Is.Not.Null);
            Assert.That(compVariantA!.NumValue, Is.EqualTo(1.5f));
            Assert.That(compVariantA!.EnumValue, Is.EqualTo(PrototypeVariantizationTestEnum.Second));
            Assert.That(compVariantA!.StringValue, Is.EqualTo("string-2"));
            Assert.That(compVariantA!.StringArray.SequenceEqual(["el-1", "el-2"]));

            Assert.That(compVariantA!.StringDict.TryGetValue("key1", out var value1));
            Assert.That(value1, Is.EqualTo("string-2a"));
            Assert.That(compVariantA!.StringDict.TryGetValue("key2", out var value2));
            Assert.That(value2, Is.EqualTo("string-2b"));

            Assert.That(compVariantA!.RecursiveRecord?.Child?.Child?.Value, Is.EqualTo("child-2"));
        });

        // Second variant
        Assert.Multiple(() =>
        {
            var testProtoVariantB = protoManager.Index<EntityPrototype>(TestProtoVariantBId);
            Assert.That(testProtoVariantB.Components, Contains.Key("PrototypeVariantizationTest"));

            var compVariantB = testProtoVariantB.Components["PrototypeVariantizationTest"].Component as PrototypeVariantizationTestComponent;
            Assert.That(compVariantB, Is.Not.Null);
            Assert.That(compVariantB!.NumValue, Is.EqualTo(2));
            Assert.That(compVariantB!.EnumValue, Is.EqualTo(PrototypeVariantizationTestEnum.Third));
            Assert.That(compVariantB!.StringValue, Is.EqualTo("string-3"));
            Assert.That(compVariantB!.StringArray.SequenceEqual(["el-1", "el-2", "el-3"]));

            Assert.That(compVariantB!.StringDict.TryGetValue("key1", out var value1));
            Assert.That(value1, Is.EqualTo("string-3a"));
            Assert.That(compVariantB!.StringDict.TryGetValue("key2", out var value2));
            Assert.That(value2, Is.EqualTo("string-3b"));

            Assert.That(compVariantB!.RecursiveRecord?.Child?.Child?.Value, Is.EqualTo("child-3"));
        });
    }

    const string DOCUMENT = $@"
- type: entity
  id: !type:CreateVariants
    values: [ TestPrototype, TestPrototypeVariantA, TestPrototypeVariantB ]  
  components:
  - type: PrototypeVariantizationTest
    numValue: !type:CreateVariants
      values: [ 1, 1.5, 2 ] 
    enumValue: !type:CreateVariants
      values: [ 0, 1, 2 ]
    stringValue: !type:CreateVariants
      values: [ string-1, string-2, string-3 ]
    stringArray: !type:CreateVariants
      sequences: [ [ el-1 ], [ el-1, el-2 ], [ el-1, el-2, el-3 ] ]
    stringDict:
      key1: !type:CreateVariants
        values: [ string-1a, string-2a, string-3a ]
      key2: !type:CreateVariants
        values: [ string-1b, string-2b, string-3b ]
    recursiveRecord: !type:RecursionTestRecord
      child:
        child:
          value: !type:CreateVariants
            values: [ child-1, child-2, child-3 ]
";
}

internal sealed partial class PrototypeVariantizationTestComponent : Component
{
    [DataField] public float NumValue = -1;
    [DataField] public PrototypeVariantizationTestEnum EnumValue = PrototypeVariantizationTestEnum.Invalid;
    [DataField] public string StringValue = string.Empty;
    [DataField] public string[] StringArray = Array.Empty<string>();
    [DataField] public Dictionary<string, string> StringDict = new();
    [DataField] public RecursionTestRecord RecursiveRecord = new();
}

[DataDefinition]
internal sealed partial record RecursionTestRecord
{
    [DataField] public RecursionTestRecord? Child = null;
    [DataField] public string Value = string.Empty;
}

public enum PrototypeVariantizationTestEnum : int
{
    Invalid = -1,
    First = 0,
    Second = 1,
    Third = 2,
}

