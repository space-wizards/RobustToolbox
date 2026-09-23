using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Tests.Utility;

[Parallelizable(ParallelScope.All)]
[TestOf(typeof(ColorExtensions))]
internal sealed class ColorExtensionsTest
{
    [Test]
    public void TestAnalogousPalette()
    {
        var palette = ColorExtensions.GetAnalogousComplementaries(Color.Red);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(palette, Has.Length.EqualTo(3));
            Assert.That(MathHelper.CloseToPercent(palette[0], Color.Red));

            Assert.That(Color.ToHsl(palette[0]).X, Is.Zero);
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[1]).X, 30f / 360f));
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[2]).X, 1f - 30f / 360f));
        }
    }

    [Test]
    public void TestTriadicPalette()
    {
        var palette = ColorExtensions.GetTriadicComplementaries(Color.Red);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(palette, Has.Length.EqualTo(3));
            Assert.That(MathHelper.CloseToPercent(palette[0], Color.Red));

            Assert.That(Color.ToHsl(palette[0]).X, Is.Zero);
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[1]).X, 120f / 360f));
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[2]).X, 1f - 120f / 360f));
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

            Assert.That(Color.ToHsl(palette[0]).X, Is.Zero);
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[1]).X, 150f / 360f));
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[2]).X, 1f - 150f / 360f));
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

            Assert.That(Color.ToHsl(palette[0]).X, Is.Zero);
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[1]).X, 180f / 360f));
            Assert.That(MathHelper.CloseToPercent(Color.ToHsl(palette[2]).X, 1f - 180f / 360f));
        }
    }
}
