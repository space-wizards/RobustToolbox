using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

[TestFixture]
public sealed partial class IncludeTest : RobustUnitTest
{
    [DataDefinition]
    private sealed partial class ReadWriteTestDataDefinition
    {
        [DataField("f1")] public int F1;

        [IncludeDataField] public ReadWriteTestNestedDataDefinition Nested = default!;
    }

    [DataDefinition]
    private sealed partial class ReadWriteTestNestedDataDefinition
    {
        [DataField("f1")] public int F1;

        [DataField("f2")] public bool F2;
    }

    [Test]
    public void TestReadWrite()
    {
        var serv3Mgr = IoCManager.Resolve<ISerializationManager>();
        serv3Mgr.Initialize();

        var mapping = new MappingDataNode();
        mapping.Add("f1", "1");
        mapping.Add("f2", "true");

        var val = serv3Mgr.Read<ReadWriteTestDataDefinition>(mapping, notNullableOverride: true);

        Assert.That(val.F1, Is.EqualTo(1));
        Assert.That(val.Nested.F1, Is.EqualTo(1));
        Assert.That(val.Nested.F2, Is.EqualTo(true));

        var newMapping = serv3Mgr.WriteValueAs<MappingDataNode>(val);

        Assert.That(newMapping.Count, Is.EqualTo(2));
        Assert.That(newMapping.TryGet<ValueDataNode>("f1", out var f1Node));
        Assert.That(f1Node!.Value, Is.EqualTo("1"));
        Assert.That(newMapping.TryGet<ValueDataNode>("f2", out var f2Node));
        Assert.That(f2Node!.Value.ToLower(), Is.EqualTo("true"));
    }

    [Test]
    public void TestPushComposition()
    {
        //todo paul
    }
}
