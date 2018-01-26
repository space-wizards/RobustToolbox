using System;
using SystemColor = System.Drawing.Color;

namespace SS14.Shared.Maths
{
    [Serializable]
    public struct Color : IEquatable<Color>
    {
        // TODO: Use our own implementation of all this logic.
        private readonly Color4 _color;

        public float R => _color.R;
        public float G => _color.G;
        public float B => _color.B;
        public float A => _color.A;

        public byte RByte => (byte)(R * byte.MaxValue);
        public byte GByte => (byte)(G * byte.MaxValue);
        public byte BByte => (byte)(B * byte.MaxValue);
        public byte AByte => (byte)(A * byte.MaxValue);

        public Color(float r, float g, float b, float a = 1)
        {
            _color = new Color4(r, g, b, a);
        }

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            _color = new Color4(r, g, b, a);
        }

        // Used by the casts themselves.
        // Use the implicit cast for other things.
        private Color(Color4 color)
        {
            _color = color;
        }

        public int ToArgb()
        {
            return _color.ToArgb();
        }

        public static implicit operator Color(Color4 color)
        {
            return new Color(color);
        }

        public static implicit operator Color4(Color color)
        {
            return color._color;
        }

        public static implicit operator Color(SystemColor color)
        {
            return new Color(color);
        }

        public Color WithRed(float newR)
        {
            return new Color(newR, G, B, A);
        }

        public Color WithGreen(float newG)
        {
            return new Color(R, newG, B, A);
        }

        public Color WithBlue(float newB)
        {
            return new Color(R, G, newB, A);
        }

        public Color WithAlpha(float newA)
        {
            return new Color(R, G, B, newA);
        }

        public Color WithRed(byte newR)
        {
            return new Color((float)newR / byte.MaxValue, G, B, A);
        }

        public Color WithGreen(byte newG)
        {
            return new Color(R, (float)newG / byte.MaxValue, B, A);
        }

        public Color WithBlue(byte newB)
        {
            return new Color(R, G, (float)newB / byte.MaxValue, A);
        }

        public Color WithAlpha(byte newA)
        {
            return new Color(R, G, B, (float)newA / byte.MaxValue);
        }

