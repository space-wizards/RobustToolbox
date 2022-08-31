using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed class DataRecordTest : RobustUnitTest
{
    [DataRecord]
    public record TestRecord1(int aTest, int AnotherTest);

    [Test]
    public void TestReadWrite()
    {
        var serv3Mgr = IoCManager.Resolve<ISerializationManager>();
        serv3Mgr.Initialize();

        var mapping = new MappingDataNode();
        mapping.Add("aTest", "1");
        mapping.Add("anotherTest", "2");

        var val = serv3Mgr.Read<TestRecord1>(mapping);

        Assert.That(val.aTest, Is.EqualTo(1));
        Assert.That(val.AnotherTest, Is.EqualTo(2));

        var newMapping = serv3Mgr.WriteValueAs<MappingDataNode>(val);

        Assert.That(newMapping.Count, Is.EqualTo(2));
        Assert.That(newMapping.TryGet<ValueDataNode>("aTest", out var node1));
        Assert.That(node1!.Value, Is.EqualTo("1"));
        Assert.That(newMapping.TryGet<ValueDataNode>("anotherTest", out var node2));
        Assert.That(node2!.Value.ToLower(), Is.EqualTo("2"));
    }
}
