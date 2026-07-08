using System;
using System.Collections.Generic;
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
    private static readonly TimeSpan CopyCurTime = TimeSpan.FromSeconds(10);

    private static IEnumerable<TestCaseData> CopyTestCases()
    {
        yield return new TestCaseData(TimeSpan.FromSeconds(1), CopyCurTime + TimeSpan.FromSeconds(1))
            .SetName("Copy applies current time");
        yield return new TestCaseData(TimeSpan.Zero, CopyCurTime)
            .SetName("Copy zero applies current time");
        yield return new TestCaseData(TimeSpan.FromSeconds(-1), CopyCurTime + TimeSpan.FromSeconds(-1))
            .SetName("Copy negative applies current time");
        yield return new TestCaseData(TimeSpan.MaxValue, TimeSpan.MaxValue)
            .SetName("Copy max value clamps to max value");
    }

    private static IEnumerable<TestCaseData> ApplyOffsetTestCases()
    {
        yield return new TestCaseData(TimeSpan.MaxValue, TimeSpan.FromSeconds(10), TimeSpan.MaxValue)
            .SetName("Apply offset max value clamps to max value");
        yield return new TestCaseData(TimeSpan.MinValue, TimeSpan.FromSeconds(-10), TimeSpan.MinValue)
            .SetName("Apply offset min value clamps to min value");
    }

    [Test]
    public void ReadReturnsRawOffset()
    {
        var result = Serialization.Read<TimeSpan, ValueDataNode, TimeOffsetSerializer>(new ValueDataNode("1"));

        Assert.That(result, Is.EqualTo(TimeSpan.FromSeconds(1)));
    }

    [TestCaseSource(nameof(CopyTestCases))]
    public void CopyAppliesCurrentTime(TimeSpan source, TimeSpan expected)
    {
        var timing = IoCManager.Resolve<IGameTiming>();
        timing.TimeBase = (CopyCurTime, timing.CurTick);
        timing.TickRemainder = TimeSpan.Zero;

        var result = Serialization.CreateCopy<TimeSpan, TimeOffsetSerializer>(source);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(ApplyOffsetTestCases))]
    public void ApplyOffsetClamps(TimeSpan source, TimeSpan offset, TimeSpan expected)
    {
        Assert.That(TimeOffsetSerializer.ApplyOffset(source, offset), Is.EqualTo(expected));
    }
}
