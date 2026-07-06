using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Tests.Utility;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(ColorExtensions))]
internal sealed class ColorExtensionsTest
{
    [Test]
    public void TestTriadicPalette()
    {
        var palette = ColorExtensions.GetTriadicComplementaries(Color.Red);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(palette, Has.Length.EqualTo(3));
            Assert.That(MathHelper.CloseToPercent(palette[0], Color.Red));

            Assert.That(Color.ToHsl(palette[1]).X, Is.EqualTo(0.33333f));
            Assert.That(Color.ToHsl(palette[2]).X, Is.EqualTo(0.66667f));
        }
    }

    [Test]
    public void TestSplitComplementaryPalette()
    {
        var palette = ColorExtensions.GetSplitComplementaries(Color.Red);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(palette, Has.Length.EqualTo(3));
            Assert.That(MathHelper.CloseToPercent(palette[0], Color.Red));

            Assert.That(Color.ToHsl(palette[1]).X, Is.EqualTo(0.41667f));
            Assert.That(Color.ToHsl(palette[2]).X, Is.InRange(0.58333f, 0.58334f)); // TODO FIX THIS
        }
    }

    [Test]
    public void TestComplementaryPalette()
    {
        var palette = ColorExtensions.GetOneComplementary(Color.Red);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(palette, Has.Length.EqualTo(3));
            Assert.That(MathHelper.CloseToPercent(palette[0], Color.Red));

            Assert.That(Color.ToHsl(palette[1]).X, Is.EqualTo(0.5f));
            Assert.That(Color.ToHsl(palette[2]).X, Is.EqualTo(0.5f));
        }
    }
}