        /// <summary>
        /// Interpolate two colors with a lambda, AKA returning the two colors combined with a ratio of <paramref name="lambda" />.
        /// </summary>
        /// <param name="lambda">
        /// A value ranging from 0-1. The higher the value the more is taken from <paramref name="endPoint1" />,
        /// with 0.5 being 50% of both colors, 0.25 being 25% of <paramref name="endPoint1" /> and 75% <paramref name="endPoint2" />.
        /// </param>
        public static Color InterpolateBetween(Color endPoint1, Color endPoint2, double lambda)
        {
            if (lambda < 0 || lambda > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lambda));
            }
            return new Color(
                (float)(endPoint1.R * lambda + endPoint2.R * (1 - lambda)),
                (float)(endPoint1.G * lambda + endPoint2.G * (1 - lambda)),
                (float)(endPoint1.B * lambda + endPoint2.B * (1 - lambda)),
                (float)(endPoint1.A * lambda + endPoint2.A * (1 - lambda))
            );
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
                else if (hexColor.Length == 5)
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
                else if (hexColor.Length == 4)
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
                throw new ArgumentException("Invalid color code and no fallback provided.", nameof(hexColor));
        }

        public static Color FromSrgb(Color color)
        {
            return Color4.FromSrgb(color);
        }

        public static Color ToSrgb(Color color)
        {
            return Color4.ToSrgb(color);
        }

        public static Color FromHsl(Vector4 vec)
        {
            return Color4.FromHsl(vec);
        }

        public static Vector4 ToHsl(Color color)
        {
            return Color4.ToHsl(color);
        }

        public static Color FromHsv(Vector4 vec)
        {
            return Color4.FromHsv(vec);
        }

        public static Vector4 ToHsv(Color color)
        {
            return Color4.ToHsv(color);
        }

        public override string ToString()
        {
            return _color.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is Color color && Equals(color);
        }

        public bool Equals(Color other)
        {
            return _color.Equals(other._color);
        }

        public override int GetHashCode()
        {
            return _color.GetHashCode();
        }

        public static bool operator ==(Color color1, Color color2)
        {
            return color1.Equals(color2);
        }

        public static bool operator !=(Color color1, Color color2)
        {
            return !(color1 == color2);
        }

        /// <summary>
        /// (R, G, B, A) = (255, 255, 255, 0)
        /// </summary>
        public static Color Transparent => new Color(255, 255, 255, 0);

        /// <summary>
        /// (R, G, B, A) = (240, 248, 255, 255)
        /// </summary>
        public static Color AliceBlue => new Color(240, 248, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (250, 235, 215, 255)
        /// </summary>
        public static Color AntiqueWhite => new Color(250, 235, 215, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 255, 255, 255)
        /// </summary>
        public static Color Aqua => new Color(0, 255, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (127, 255, 212, 255)
        /// </summary>
        public static Color Aquamarine => new Color(127, 255, 212, 255);

        /// <summary>
        /// (R, G, B, A) = (240, 255, 255, 255)
        /// </summary>
        public static Color Azure => new Color(240, 255, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (245, 245, 220, 255)
        /// </summary>
        public static Color Beige => new Color(245, 245, 220, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 228, 196, 255)
        /// </summary>
        public static Color Bisque => new Color(255, 228, 196, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 0, 0, 255)
        /// </summary>
        public static Color Black => new Color(0, 0, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 235, 205, 255)
        /// </summary>
        public static Color BlanchedAlmond => new Color(255, 235, 205, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 0, 255, 255)
        /// </summary>
        public static Color Blue => new Color(0, 0, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (138, 43, 226, 255)
        /// </summary>
        public static Color BlueViolet => new Color(138, 43, 226, 255);

        /// <summary>
        /// (R, G, B, A) = (165, 42, 42, 255)
        /// </summary>
        public static Color Brown => new Color(165, 42, 42, 255);

        /// <summary>
        /// (R, G, B, A) = (222, 184, 135, 255)
        /// </summary>
        public static Color BurlyWood => new Color(222, 184, 135, 255);

        /// <summary>
        /// (R, G, B, A) = (95, 158, 160, 255)
        /// </summary>
        public static Color CadetBlue => new Color(95, 158, 160, 255);

        /// <summary>
        /// (R, G, B, A) = (127, 255, 0, 255)
        /// </summary>
        public static Color Chartreuse => new Color(127, 255, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (210, 105, 30, 255)
        /// </summary>
        public static Color Chocolate => new Color(210, 105, 30, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 127, 80, 255)
        /// </summary>
        public static Color Coral => new Color(255, 127, 80, 255);

        /// <summary>
        /// (R, G, B, A) = (100, 149, 237, 255)
        /// </summary>
        public static Color CornflowerBlue => new Color(100, 149, 237, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 248, 220, 255)
        /// </summary>
        public static Color Cornsilk => new Color(255, 248, 220, 255);

        /// <summary>
        /// (R, G, B, A) = (220, 20, 60, 255)
        /// </summary>
        public static Color Crimson => new Color(220, 20, 60, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 255, 255, 255)
        /// </summary>
        public static Color Cyan => new Color(0, 255, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 0, 139, 255)
        /// </summary>
        public static Color DarkBlue => new Color(0, 0, 139, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 139, 139, 255)
        /// </summary>
        public static Color DarkCyan => new Color(0, 139, 139, 255);

        /// <summary>
        /// (R, G, B, A) = (184, 134, 11, 255)
        /// </summary>
        public static Color DarkGoldenrod => new Color(184, 134, 11, 255);

        /// <summary>
        /// (R, G, B, A) = (169, 169, 169, 255)
        /// </summary>
        public static Color DarkGray => new Color(169, 169, 169, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 100, 0, 255)
        /// </summary>
        public static Color DarkGreen => new Color(0, 100, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (189, 183, 107, 255)
        /// </summary>
        public static Color DarkKhaki => new Color(189, 183, 107, 255);

        /// <summary>
        /// (R, G, B, A) = (139, 0, 139, 255)
        /// </summary>
        public static Color DarkMagenta => new Color(139, 0, 139, 255);

        /// <summary>
        /// (R, G, B, A) = (85, 107, 47, 255)
        /// </summary>
        public static Color DarkOliveGreen => new Color(85, 107, 47, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 140, 0, 255)
        /// </summary>
        public static Color DarkOrange => new Color(255, 140, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (153, 50, 204, 255)
        /// </summary>
        public static Color DarkOrchid => new Color(153, 50, 204, 255);

        /// <summary>
        /// (R, G, B, A) = (139, 0, 0, 255)
        /// </summary>
        public static Color DarkRed => new Color(139, 0, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (233, 150, 122, 255)
        /// </summary>
        public static Color DarkSalmon => new Color(233, 150, 122, 255);

        /// <summary>
        /// (R, G, B, A) = (143, 188, 139, 255)
        /// </summary>
        public static Color DarkSeaGreen => new Color(143, 188, 139, 255);

        /// <summary>
        /// (R, G, B, A) = (72, 61, 139, 255)
        /// </summary>
        public static Color DarkSlateBlue => new Color(72, 61, 139, 255);

        /// <summary>
        /// (R, G, B, A) = (47, 79, 79, 255)
        /// </summary>
        public static Color DarkSlateGray => new Color(47, 79, 79, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 206, 209, 255)
        /// </summary>
        public static Color DarkTurquoise => new Color(0, 206, 209, 255);

        /// <summary>
        /// (R, G, B, A) = (148, 0, 211, 255)
        /// </summary>
        public static Color DarkViolet => new Color(148, 0, 211, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 20, 147, 255)
        /// </summary>
        public static Color DeepPink => new Color(255, 20, 147, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 191, 255, 255)
        /// </summary>
        public static Color DeepSkyBlue => new Color(0, 191, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (105, 105, 105, 255)
        /// </summary>
        public static Color DimGray => new Color(105, 105, 105, 255);

        /// <summary>
        /// (R, G, B, A) = (30, 144, 255, 255)
        /// </summary>
        public static Color DodgerBlue => new Color(30, 144, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (178, 34, 34, 255)
        /// </summary>
        public static Color Firebrick => new Color(178, 34, 34, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 250, 240, 255)
        /// </summary>
        public static Color FloralWhite => new Color(255, 250, 240, 255);

        /// <summary>
        /// (R, G, B, A) = (34, 139, 34, 255)
        /// </summary>
        public static Color ForestGreen => new Color(34, 139, 34, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 0, 255, 255)
        /// </summary>
        public static Color Fuchsia => new Color(255, 0, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (220, 220, 220, 255)
        /// </summary>
        public static Color Gainsboro => new Color(220, 220, 220, 255);

        /// <summary>
        /// (R, G, B, A) = (248, 248, 255, 255)
        /// </summary>
        public static Color GhostWhite => new Color(248, 248, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 215, 0, 255)
        /// </summary>
        public static Color Gold => new Color(255, 215, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (218, 165, 32, 255)
        /// </summary>
        public static Color Goldenrod => new Color(218, 165, 32, 255);

        /// <summary>
        /// (R, G, B, A) = (128, 128, 128, 255)
        /// </summary>
        public static Color Gray => new Color(128, 128, 128, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 128, 0, 255)
        /// </summary>
        public static Color Green => new Color(0, 128, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (173, 255, 47, 255)
        /// </summary>
        public static Color GreenYellow => new Color(173, 255, 47, 255);

        /// <summary>
        /// (R, G, B, A) = (240, 255, 240, 255)
        /// </summary>
        public static Color Honeydew => new Color(240, 255, 240, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 105, 180, 255)
        /// </summary>
        public static Color HotPink => new Color(255, 105, 180, 255);

        /// <summary>
        /// (R, G, B, A) = (205, 92, 92, 255)
        /// </summary>
        public static Color IndianRed => new Color(205, 92, 92, 255);

        /// <summary>
        /// (R, G, B, A) = (75, 0, 130, 255)
        /// </summary>
        public static Color Indigo => new Color(75, 0, 130, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 255, 240, 255)
        /// </summary>
        public static Color Ivory => new Color(255, 255, 240, 255);

        /// <summary>
        /// (R, G, B, A) = (240, 230, 140, 255)
        /// </summary>
        public static Color Khaki => new Color(240, 230, 140, 255);

        /// <summary>
        /// (R, G, B, A) = (230, 230, 250, 255)
        /// </summary>
        public static Color Lavender => new Color(230, 230, 250, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 240, 245, 255)
        /// </summary>
        public static Color LavenderBlush => new Color(255, 240, 245, 255);

        /// <summary>
        /// (R, G, B, A) = (124, 252, 0, 255)
        /// </summary>
        public static Color LawnGreen => new Color(124, 252, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 250, 205, 255)
        /// </summary>
        public static Color LemonChiffon => new Color(255, 250, 205, 255);

        /// <summary>
        /// (R, G, B, A) = (173, 216, 230, 255)
        /// </summary>
        public static Color LightBlue => new Color(173, 216, 230, 255);

        /// <summary>
        /// (R, G, B, A) = (240, 128, 128, 255)
        /// </summary>
        public static Color LightCoral => new Color(240, 128, 128, 255);

        /// <summary>
        /// (R, G, B, A) = (224, 255, 255, 255)
        /// </summary>
        public static Color LightCyan => new Color(224, 255, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (250, 250, 210, 255)
        /// </summary>
        public static Color LightGoldenrodYellow => new Color(250, 250, 210, 255);

        /// <summary>
        /// (R, G, B, A) = (144, 238, 144, 255)
        /// </summary>
        public static Color LightGreen => new Color(144, 238, 144, 255);

        /// <summary>
        /// (R, G, B, A) = (211, 211, 211, 255)
        /// </summary>
        public static Color LightGray => new Color(211, 211, 211, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 182, 193, 255)
        /// </summary>
        public static Color LightPink => new Color(255, 182, 193, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 160, 122, 255)
        /// </summary>
        public static Color LightSalmon => new Color(255, 160, 122, 255);

        /// <summary>
        /// (R, G, B, A) = (32, 178, 170, 255)
        /// </summary>
        public static Color LightSeaGreen => new Color(32, 178, 170, 255);

        /// <summary>
        /// (R, G, B, A) = (135, 206, 250, 255)
        /// </summary>
        public static Color LightSkyBlue => new Color(135, 206, 250, 255);

        /// <summary>
        /// (R, G, B, A) = (119, 136, 153, 255)
        /// </summary>
        public static Color LightSlateGray => new Color(119, 136, 153, 255);

        /// <summary>
        /// (R, G, B, A) = (176, 196, 222, 255)
        /// </summary>
        public static Color LightSteelBlue => new Color(176, 196, 222, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 255, 224, 255)
        /// </summary>
        public static Color LightYellow => new Color(255, 255, 224, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 255, 0, 255)
        /// </summary>
        public static Color Lime => new Color(0, 255, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (50, 205, 50, 255)
        /// </summary>
        public static Color LimeGreen => new Color(50, 205, 50, 255);

        /// <summary>
        /// (R, G, B, A) = (250, 240, 230, 255)
        /// </summary>
        public static Color Linen => new Color(250, 240, 230, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 0, 255, 255)
        /// </summary>
        public static Color Magenta => new Color(255, 0, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (128, 0, 0, 255)
        /// </summary>
        public static Color Maroon => new Color(128, 0, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (102, 205, 170, 255)
        /// </summary>
        public static Color MediumAquamarine => new Color(102, 205, 170, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 0, 205, 255)
        /// </summary>
        public static Color MediumBlue => new Color(0, 0, 205, 255);

        /// <summary>
        /// (R, G, B, A) = (186, 85, 211, 255)
        /// </summary>
        public static Color MediumOrchid => new Color(186, 85, 211, 255);

        /// <summary>
        /// (R, G, B, A) = (147, 112, 219, 255)
        /// </summary>
        public static Color MediumPurple => new Color(147, 112, 219, 255);

        /// <summary>
        /// (R, G, B, A) = (60, 179, 113, 255)
        /// </summary>
        public static Color MediumSeaGreen => new Color(60, 179, 113, 255);

        /// <summary>
        /// (R, G, B, A) = (123, 104, 238, 255)
        /// </summary>
        public static Color MediumSlateBlue => new Color(123, 104, 238, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 250, 154, 255)
        /// </summary>
        public static Color MediumSpringGreen => new Color(0, 250, 154, 255);

        /// <summary>
        /// (R, G, B, A) = (72, 209, 204, 255)
        /// </summary>
        public static Color MediumTurquoise => new Color(72, 209, 204, 255);

        /// <summary>
        /// (R, G, B, A) = (199, 21, 133, 255)
        /// </summary>
        public static Color MediumVioletRed => new Color(199, 21, 133, 255);

        /// <summary>
        /// (R, G, B, A) = (25, 25, 112, 255)
        /// </summary>
        public static Color MidnightBlue => new Color(25, 25, 112, 255);

        /// <summary>
        /// (R, G, B, A) = (245, 255, 250, 255)
        /// </summary>
        public static Color MintCream => new Color(245, 255, 250, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 228, 225, 255)
        /// </summary>
        public static Color MistyRose => new Color(255, 228, 225, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 228, 181, 255)
        /// </summary>
        public static Color Moccasin => new Color(255, 228, 181, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 222, 173, 255)
        /// </summary>
        public static Color NavajoWhite => new Color(255, 222, 173, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 0, 128, 255)
        /// </summary>
        public static Color Navy => new Color(0, 0, 128, 255);

        /// <summary>
        /// (R, G, B, A) = (253, 245, 230, 255)
        /// </summary>
        public static Color OldLace => new Color(253, 245, 230, 255);

        /// <summary>
        /// (R, G, B, A) = (128, 128, 0, 255)
        /// </summary>
        public static Color Olive => new Color(128, 128, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (107, 142, 35, 255)
        /// </summary>
        public static Color OliveDrab => new Color(107, 142, 35, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 165, 0, 255)
        /// </summary>
        public static Color Orange => new Color(255, 165, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 69, 0, 255)
        /// </summary>
        public static Color OrangeRed => new Color(255, 69, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (218, 112, 214, 255)
        /// </summary>
        public static Color Orchid => new Color(218, 112, 214, 255);

        /// <summary>
        /// (R, G, B, A) = (238, 232, 170, 255)
        /// </summary>
        public static Color PaleGoldenrod => new Color(238, 232, 170, 255);

        /// <summary>
        /// (R, G, B, A) = (152, 251, 152, 255)
        /// </summary>
        public static Color PaleGreen => new Color(152, 251, 152, 255);

        /// <summary>
        /// (R, G, B, A) = (175, 238, 238, 255)
        /// </summary>
        public static Color PaleTurquoise => new Color(175, 238, 238, 255);

        /// <summary>
        /// (R, G, B, A) = (219, 112, 147, 255)
        /// </summary>
        public static Color PaleVioletRed => new Color(219, 112, 147, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 239, 213, 255)
        /// </summary>
        public static Color PapayaWhip => new Color(255, 239, 213, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 218, 185, 255)
        /// </summary>
        public static Color PeachPuff => new Color(255, 218, 185, 255);

        /// <summary>
        /// (R, G, B, A) = (205, 133, 63, 255)
        /// </summary>
        public static Color Peru => new Color(205, 133, 63, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 192, 203, 255)
        /// </summary>
        public static Color Pink => new Color(255, 192, 203, 255);

        /// <summary>
        /// (R, G, B, A) = (221, 160, 221, 255)
        /// </summary>
        public static Color Plum => new Color(221, 160, 221, 255);

        /// <summary>
        /// (R, G, B, A) = (176, 224, 230, 255)
        /// </summary>
        public static Color PowderBlue => new Color(176, 224, 230, 255);

        /// <summary>
        /// (R, G, B, A) = (128, 0, 128, 255)
        /// </summary>
        public static Color Purple => new Color(128, 0, 128, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 0, 0, 255)
        /// </summary>
        public static Color Red => new Color(255, 0, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (188, 143, 143, 255)
        /// </summary>
        public static Color RosyBrown => new Color(188, 143, 143, 255);

        /// <summary>
        /// (R, G, B, A) = (65, 105, 225, 255)
        /// </summary>
        public static Color RoyalBlue => new Color(65, 105, 225, 255);

        /// <summary>
        /// (R, G, B, A) = (139, 69, 19, 255)
        /// </summary>
        public static Color SaddleBrown => new Color(139, 69, 19, 255);

        /// <summary>
        /// (R, G, B, A) = (250, 128, 114, 255)
        /// </summary>
        public static Color Salmon => new Color(250, 128, 114, 255);

        /// <summary>
        /// (R, G, B, A) = (244, 164, 96, 255)
        /// </summary>
        public static Color SandyBrown => new Color(244, 164, 96, 255);

        /// <summary>
        /// (R, G, B, A) = (46, 139, 87, 255)
        /// </summary>
        public static Color SeaGreen => new Color(46, 139, 87, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 245, 238, 255)
        /// </summary>
        public static Color SeaShell => new Color(255, 245, 238, 255);

        /// <summary>
        /// (R, G, B, A) = (160, 82, 45, 255)
        /// </summary>
        public static Color Sienna => new Color(160, 82, 45, 255);

        /// <summary>
        /// (R, G, B, A) = (192, 192, 192, 255)
        /// </summary>
        public static Color Silver => new Color(192, 192, 192, 255);

        /// <summary>
        /// (R, G, B, A) = (135, 206, 235, 255)
        /// </summary>
        public static Color SkyBlue => new Color(135, 206, 235, 255);

        /// <summary>
        /// (R, G, B, A) = (106, 90, 205, 255)
        /// </summary>
        public static Color SlateBlue => new Color(106, 90, 205, 255);

        /// <summary>
        /// (R, G, B, A) = (112, 128, 144, 255)
        /// </summary>
        public static Color SlateGray => new Color(112, 128, 144, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 250, 250, 255)
        /// </summary>
        public static Color Snow => new Color(255, 250, 250, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 255, 127, 255)
        /// </summary>
        public static Color SpringGreen => new Color(0, 255, 127, 255);

        /// <summary>
        /// (R, G, B, A) = (70, 130, 180, 255)
        /// </summary>
        public static Color SteelBlue => new Color(70, 130, 180, 255);

        /// <summary>
        /// (R, G, B, A) = (210, 180, 140, 255)
        /// </summary>
        public static Color Tan => new Color(210, 180, 140, 255);

        /// <summary>
        /// (R, G, B, A) = (0, 128, 128, 255)
        /// </summary>
        public static Color Teal => new Color(0, 128, 128, 255);

        /// <summary>
        /// (R, G, B, A) = (216, 191, 216, 255)
        /// </summary>
        public static Color Thistle => new Color(216, 191, 216, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 99, 71, 255)
        /// </summary>
        public static Color Tomato => new Color(255, 99, 71, 255);

        /// <summary>
        /// (R, G, B, A) = (64, 224, 208, 255)
        /// </summary>
        public static Color Turquoise => new Color(64, 224, 208, 255);

        /// <summary>
        /// (R, G, B, A) = (238, 130, 238, 255)
        /// </summary>
        public static Color Violet => new Color(238, 130, 238, 255);

        /// <summary>
        /// (R, G, B, A) = (245, 222, 179, 255)
        /// </summary>
        public static Color Wheat => new Color(245, 222, 179, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 255, 255, 255)
        /// </summary>
        public static Color White => new Color(255, 255, 255, 255);

        /// <summary>
        /// (R, G, B, A) = (245, 245, 245, 255)
        /// </summary>
        public static Color WhiteSmoke => new Color(245, 245, 245, 255);

        /// <summary>
        /// (R, G, B, A) = (255, 255, 0, 255)
        /// </summary>
        public static Color Yellow => new Color(255, 255, 0, 255);

        /// <summary>
        /// (R, G, B, A) = (154, 205, 50, 255)
        /// </summary>
        public static Color YellowGreen => new Color(154, 205, 50, 255);
    }
}
