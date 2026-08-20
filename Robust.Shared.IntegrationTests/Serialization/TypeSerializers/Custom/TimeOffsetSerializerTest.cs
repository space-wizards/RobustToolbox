using System;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers.Custom;

[TestFixture]
[TestOf(typeof(TimeOffsetSerializer))]
internal sealed class TimeOffsetSerializerTest : OurSerializationTest
{
    [Test]
    public void ReadReturnsRawOffset()
    {
        var result = Serialization.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(new ValueDataNode("1"));

        Assert.That(result, Is.EqualTo(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void CreateCopyAppliesCurrentTime()
    {
        WithCurTime(TimeSpan.FromSeconds(10), () =>
        {
            Assert.That(
                Serialization.CreateCopy<TimeSpan, TimeOffsetSerializer>(TimeSpan.FromSeconds(1)),
                Is.EqualTo(TimeSpan.FromSeconds(11)));
        });
    }

    [Test]
    public void CreateCopyClampsOverflow()
    {
        WithCurTime(TimeSpan.FromSeconds(10), () =>
        {
            Assert.That(
                Serialization.CreateCopy<TimeSpan, TimeOffsetSerializer>(TimeSpan.MaxValue),
                Is.EqualTo(TimeSpan.MaxValue));
        });
    }

    private static void WithCurTime(TimeSpan curTime, Action action)
    {
        var timing = IoCManager.Resolve<IGameTiming>();
        var oldTimeBase = timing.TimeBase;
        var oldTickRemainder = timing.TickRemainder;

        try
        {
            timing.TimeBase = (curTime, timing.CurTick);
            timing.TickRemainder = TimeSpan.Zero;
            action();
        }
        finally
        {
            timing.TimeBase = oldTimeBase;
            timing.TickRemainder = oldTickRemainder;
        }
    }
}
