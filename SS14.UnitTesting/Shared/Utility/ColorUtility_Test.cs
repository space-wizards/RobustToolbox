using NUnit.Framework;
using SFML.Graphics;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;

namespace SS14.UnitTesting.Shared.Utility
{
    [TestFixture]
    public class ColorUtility_Test
    {
        private Random random;

        [OneTimeSetUp]
        public void Setup()
        {
            random = new Random();
        }
        // Values here are hue, sat, lum, RGB color expected.
        // Hue is in 0-360°, even though the method takes 0-1.
        // Sat and lum are in %, so 0-100.
        public static IEnumerable<(double h, double s, double l, Color rgb)> TestData = new List<(double, double, double, Color)>()
        {
            (0, 100, 100, new Color(255, 255, 255)),
            (180, 100, 100, new Color(255, 255, 255)),
            (0, 100, 50, new Color(255, 0, 0)),
            (60, 100, 50, new Color(255, 255, 0)),
            // Following colors were randomely picked using a Python script.
            (4, 38, 39, new Color(137, 67, 62)),
            (322, 22, 95, new Color(245, 239, 243)),
            (141, 18, 67, new Color(156, 186, 166)),
            (299, 84, 67, new Color(239, 100, 242)),
            (333, 57, 86, new Color(240, 199, 217)),
            (146, 88, 62, new Color(73, 243, 147)),
            (70, 4, 9, new Color(24, 24, 22)),
            (333, 68, 25, new Color(107, 20, 59)),
            (294, 26, 8, new Color(25, 15, 26)),
            (101, 60, 79, new Color(190, 234, 169)),
        };

        [Test, Sequential]
        public void HSLToRGB_Test([ValueSource(nameof(TestData))] (double h, double s, double l, Color rgb) data)
        {
            byte alpha = (byte)random.Next();
            data.rgb.A = alpha;
            Assert.That(ColorUtility.HSLToRGB(data.h / 360, data.s / 100, data.l / 100, alpha), Is.EqualTo(data.rgb));
        }
    }
}

