using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers.Custom;

[TestFixture]
public sealed class TimeOffsetSerializerTest : RobustIntegrationTest
{
    [Test]
    public async Task SerializationTest()
    {
        var sim = StartServer();
        await sim.WaitIdleAsync();

        var serialization = sim.ResolveDependency<ISerializationManager>();

        await sim.WaitRunTicks(10);

        var curTime = sim.ResolveDependency<IGameTiming>().CurTime;

        Assert.That(curTime.TotalSeconds, Is.GreaterThan(0));

        var dataTime = curTime + TimeSpan.FromSeconds(2);
        var node = serialization.WriteWithTypeSerializer(typeof(TimeSpan), typeof(TimeOffsetSerializer), dataTime);
        Assert.That(((ValueDataNode) node).Value, Is.EqualTo("2"));
    }

    [Test]
    public async Task DeserializationTest()
    {
        var sim = StartServer();
        await sim.WaitIdleAsync();

        var serialization = sim.ResolveDependency<ISerializationManager>();

        await sim.WaitRunTicks(10);

        var curTime = sim.ResolveDependency<IGameTiming>().CurTime;

        var node = new ValueDataNode("2");
        var deserialized =
            serialization.ReadWithTypeSerializer(typeof(TimeSpan), typeof(TimeOffsetSerializer), node);

        Assert.That(deserialized, Is.Not.EqualTo(null));
        var time = (TimeSpan) deserialized!;

        Assert.That(time, Is.EqualTo(curTime + TimeSpan.FromSeconds(2)));
    }
}
