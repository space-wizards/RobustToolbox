using System;
using System.Globalization;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
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
        var timing = sim.ResolveDependency<IGameTiming>();
        var entMan = sim.ResolveDependency<IEntityManager>();
        var ctx = new MapSerializationContext(entMan, timing);

        await sim.WaitRunTicks(10);
        Assert.That(timing.CurTime.TotalSeconds, Is.GreaterThan(0));

        // "pause" a map at this time
        var pauseTime = timing.CurTime;
        await sim.WaitRunTicks(10);

        // Spawn a paused entity
        var uid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        var metaSys = entMan.System<MetaDataSystem>();
        metaSys.SetEntityPaused(uid, true);

        await sim.WaitRunTicks(10);
        Assert.That(metaSys.GetPauseTime(uid).TotalSeconds, Is.GreaterThan(0));

        var curTime = timing.CurTime;
        var dataTime = curTime + TimeSpan.FromSeconds(2);
        ctx.PauseTime = curTime - pauseTime;
        var entPauseDuration = metaSys.GetPauseTime(uid);

        Assert.That(curTime.TotalSeconds, Is.GreaterThan(0));
        Assert.That(entPauseDuration.TotalSeconds, Is.GreaterThan(0));
        Assert.That(ctx.PauseTime.TotalSeconds, Is.GreaterThan(0));

        Assert.That(ctx.PauseTime, Is.Not.EqualTo(curTime));
        Assert.That(ctx.PauseTime, Is.Not.EqualTo(entPauseDuration));
        Assert.That(entPauseDuration, Is.Not.EqualTo(curTime));

        // time gets properly offset when reading a post-init map
        ctx.MapInitialized = true;
        var node = serialization.WriteValue<TimeSpan, TimeOffsetSerializer>(dataTime, context: ctx);
        var value = ((ValueDataNode) node).Value;
        var expected = (dataTime - curTime + ctx.PauseTime).TotalSeconds.ToString(CultureInfo.InvariantCulture);
        Assert.That(value, Is.EqualTo(expected));

        // When writing paused entities, it will instead use the entity's pause time:
        ctx.CurrentWritingEntity = uid;
        node = serialization.WriteValue<TimeSpan, TimeOffsetSerializer>(dataTime, context: ctx);
        value = ((ValueDataNode) node).Value;
        expected = (dataTime - curTime + entPauseDuration).TotalSeconds.ToString(CultureInfo.InvariantCulture);
        Assert.That(value, Is.EqualTo(expected));

        // Uninitialized maps always serialize as zero
        ctx.MapInitialized = false;
        node = serialization.WriteValue<TimeSpan, TimeOffsetSerializer>(dataTime, context: ctx);
        value = ((ValueDataNode) node).Value;
        Assert.That(value, Is.EqualTo("0"));

        ctx.CurrentWritingEntity = null;
        node = serialization.WriteValue<TimeSpan, TimeOffsetSerializer>(dataTime, context: ctx);
        value = ((ValueDataNode) node).Value;
        Assert.That(value, Is.EqualTo("0"));
    }

    [Test]
    public async Task DeserializationTest()
    {
        var sim = StartServer();
        await sim.WaitIdleAsync();

        var serialization = sim.ResolveDependency<ISerializationManager>();

        await sim.WaitRunTicks(10);

        var timing = sim.ResolveDependency<IGameTiming>();
        var entMan = sim.ResolveDependency<IEntityManager>();
        var ctx = new MapSerializationContext(entMan, timing);
        var curTime = timing.CurTime;
        var node = new ValueDataNode("2");

        // time gets properly offset when reading a post-init map
        ctx.MapInitialized = true;
        var time = serialization.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(node, ctx);
        Assert.That(time, Is.EqualTo(curTime + TimeSpan.FromSeconds(2)));

        // pre-init maps read time offsets as 0.
        ctx.MapInitialized = false;
        time = serialization.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(node, ctx);
        Assert.That(time, Is.EqualTo(TimeSpan.Zero));

        // Same goes for no-context reads
        time = serialization.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(node);
        Assert.That(time, Is.EqualTo(TimeSpan.Zero));
    }
}
