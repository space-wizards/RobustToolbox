using System;
using System.Diagnostics;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Utility;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(DebugTools))]
public sealed class DebugTools_Test
{
    [Test]
    [TestCase(true, 5)]
    [Conditional("DEBUG")]
    public void TestAssertFormatNoAlloc(bool check, int val)
    {
        var allocA = GC.GetAllocatedBytesForCurrentThread();

        DebugTools.Assert(check, $"Oops: {val}");

        var allocB = GC.GetAllocatedBytesForCurrentThread();

        Assert.That(allocB, Is.EqualTo(allocA));
    }
}
