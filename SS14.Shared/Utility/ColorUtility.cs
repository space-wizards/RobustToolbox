using SFML.Graphics;
using System;

namespace SS14.Shared.Utility
{
    public static class ColorUtility
    {

        // http://www.geekymonkey.com/Programming/CSharp/RGB2HSL_HSL2RGB.htm
        /// <summary>
        /// Converts a HSL color value to an RGB color value.
        /// The HSL colors are inputs in range 0-1 always.
        /// </summary>
        /// <param name="hue">The hue of the color.</param>
        /// <param name="saturation">The saturation of the color.</param>
        /// <param name="luminance">The luminance of the color.</param>
        /// <param name="alpha">The alpha of the resulting color.</param>
        /// <returns>An SFML color with the RGB values set.</returns>
        public static Color HSLToRGB(double hue, double saturation, double luminance, byte alpha=255)
        {
            // default to gray
            var r = luminance;
            var g = luminance;
            var b = luminance;
            var v = (luminance <= 0.5) ? (luminance * (1.0 + saturation)) : (luminance + saturation - luminance * saturation);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = luminance + luminance - v;
                sv = (v - m ) / v;
                hue *= 6.0;
                sextant = (int)hue;
                fract = hue - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }

            Color rgb = new Color()
            {
                R = Convert.ToByte(r * 255.0f),
                G = Convert.ToByte(g * 255.0f),
                B = Convert.ToByte(b * 255.0f),
                A = alpha,
            };
            return rgb;
        }
    }
}
