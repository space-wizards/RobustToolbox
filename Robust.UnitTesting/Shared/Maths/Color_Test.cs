using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(Color))]
    public sealed class Color_Test
    {
        static IEnumerable<byte> BytesSource = new byte[]
        {
            0,
            1,
            31,
            32,
            63,
            64,
            127,
            128,
            254,
            255
        };

        static IEnumerable<(byte, byte, byte, byte)> FourBytesSource = BytesSource.SelectMany(b => new (byte, byte, byte, byte)[] {
            (b, 0, 0, 0),
            (0, b, 0, 0),
            (0, 0, b, 0),
            (0, 0, 0, b)
        }).Distinct();

        static IEnumerable<float> FloatsSource = BytesSource.Select(i => i / (float) byte.MaxValue);

        static IEnumerable<(float, float, float, float)> FourFloatsSource = FloatsSource.SelectMany(f => new (float, float, float, float)[] {
            (f, 0, 0, 0),
            (0, f, 0, 0),
            (0, 0, f, 0),
            (0, 0, 0, f)
        }).Distinct();

        private static IEnumerable<(string hex, Color expected)> HexColorsParsingSource = new[]
        {
            ("#FFFFFFFF", new Color(0xff, 0xff, 0xff)),
            ("#FFFFFF", new Color(0xff, 0xff, 0xff)),
            ("#12345678", new Color(0x12, 0x34, 0x56, 0x78)),
            ("#123456", new Color(0x12, 0x34, 0x56)),
            ("#FFF", new Color(0xff, 0xff, 0xff)),
            ("#AAA", new Color(0xaa, 0xaa, 0xaa)),
            ("#963", new Color(0x99, 0x66, 0x33)),
        };

        [Test, Sequential]
        public void ColorConstructorFloat([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats,
                                          [ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rf, gf, bf, af) = floats;
            var (rb, gb, bb, ab) = bytes;

            var color = new Color(rf, gf, bf, af);

            Assert.That(color.R, Is.EqualTo(rf));
            Assert.That(color.G, Is.EqualTo(gf));
            Assert.That(color.B, Is.EqualTo(bf));
            Assert.That(color.A, Is.EqualTo(af));

            Assert.That(color.RByte, Is.EqualTo(rb));
            Assert.That(color.GByte, Is.EqualTo(gb));
            Assert.That(color.BByte, Is.EqualTo(bb));
            Assert.That(color.AByte, Is.EqualTo(ab));
        }

        [Test, Sequential]
        public void ColorConstructorByte([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats,
                                         [ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rf, gf, bf, af) = floats;
            var (rb, gb, bb, ab) = bytes;

            var color = new Color(rb, gb, bb, ab);

            Assert.That(color.R, Is.EqualTo(rf));
            Assert.That(color.G, Is.EqualTo(gf));
            Assert.That(color.B, Is.EqualTo(bf));
            Assert.That(color.A, Is.EqualTo(af));

            Assert.That(color.RByte, Is.EqualTo(rb));
            Assert.That(color.GByte, Is.EqualTo(gb));
            Assert.That(color.BByte, Is.EqualTo(bb));
            Assert.That(color.AByte, Is.EqualTo(ab));
        }

        [Test]
        public void ToArgb([ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rb, gb, bb, ab) = bytes;

            var color = new Color(rb, gb, bb, ab);

            var argb = (uint) color.ToArgb();

            var aMask = (uint) byte.MaxValue << 24;
            var rMask = (uint) byte.MaxValue << 16;
            var gMask = (uint) byte.MaxValue << 8;
            var bMask = (uint) byte.MaxValue;

            Assert.That((argb & aMask) >> 24, Is.EqualTo(ab));
            Assert.That((argb & rMask) >> 16, Is.EqualTo(rb));
            Assert.That((argb & gMask) >> 8, Is.EqualTo(gb));
            Assert.That((argb & bMask), Is.EqualTo(bb));
        }

        [Test]
        public void ColorEquals([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var colorDiffRed = new Color(byte.MaxValue - rf, gf, bf, af);
            var colorDiffGreen = new Color(rf, byte.MaxValue - gf, bf, af);
            var colorDiffBlue = new Color(rf, gf, byte.MaxValue - bf, af);
            var colorDiffAlpha = new Color(rf, gf, bf, byte.MaxValue - af);
            var sameColor = new Color(rf, gf, bf, af);
            object sameColorAsObject = sameColor;
            Color? nullColor = null;
            UIBox2 notColor = new UIBox2(rf, gf, bf, af);

#pragma warning disable NUnit2009
            // This tests that .Equals actually works so ignoring the warning is fine.
            Assert.That(controlColor, Is.EqualTo(controlColor));
#pragma warning restore NUnit2009
            Assert.That(controlColor, Is.Not.EqualTo(colorDiffRed));
            Assert.That(controlColor, Is.Not.EqualTo(colorDiffGreen));
            Assert.That(controlColor, Is.Not.EqualTo(colorDiffBlue));
            Assert.That(controlColor, Is.Not.EqualTo(colorDiffAlpha));
            Assert.That(controlColor, Is.EqualTo(sameColor));
            Assert.That(controlColor, Is.EqualTo(sameColorAsObject));
            Assert.That(controlColor, Is.Not.EqualTo(nullColor));
            // NUnit's analyzer literally disallows this because it knows it's bogus, so...
            // Assert.That(controlColor, Is.Not.EqualTo(notColor));
        }

        [Test]
        public void ColorEqualsOperator([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var colorDiffRed = new Color(byte.MaxValue - rf, gf, bf, af);
            var colorDiffGreen = new Color(rf, byte.MaxValue - gf, bf, af);
            var colorDiffBlue = new Color(rf, gf, byte.MaxValue - bf, af);
            var colorDiffAlpha = new Color(rf, gf, bf, byte.MaxValue - af);
            var sameColor = new Color(rf, gf, bf, af);
            Color? nullColor = null;

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.That(controlColor == controlColor);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(controlColor == colorDiffRed, Is.False);
            Assert.That(controlColor == colorDiffGreen, Is.False);
            Assert.That(controlColor == colorDiffBlue, Is.False);
            Assert.That(controlColor == colorDiffAlpha, Is.False);
            Assert.That(controlColor == sameColor);
            Assert.That(controlColor == nullColor, Is.False);
        }

        [Test]
        public void ColorInequalsOperator([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var colorDiffRed = new Color(byte.MaxValue - rf, gf, bf, af);
            var colorDiffGreen = new Color(rf, byte.MaxValue - gf, bf, af);
            var colorDiffBlue = new Color(rf, gf, byte.MaxValue - bf, af);
            var colorDiffAlpha = new Color(rf, gf, bf, byte.MaxValue - af);
            var sameColor = new Color(rf, gf, bf, af);
            Color? nullColor = null;

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.That(controlColor != controlColor, Is.False);
#pragma warning restore CS1718 // Comparison made to same variable
            Assert.That(controlColor != colorDiffRed);
            Assert.That(controlColor != colorDiffGreen);
            Assert.That(controlColor != colorDiffBlue);
            Assert.That(controlColor != colorDiffAlpha);
            Assert.That(controlColor != sameColor, Is.False);
            Assert.That(controlColor != nullColor);
        }

        [Test]
        public void ColorToSystemDrawingColor([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var color = new Color(rf, gf, bf, af);
            var sysColor = System.Drawing.Color.FromArgb(color.ToArgb());

            Assert.That(color, Is.EqualTo((Color) sysColor));
            Assert.That(sysColor, Is.EqualTo((System.Drawing.Color) color));
        }

        static IEnumerable<string> DefaultColorNames => Color.GetAllDefaultColors().Select(e => e.Key);

        [Test]
        public void GetAllDefaultColorsFromName([ValueSource(nameof(DefaultColorNames))] string colorName)
        {
            var color = Color.FromName(colorName);
            var sysColor = System.Drawing.Color.FromName(colorName);

            Assert.That(color, Is.EqualTo((Color) sysColor));
        }

        [Test]
        public void GetColorFromNameExceptions()
        {
            const string name = "Definitely not a color name";

            Assert.Throws<KeyNotFoundException>(() => Color.FromName(name));
            Assert.DoesNotThrow(() => Color.TryFromName(name, out _));
        }

        [Test]
        public void WithRedFloat([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;
            var f = byte.MaxValue - rf;

            var color = new Color(rf, gf, bf, af);
            var controlColor = new Color(f, gf, bf, af);

            var colorWithRed = color.WithRed(f);

            Assert.That(colorWithRed, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithGreenFloat([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;
            var f = byte.MaxValue - gf;

            var color = new Color(rf, gf, bf, af);
            var controlColor = new Color(rf, f, bf, af);

            var colorWithGreen = color.WithGreen(f);

            Assert.That(colorWithGreen, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithBlueFloat([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;
            var f = byte.MaxValue - bf;

            var color = new Color(rf, gf, bf, af);
            var controlColor = new Color(rf, gf, f, af);

            var colorWithBlue = color.WithBlue(f);

            Assert.That(colorWithBlue, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithAlphaFloat([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;
            var f = byte.MaxValue - af;

            var color = new Color(rf, gf, bf, af);
            var controlColor = new Color(rf, gf, bf, f);

            var colorWithAlpha = color.WithAlpha(f);

            Assert.That(colorWithAlpha, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithRedByte([ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rb, gb, bb, ab) = bytes;
            var b = (byte)(byte.MaxValue - rb);

            var color = new Color(rb, gb, bb, ab);
            var controlColor = new Color(b, gb, bb, ab);

            var colorWithRed = color.WithRed(b);

            Assert.That(colorWithRed, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithGreenByte([ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rb, gb, bb, ab) = bytes;
            var b = (byte)(byte.MaxValue - gb);

            var color = new Color(rb, gb, bb, ab);
            var controlColor = new Color(rb, b, bb, ab);

            var colorWithGreen = color.WithGreen(b);

            Assert.That(colorWithGreen, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithBlueByte([ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rb, gb, bb, ab) = bytes;
            var b = (byte)(byte.MaxValue - bb);

            var color = new Color(rb, gb, bb, ab);
            var controlColor = new Color(rb, gb, b, ab);

            var colorWithBlue = color.WithBlue(b);

            Assert.That(colorWithBlue, Is.EqualTo(controlColor));
        }

        [Test]
        public void WithAlphaByte([ValueSource(nameof(FourBytesSource))] (byte, byte, byte, byte) bytes)
        {
            var (rb, gb, bb, ab) = bytes;
            var b = (byte) (byte.MaxValue - ab);

            var color = new Color(rb, gb, bb, ab);
            var controlColor = new Color(rb, gb, bb, b);

            var colorWithAlpha = color.WithAlpha(b);

            Assert.That(colorWithAlpha, Is.EqualTo(controlColor));
        }

        [Test]
        public void ToFromSrgb([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var color = Color.FromSrgb(Color.ToSrgb(controlColor));

            Assert.That(color, Is.EqualTo(controlColor));
        }

        [Test]
        public void ToFromHsl([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var color = Color.FromHsl(Color.ToHsl(controlColor));

            Assert.That(color, Is.EqualTo(controlColor));
        }

        [Test]
        public void ToFromHsv([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var color = Color.FromHsv(Color.ToHsv(controlColor));

            Assert.That(color, Is.EqualTo(controlColor));
        }

        [Test]
        public void ToFromXyz([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            const float tolerance = 1e-4f;

            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var color = Color.FromXyz(Color.ToXyz(controlColor));

            Assert.That(color.R, Is.EqualTo(controlColor.R).Within(tolerance));
            Assert.That(color.G, Is.EqualTo(controlColor.G).Within(tolerance));
            Assert.That(color.B, Is.EqualTo(controlColor.B).Within(tolerance));
            Assert.That(color.A, Is.EqualTo(controlColor.A).Within(tolerance));
        }

        [Test]
        public void ToFromYcbcr([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var color = Color.FromYcbcr(Color.ToYcbcr(controlColor));

            Assert.That(color, Is.EqualTo(controlColor));
        }

        [Test]
        public void ToFromHcy([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats)
        {
            var (rf, gf, bf, af) = floats;

            var controlColor = new Color(rf, gf, bf, af);
            var color = Color.FromHcy(Color.ToHcy(controlColor));

            Assert.That(color, Is.EqualTo(controlColor));
        }

        static IEnumerable<float> InterpolationValues => new float[]
        {
            0f,
            0.2f,
            0.4f,
            0.6f,
            0.8f,
            1f
        };

        [Test]
        public void InterpolateBetween([ValueSource(nameof(FourFloatsSource))] (float, float, float, float) floats,
                                       [ValueSource(nameof(InterpolationValues))] float interpolation)
        {
            var (r1, g1, b1, a1) = floats;
            var (b2, a2, r2, g2) = floats;

            var color1 = new Color(r1, g1, b1, a1);
            var color2 = new Color(r2, g2, b2, a2);

            var interColor = Color.InterpolateBetween(color1, color2, interpolation);
            var inverseInterColor = Color.InterpolateBetween(color2, color1, 1 - interpolation);

            Assert.That(interColor, Is.EqualTo(inverseInterColor));
        }

        [Test]
        public void FromHexThrows()
        {
            Assert.Throws<ArgumentException>(() => Color.FromHex(" "));
            Assert.Throws<ArgumentException>(() => Color.FromHex("#aaaaaaaaa"));
            Assert.Throws<ArgumentException>(() => Color.FromHex("#aaaaaaa"));
            Assert.Throws<ArgumentException>(() => Color.FromHex("#aaaaa"));
            Assert.Throws<ArgumentException>(() => Color.FromHex("#aa"));
            Assert.Throws<ArgumentException>(() => Color.FromHex("#a"));
            Assert.Throws<ArgumentException>(() => Color.FromHex("#"));
            Assert.Throws<ArgumentException>(() => Color.FromHex(""));
        }

        [Test]
        public void FromHex([ValueSource(nameof(HexColorsParsingSource))] (string hex, Color expected) data)
        {
            var (hex, expected) = data;

            Assert.That(Color.FromHex(hex), Is.EqualTo(expected));
        }
    }
}
