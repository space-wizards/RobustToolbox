using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
public sealed class MapBitmask_Tests
{
    private static readonly TestCaseData[] Cases = new[]
    {
        new TestCaseData(Vector2i.Zero, SharedMapSystem.ToBitmask(Vector2i.Zero), true),

        new TestCaseData(Vector2i.One * 7, SharedMapSystem.ToBitmask(Vector2i.One * 7), true),

        new TestCaseData(Vector2i.One * 7, SharedMapSystem.ToBitmask(Vector2i.Zero), false),
    };

    [Test, TestCaseSource(nameof(Cases))]
    public void TestBitmask(Vector2i index, ulong bitmask, bool result)
    {
        Assert.That(SharedMapSystem.FromBitmask(index, bitmask), Is.EqualTo(result));
    }
}
