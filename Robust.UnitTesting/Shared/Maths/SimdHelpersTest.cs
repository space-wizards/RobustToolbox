using System.Runtime.Intrinsics;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths;

[TestFixture]
[Parallelizable]
[TestOf(typeof(SimdHelpers))]
internal sealed class SimdHelpersTest
{
    [Test]
    public void TestAddHorizontal128()
    {
        var vec = Vector128.Create(1f, 2f, 3f, 4f);

        var sum = SimdHelpers.AddHorizontal128(vec);

        var scalar = sum.GetElement(0);

        Assert.That(scalar, Is.EqualTo(10).Within(0.001f));
    }

    [Test]
    public void TestAddHorizontal256()
    {
        var vec = Vector256.Create(1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f);

        var sum = SimdHelpers.AddHorizontal256(vec);

        var scalar = sum.GetElement(0);

        Assert.That(scalar, Is.EqualTo(36).Within(0.001f));
    }
}
