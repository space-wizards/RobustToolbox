using Robust.Shared.Maths;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    public sealed class ColorUtils_Test
    {
        [Test]
        public void TestInterpolateBetween()
        {
            var black = Color.Black;
            var white = Color.White;

            Assert.That(Color.InterpolateBetween(black, white, 1), Is.EqualTo(white));
            Assert.That(Color.InterpolateBetween(black, white, 0), Is.EqualTo(black));
            // Should be grey but floating points hate us so...
            // The byte conversion shouldn't have issues because the error marging should be small enough.
            var grey = Color.InterpolateBetween(black, white, 0.5f);
            Assert.That(grey.RByte, Is.EqualTo(127));
            Assert.That(grey.GByte, Is.EqualTo(127));
            Assert.That(grey.BByte, Is.EqualTo(127));
        }

        [Test]
        public void TestWithAlpha()
        {
            Assert.That(Color.White.WithAlpha(128), Is.EqualTo(new Color(255, 255, 255, 128)));
            Assert.That(Color.White.WithAlpha(0.5f), Is.EqualTo(new Color(1, 1, 1, 0.5f)));
        }

        [Test]
        public void TestFromHex()
        {
            // Test fallback.
            Assert.That(Color.FromHex("honk", Color.AliceBlue), Is.EqualTo(Color.AliceBlue));
            Assert.That(() => Color.FromHex("honk"), Throws.ArgumentException);
            Assert.That(() => Color.FromHex("#FFFFFFFF00"), Throws.ArgumentException);

            Assert.That(Color.FromHex("#FFFFFF"), Is.EqualTo(Color.White));
            Assert.That(Color.FromHex("#FFFFFFFF"), Is.EqualTo(Color.White));
            Assert.That(Color.FromHex("#FFFFFF69"), Is.EqualTo(Color.White.WithAlpha(0x69)));
            Assert.That(Color.FromHex("#FFF"), Is.EqualTo(Color.White));
            Assert.That(Color.FromHex("#FFF8"), Is.EqualTo(Color.White.WithAlpha(0x88)));
            Assert.That(Color.FromHex("#ABCDEF"), Is.EqualTo(new Color(0xAB, 0xCD, 0xEF)));
        }

        [Test]
        public void TestByteParts()
        {
            var color = new Color(23, 38, 18, 20);
            Assert.That(color.RByte, Is.EqualTo(23));
            Assert.That(color.GByte, Is.EqualTo(38));
            Assert.That(color.BByte, Is.EqualTo(18));
            Assert.That(color.AByte, Is.EqualTo(20));
        }
    }
}
