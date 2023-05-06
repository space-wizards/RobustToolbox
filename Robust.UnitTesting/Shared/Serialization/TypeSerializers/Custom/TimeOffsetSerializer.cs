using System;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

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
        var node = serialization.WriteValue<TimeSpan, TimeOffsetSerializer>(dataTime);
        Assert.That(((ValueDataNode) node).Value, Is.EqualTo(dataTime.TotalSeconds.ToString(CultureInfo.InvariantCulture)));
    }

    [Test]
    public async Task DeserializationTest()
    {
        var sim = StartServer();
        await sim.WaitIdleAsync();

        var serialization = sim.ResolveDependency<ISerializationManager>();

        await sim.WaitRunTicks(10);

        var curTime = sim.ResolveDependency<IGameTiming>().CurTime;
        var serializer = new TimeOffsetSerializer();

        var node = new ValueDataNode("2");
        var time = TimeSpan.Zero;
        var basic = serialization.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(node);
        serialization.CopyTo(serializer, basic, ref time);
        Assert.That(time, Is.EqualTo(curTime + TimeSpan.FromSeconds(2)));
    }
}
