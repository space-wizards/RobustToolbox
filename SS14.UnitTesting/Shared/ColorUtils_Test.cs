using NUnit.Framework;
using OpenTK.Graphics;
using SS14.Shared;
using System;

namespace SS14.UnitTesting.Shared
{
    [TestFixture]
    public class ColorUtils_Test
    {
        [Test]
        public void TestInterpolateBetween()
        {
            var black = Color4.Black;
            var white = Color4.White;
            Assert.That(() => ColorUtils.InterpolateBetween(black, white, -10), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => ColorUtils.InterpolateBetween(black, white, 10), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(ColorUtils.InterpolateBetween(black, white, 0), Is.EqualTo(white));
            Assert.That(ColorUtils.InterpolateBetween(black, white, 1), Is.EqualTo(black));
            // Should be grey but floating points hate us so...
            // The byte conversion shouldn't have issues because the error marging should be small enough.
            var grey = ColorUtils.InterpolateBetween(black, white, 0.5);
            Assert.That(grey.RByte(), Is.EqualTo(127));
            Assert.That(grey.GByte(), Is.EqualTo(127));
            Assert.That(grey.BByte(), Is.EqualTo(127));
        }

        [Test]
        public void TestWithAlpha()
        {
            Assert.That(Color4.White.WithAlpha(128), Is.EqualTo(new Color4(255, 255, 255, 128)));
            Assert.That(Color4.White.WithAlpha(0.5f), Is.EqualTo(new Color4(1, 1, 1, 0.5f)));
        }

        [Test]
        public void TestFromHex()
        {
            // Test fallback.
            Assert.That(ColorUtils.FromHex("honk", Color4.AliceBlue), Is.EqualTo(Color4.AliceBlue));
            Assert.That(() => ColorUtils.FromHex("honk"), Throws.ArgumentException);
            Assert.That(() => ColorUtils.FromHex("#FFFFFFFF00"), Throws.ArgumentException);

            Assert.That(ColorUtils.FromHex("#FFFFFF"), Is.EqualTo(Color4.White));
            Assert.That(ColorUtils.FromHex("#FFFFFFFF"), Is.EqualTo(Color4.White));
            Assert.That(ColorUtils.FromHex("#FFFFFF69"), Is.EqualTo(Color4.White.WithAlpha(0x69)));
            Assert.That(ColorUtils.FromHex("#FFF"), Is.EqualTo(Color4.White));
            Assert.That(ColorUtils.FromHex("#FFF8"), Is.EqualTo(Color4.White.WithAlpha(0x88)));
            Assert.That(ColorUtils.FromHex("#ABCDEF"), Is.EqualTo(new Color4(0xAB, 0xCD, 0xEF, 0xFF)));
        }

        [Test]
        public void TestByteParts()
        {
            var color = new Color4(23, 38, 18, 20);
            Assert.That(color.RByte(), Is.EqualTo(23));
            Assert.That(color.GByte(), Is.EqualTo(38));
            Assert.That(color.BByte(), Is.EqualTo(18));
            Assert.That(color.AByte(), Is.EqualTo(20));
        }
    }
}
