using SFML.Graphics;
using System;

namespace SS14.Shared
{
    public static class ColorUtils
    {
        public static Color InterpolateBetween(Color endPoint1, Color endPoint2, double lambda)
        {
            if (lambda < 0 || lambda > 1)
            {
                throw new ArgumentOutOfRangeException("lambda");
            }
            return new Color(
                (byte)(endPoint1.R * lambda + endPoint2.R * (1 - lambda)),
                (byte)(endPoint1.G * lambda + endPoint2.G * (1 - lambda)),
                (byte)(endPoint1.B * lambda + endPoint2.B * (1 - lambda)),
                (byte)(endPoint1.A * lambda + endPoint2.A * (1 - lambda))
                );
        }

        public static Color WithAlpha(this Color color, byte a)
        {
            color.A = a;
            return color;
        }

        public static Color FromHex(string hexColor, Color? fallback = null)
        {
            if (hexColor[0] == '#')
            {

                if (hexColor.Length == 9)
                {
                    return new Color(Convert.ToByte(hexColor.Substring(1, 2), 16),
                                     Convert.ToByte(hexColor.Substring(3, 2), 16),
                                     Convert.ToByte(hexColor.Substring(5, 2), 16),
                                     Convert.ToByte(hexColor.Substring(7, 2), 16));
                }
                else if (hexColor.Length == 7)
                {
                    return new Color(Convert.ToByte(hexColor.Substring(1, 2), 16),
                                     Convert.ToByte(hexColor.Substring(3, 2), 16),
                                     Convert.ToByte(hexColor.Substring(5, 2), 16));
                }
                else if (hexColor.Length == 4)
                {
                    string r = hexColor[1].ToString();
                    string g = hexColor[2].ToString();
                    string b = hexColor[3].ToString();
                    string a = hexColor[4].ToString();

                    return new Color(Convert.ToByte(r + r, 16),
                                     Convert.ToByte(g + g, 16),
                                     Convert.ToByte(b + b, 16),
                                     Convert.ToByte(a + a, 16));
                }
                else if (hexColor.Length == 3)
                {
                    string r = hexColor[1].ToString();
                    string g = hexColor[2].ToString();
                    string b = hexColor[3].ToString();

                    return new Color(Convert.ToByte(r + r, 16),
                                     Convert.ToByte(g + g, 16),
                                     Convert.ToByte(b + b, 16));
                }
            }

            if (fallback.HasValue)
                return fallback.Value;
            else
                throw new ArgumentException("Invalid color code.", "hexColor");
        }
    }
}
