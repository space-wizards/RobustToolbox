//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2008 the Open Toolkit library, except where noted.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

#if NETCOREAPP
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Represents a color with 4 floating-point components (R, G, B, A).
    /// </summary>
    [Serializable]
    public struct Color : IEquatable<Color>
    {
        /// <summary>
        ///     The red component of this Color4 structure.
        /// </summary>
        public float R;

        /// <summary>
        ///     The green component of this Color4 structure.
        /// </summary>
        public float G;

        /// <summary>
        ///     The blue component of this Color4 structure.
        /// </summary>
        public float B;

        /// <summary>
        ///     The alpha component of this Color4 structure.
        /// </summary>
        public float A;

        public readonly byte RByte => (byte) (R * byte.MaxValue);
        public readonly byte GByte => (byte) (G * byte.MaxValue);
        public readonly byte BByte => (byte) (B * byte.MaxValue);
        public readonly byte AByte => (byte) (A * byte.MaxValue);

        /// <summary>
        ///     Constructs a new Color4 structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Color4 structure.</param>
        /// <param name="g">The green component of the new Color4 structure.</param>
        /// <param name="b">The blue component of the new Color4 structure.</param>
        /// <param name="a">The alpha component of the new Color4 structure.</param>
        public Color(float r, float g, float b, float a = 1)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        ///     Constructs a new Color4 structure from the specified components.
        /// </summary>
        /// <param name="r">The red component of the new Color4 structure.</param>
        /// <param name="g">The green component of the new Color4 structure.</param>
        /// <param name="b">The blue component of the new Color4 structure.</param>
        /// <param name="a">The alpha component of the new Color4 structure.</param>
        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r / (float) byte.MaxValue;
            G = g / (float) byte.MaxValue;
            B = b / (float) byte.MaxValue;
            A = a / (float) byte.MaxValue;
        }

        /// <summary>
        ///     Converts this color to an integer representation with 8 bits per channel.
        /// </summary>
        /// <returns>A <see cref="System.Int32" /> that represents this instance.</returns>
        /// <remarks>
        ///     This method is intended only for compatibility with System.Drawing. It compresses the color into 8 bits per
        ///     channel, which means color information is lost.
        /// </remarks>
        public readonly int ToArgb()
        {
            var value =
                ((uint) (A * byte.MaxValue) << 24) |
                ((uint) (R * byte.MaxValue) << 16) |
                ((uint) (G * byte.MaxValue) << 8) |
                (uint) (B * byte.MaxValue);

            return unchecked((int) value);
        }

        /// <summary>
        ///     Compares the specified Color4 structures for equality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is equal to right; false otherwise.</returns>
        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///     Compares the specified Color4 structures for inequality.
        /// </summary>
        /// <param name="left">The left-hand side of the comparison.</param>
        /// <param name="right">The right-hand side of the comparison.</param>
        /// <returns>True if left is not equal to right; false otherwise.</returns>
        public static bool operator !=(Color left, Color right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///     Converts the specified System.Drawing.Color to a Color4 structure.
        /// </summary>
        /// <param name="color">The System.Drawing.Color to convert.</param>
        /// <returns>A new Color4 structure containing the converted components.</returns>
        public static implicit operator Color(System.Drawing.Color color)
        {
            return new(color.R, color.G, color.B, color.A);
        }

        public static implicit operator Color((float r, float g, float b, float a) tuple)
        {
            return new(tuple.r, tuple.g, tuple.b, tuple.a);
        }

        public static implicit operator Color((float r, float g, float b) tuple)
        {
            return new(tuple.r, tuple.g, tuple.b);
        }

        public readonly void Deconstruct(out float r, out float g, out float b, out float a)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        public readonly void Deconstruct(out float r, out float g, out float b)
        {
            r = R;
            g = G;
            b = B;
        }

        /// <summary>
        ///     Converts the specified Color4 to a System.Drawing.Color structure.
        /// </summary>
        /// <param name="color">The Color4 to convert.</param>
        /// <returns>A new System.Drawing.Color structure containing the converted components.</returns>
        public static explicit operator System.Drawing.Color(Color color)
        {
            return System.Drawing.Color.FromArgb(
                (int) (color.A * byte.MaxValue),
                (int) (color.R * byte.MaxValue),
                (int) (color.G * byte.MaxValue),
                (int) (color.B * byte.MaxValue));
        }

        public static Color FromName(string colorname)
        {
            return DefaultColors[colorname.ToLower()];
        }

        public static bool TryFromName(string colorName, out Color color)
        {
            return DefaultColors.TryGetValue(colorName.ToLower(), out color);
        }

        public static IEnumerable<KeyValuePair<string, Color>> GetAllDefaultColors()
        {
            return DefaultColors;
        }

        /// <summary>
        ///     Compares whether this Color4 structure is equal to the specified object.
        /// </summary>
        /// <param name="obj">An object to compare to.</param>
        /// <returns>True obj is a Color4 structure with the same components as this Color4; false otherwise.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (!(obj is Color))
                return false;

            return Equals((Color) obj);
        }

        /// <summary>
        ///     Calculates the hash code for this Color4 structure.
        /// </summary>
        /// <returns>A System.Int32 containing the hash code of this Color4 structure.</returns>
        public override readonly int GetHashCode()
        {
            return ToArgb();
        }

        /// <summary>
        ///     Creates a System.String that describes this Color4 structure.
        /// </summary>
        /// <returns>A System.String that describes this Color4 structure.</returns>
        public override readonly string ToString()
        {
            return $"{{(R, G, B, A) = ({R}, {G}, {B}, {A})}}";
        }

        public readonly Color WithRed(float newR)
        {
            return new(newR, G, B, A);
        }

        public readonly Color WithGreen(float newG)
        {
            return new(R, newG, B, A);
        }

        public readonly Color WithBlue(float newB)
        {
            return new(R, G, newB, A);
        }

        public readonly Color WithAlpha(float newA)
        {
            return new(R, G, B, newA);
        }

        public readonly Color WithRed(byte newR)
        {
            return new((float) newR / byte.MaxValue, G, B, A);
        }

        public readonly Color WithGreen(byte newG)
        {
            return new(R, (float) newG / byte.MaxValue, B, A);
        }

        public readonly Color WithBlue(byte newB)
        {
            return new(R, G, (float) newB / byte.MaxValue, A);
        }

        public readonly Color WithAlpha(byte newA)
        {
            return new(R, G, B, (float) newA / byte.MaxValue);
        }

        /// <summary>
        ///     Converts sRGB color values to linear RGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="srgb">
        ///     Color value to convert in sRGB.
        /// </param>
        public static Color FromSrgb(Color srgb)
        {
            float r, g, b;
#if NETCOREAPP
            if (srgb.R <= 0.04045f)
                r = srgb.R / 12.92f;
            else
                r = MathF.Pow((srgb.R + 0.055f) / (1.0f + 0.055f), 2.4f);

            if (srgb.G <= 0.04045f)
                g = srgb.G / 12.92f;
            else
                g = MathF.Pow((srgb.G + 0.055f) / (1.0f + 0.055f), 2.4f);

            if (srgb.B <= 0.04045f)
                b = srgb.B / 12.92f;
            else
                b = MathF.Pow((srgb.B + 0.055f) / (1.0f + 0.055f), 2.4f);
#else
            if (srgb.R <= 0.04045f)
                r = srgb.R / 12.92f;
            else
                r = (float) Math.Pow((srgb.R + 0.055f) / (1.0f + 0.055f), 2.4f);

            if (srgb.G <= 0.04045f)
                g = srgb.G / 12.92f;
            else
                g = (float) Math.Pow((srgb.G + 0.055f) / (1.0f + 0.055f), 2.4f);

            if (srgb.B <= 0.04045f)
                b = srgb.B / 12.92f;
            else
                b = (float) Math.Pow((srgb.B + 0.055f) / (1.0f + 0.055f), 2.4f);
#endif

            return new Color(r, g, b, srgb.A);
        }

        /// <summary>
        ///     Converts linear RGB color values to sRGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Color ToSrgb(Color rgb)
        {
            float r, g, b;

#if NETCOREAPP
            if (rgb.R <= 0.0031308)
                r = 12.92f * rgb.R;
            else
                r = (1.0f + 0.055f) * MathF.Pow(rgb.R, 1.0f / 2.4f) - 0.055f;

            if (rgb.G <= 0.0031308)
                g = 12.92f * rgb.G;
            else
                g = (1.0f + 0.055f) * MathF.Pow(rgb.G, 1.0f / 2.4f) - 0.055f;

            if (rgb.B <= 0.0031308)
                b = 12.92f * rgb.B;
            else
                b = (1.0f + 0.055f) * MathF.Pow(rgb.B, 1.0f / 2.4f) - 0.055f;
#else
            if (rgb.R <= 0.0031308)
                r = 12.92f * rgb.R;
            else
                r = (1.0f + 0.055f) * (float) Math.Pow(rgb.R, 1.0f / 2.4f) - 0.055f;

            if (rgb.G <= 0.0031308)
                g = 12.92f * rgb.G;
            else
                g = (1.0f + 0.055f) * (float) Math.Pow(rgb.G, 1.0f / 2.4f) - 0.055f;

            if (rgb.B <= 0.0031308)
                b = 12.92f * rgb.B;
            else
                b = (1.0f + 0.055f) * (float) Math.Pow(rgb.B, 1.0f / 2.4f) - 0.055f;
#endif

            return new Color(r, g, b, rgb.A);
        }

        /// <summary>
        ///     Converts HSL color values to RGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="hsl">
        ///     Color value to convert in hue, saturation, lightness (HSL).
        ///     The X element is Hue (H), the Y element is Saturation (S), the Z element is Lightness (L), and the W element is
        ///     Alpha (which is copied to the output's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color FromHsl(Vector4 hsl)
        {
            var hue = hsl.X * 360.0f;
            var saturation = hsl.Y;
            var lightness = hsl.Z;

            var c = (1.0f - MathF.Abs(2.0f * lightness - 1.0f)) * saturation;

            var h = hue / 60.0f;
            var X = c * (1.0f - MathF.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f)
            {
                r = c;
                g = X;
                b = 0.0f;
            }
            else if (1.0f <= h && h < 2.0f)
            {
                r = X;
                g = c;
                b = 0.0f;
            }
            else if (2.0f <= h && h < 3.0f)
            {
                r = 0.0f;
                g = c;
                b = X;
            }
            else if (3.0f <= h && h < 4.0f)
            {
                r = 0.0f;
                g = X;
                b = c;
            }
            else if (4.0f <= h && h < 5.0f)
            {
                r = X;
                g = 0.0f;
                b = c;
            }
            else if (5.0f <= h && h < 6.0f)
            {
                r = c;
                g = 0.0f;
                b = X;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = lightness - c / 2.0f;
            return new Color(r + m, g + m, b + m, hsl.W);
        }

        /// <summary>
        ///     Converts RGB color values to HSL color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        ///     The X element is Hue (H), the Y element is Saturation (S), the Z element is Lightness (L), and the W element is
        ///     Alpha (a copy of the input's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHsl(Color rgb)
        {
            var max = MathF.Max(rgb.R, MathF.Max(rgb.G, rgb.B));
            var min = MathF.Min(rgb.R, MathF.Min(rgb.G, rgb.B));
            var c = max - min;

            var h = 0.0f;
            if (max == rgb.R)
                h = (rgb.G - rgb.B) / c;
            else if (max == rgb.G)
                h = (rgb.B - rgb.R) / c + 2.0f;
            else if (max == rgb.B)
                h = (rgb.R - rgb.G) / c + 4.0f;

            var hue = h / 6.0f;
            if (hue < 0.0f)
                hue += 1.0f;

            var lightness = (max + min) / 2.0f;

            var saturation = 0.0f;
            if (0.0f != lightness && lightness != 1.0f)
                saturation = c / (1.0f - MathF.Abs(2.0f * lightness - 1.0f));

            return new Vector4(hue, saturation, lightness, rgb.A);
        }

        /// <summary>
        ///     Converts HSV color values to RGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="hsv">
        ///     Color value to convert in hue, saturation, value (HSV).
        ///     The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V), and the W element is Alpha
        ///     (which is copied to the output's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color FromHsv(Vector4 hsv)
        {
            var hue = hsv.X * 360.0f;
            var saturation = hsv.Y;
            var value = hsv.Z;

            var c = value * saturation;

            var h = hue / 60.0f;
            var x = c * (1.0f - MathF.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f)
            {
                r = c;
                g = x;
                b = 0.0f;
            }
            else if (1.0f <= h && h < 2.0f)
            {
                r = x;
                g = c;
                b = 0.0f;
            }
            else if (2.0f <= h && h < 3.0f)
            {
                r = 0.0f;
                g = c;
                b = x;
            }
            else if (3.0f <= h && h < 4.0f)
            {
                r = 0.0f;
                g = x;
                b = c;
            }
            else if (4.0f <= h && h < 5.0f)
            {
                r = x;
                g = 0.0f;
                b = c;
            }
            else if (5.0f <= h && h < 6.0f)
            {
                r = c;
                g = 0.0f;
                b = x;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = value - c;
            return new Color(r + m, g + m, b + m, hsv.W);
        }

        /// <summary>
        ///     Converts RGB color values to HSV color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        ///     The X element is Hue (H), the Y element is Saturation (S), the Z element is Value (V), and the W element is Alpha
        ///     (a copy of the input's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHsv(Color rgb)
        {
            var max = MathF.Max(rgb.R, MathF.Max(rgb.G, rgb.B));
            var min = MathF.Min(rgb.R, MathF.Min(rgb.G, rgb.B));
            var c = max - min;

            var h = 0.0f;
            if (max == rgb.R)
                h = (rgb.G - rgb.B) / c % 6.0f;
            else if (max == rgb.G)
                h = (rgb.B - rgb.R) / c + 2.0f;
            else if (max == rgb.B)
                h = (rgb.R - rgb.G) / c + 4.0f;

            var hue = h * 60.0f / 360.0f;

            var saturation = 0.0f;
            if (0.0f != max)
                saturation = c / max;

            return new Vector4(hue, saturation, max, rgb.A);
        }

        /// <summary>
        ///     Converts XYZ color values to RGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="xyz">
        ///     Color value to convert with the trisimulus values of X, Y, and Z in the corresponding element, and the W element
        ///     with Alpha (which is copied to the output's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </param>
        /// <remarks>Uses the CIE XYZ colorspace.</remarks>
        public static Color FromXyz(Vector4 xyz)
        {
            var r = 0.41847f * xyz.X + -0.15866f * xyz.Y + -0.082835f * xyz.Z;
            var g = -0.091169f * xyz.X + 0.25243f * xyz.Y + 0.015708f * xyz.Z;
            var b = 0.00092090f * xyz.X + -0.0025498f * xyz.Y + 0.17860f * xyz.Z;
            return new Color(r, g, b, xyz.W);
        }

        /// <summary>
        ///     Converts RGB color values to XYZ color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value with the trisimulus values of X, Y, and Z in the corresponding element, and the W
        ///     element with Alpha (a copy of the input's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        /// <remarks>Uses the CIE XYZ colorspace.</remarks>
        public static Vector4 ToXyz(Color rgb)
        {
            var x = (0.49f * rgb.R + 0.31f * rgb.G + 0.20f * rgb.B) / 0.17697f;
            var y = (0.17697f * rgb.R + 0.81240f * rgb.G + 0.01063f * rgb.B) / 0.17697f;
            var z = (0.00f * rgb.R + 0.01f * rgb.G + 0.99f * rgb.B) / 0.17697f;
            return new Vector4(x, y, z, rgb.A);
        }

        /// <summary>
        ///     Converts YCbCr color values to RGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="ycbcr">
        ///     Color value to convert in Luma-Chrominance (YCbCr) aka YUV.
        ///     The X element contains Luma (Y, 0.0 to 1.0), the Y element contains Blue-difference chroma (U, -0.5 to 0.5), the Z
        ///     element contains the Red-difference chroma (V, -0.5 to 0.5), and the W element contains the Alpha (which is copied
        ///     to the output's Alpha value).
        /// </param>
        /// <remarks>Converts using ITU-R BT.601/CCIR 601 W(r) = 0.299 W(b) = 0.114 U(max) = 0.436 V(max) = 0.615.</remarks>
        public static Color FromYcbcr(Vector4 ycbcr)
        {
            var r = 1.0f * ycbcr.X + 0.0f * ycbcr.Y + 1.402f * ycbcr.Z;
            var g = 1.0f * ycbcr.X + -0.344136f * ycbcr.Y + -0.714136f * ycbcr.Z;
            var b = 1.0f * ycbcr.X + 1.772f * ycbcr.Y + 0.0f * ycbcr.Z;
            return new Color(r, g, b, ycbcr.W);
        }

        /// <summary>
        ///     Converts RGB color values to YUV color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value in Luma-Chrominance (YCbCr) aka YUV.
        ///     The X element contains Luma (Y, 0.0 to 1.0), the Y element contains Blue-difference chroma (U, -0.5 to 0.5), the Z
        ///     element contains the Red-difference chroma (V, -0.5 to 0.5), and the W element contains the Alpha (a copy of the
        ///     input's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        /// <remarks>Converts using ITU-R BT.601/CCIR 601 W(r) = 0.299 W(b) = 0.114 U(max) = 0.436 V(max) = 0.615.</remarks>
        public static Vector4 ToYcbcr(Color rgb)
        {
            var y = 0.299f * rgb.R + 0.587f * rgb.G + 0.114f * rgb.B;
            var u = -0.168736f * rgb.R + -0.331264f * rgb.G + 0.5f * rgb.B;
            var v = 0.5f * rgb.R + -0.418688f * rgb.G + -0.081312f * rgb.B;
            return new Vector4(y, u, v, rgb.A);
        }

        /// <summary>
        ///     Converts HCY color values to RGB color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        /// </returns>
        /// <param name="hcy">
        ///     Color value to convert in hue, chroma, luminance (HCY).
        ///     The X element is Hue (H), the Y element is Chroma (C), the Z element is luminance (Y), and the W element is Alpha
        ///     (which is copied to the output's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </param>
        public static Color FromHcy(Vector4 hcy)
        {
            var hue = hcy.X * 360.0f;
            var c = hcy.Y;
            var luminance = hcy.Z;

            var h = hue / 60.0f;
            var x = c * (1.0f - MathF.Abs(h % 2.0f - 1.0f));

            float r, g, b;
            if (0.0f <= h && h < 1.0f)
            {
                r = c;
                g = x;
                b = 0.0f;
            }
            else if (1.0f <= h && h < 2.0f)
            {
                r = x;
                g = c;
                b = 0.0f;
            }
            else if (2.0f <= h && h < 3.0f)
            {
                r = 0.0f;
                g = c;
                b = x;
            }
            else if (3.0f <= h && h < 4.0f)
            {
                r = 0.0f;
                g = x;
                b = c;
            }
            else if (4.0f <= h && h < 5.0f)
            {
                r = x;
                g = 0.0f;
                b = c;
            }
            else if (5.0f <= h && h < 6.0f)
            {
                r = c;
                g = 0.0f;
                b = x;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = luminance - (0.30f * r + 0.59f * g + 0.11f * b);
            return new Color(r + m, g + m, b + m, hcy.W);
        }

        /// <summary>
        ///     Converts RGB color values to HCY color values.
        /// </summary>
        /// <returns>
        ///     Returns the converted color value.
        ///     The X element is Hue (H), the Y element is Chroma (C), the Z element is luminance (Y), and the W element is Alpha
        ///     (a copy of the input's Alpha value).
        ///     Each has a range of 0.0 to 1.0.
        /// </returns>
        /// <param name="rgb">Color value to convert.</param>
        public static Vector4 ToHcy(Color rgb)
        {
            var max = MathF.Max(rgb.R, MathF.Max(rgb.G, rgb.B));
            var min = MathF.Min(rgb.R, MathF.Min(rgb.G, rgb.B));
            var c = max - min;

            var h = 0.0f;
            if (max == rgb.R)
                h = (rgb.G - rgb.B) / c % 6.0f;
            else if (max == rgb.G)
                h = (rgb.B - rgb.R) / c + 2.0f;
            else if (max == rgb.B)
                h = (rgb.R - rgb.G) / c + 4.0f;

            var hue = h * 60.0f / 360.0f;

            var luminance = 0.30f * rgb.R + 0.59f * rgb.G + 0.11f * rgb.B;

            return new Vector4(hue, c, luminance, rgb.A);
        }


        public static Vector4 ToCmyk(Color rgb)
        {
            var (r, g, b) = rgb;
            var k = 1 - MathF.Max(r, MathF.Max(g, b));
            var c = (1 - r - k) / (1 - k);
            var m = (1 - g - k) / (1 - k);
            var y = (1 - b - k) / (1 - k);

            return (c, m, y, k);
        }

        public static Color FromCmyk(Vector4 cmyk)
        {
            var (c, m, y, k) = cmyk;
            var r = (1 - c) * (1 - k);
            var g = (1 - m) * (1 - k);
            var b = (1 - y) * (1 - k);

            return (r, g, b);
        }

        /// <summary>
        ///     Interpolate two colors with a lambda, AKA returning the two colors combined with a ratio of
        ///     <paramref name="λ" />.
        /// </summary>
        /// <param name="α"></param>
        /// <param name="β"></param>
        /// <param name="λ">
        ///     A value ranging from 0-1. The higher the value the more is taken from <paramref name="β" />,
        ///     with 0.5 being 50% of both colors, 0.25 being 25% of <paramref name="β" /> and 75%
        ///     <paramref name="α" />.
        /// </param>
#if NETCOREAPP
        public static Color InterpolateBetween(Color α, Color β, float λ)
        {
            if (Sse.IsSupported && Fma.IsSupported)
            {
                var vecA = Unsafe.As<Color, Vector128<float>>(ref α);
                var vecB = Unsafe.As<Color, Vector128<float>>(ref β);

                vecB = Fma.MultiplyAdd(Sse.Subtract(vecB, vecA), Vector128.Create(λ), vecA);

                return Unsafe.As<Vector128<float>, Color>(ref vecB);
            }
            ref var svA = ref Unsafe.As<Color, SysVector4>(ref α);
            ref var svB = ref Unsafe.As<Color, SysVector4>(ref β);

            var res = SysVector4.Lerp(svA, svB, λ);

            return Unsafe.As<SysVector4, Color>(ref res);
        }
#else
        public static Color InterpolateBetween(in Color α, in Color β, float λ)
        {
            return new Color(
                (β.R - α.R) * λ + α.R,
                (β.G - α.G) * λ + α.G,
                (β.B - α.B) * λ + α.B,
                (β.A - α.A) * λ + α.A
            );
        }
#endif

        public static Color? TryFromHex(ReadOnlySpan<char> hexColor)
        {
            if (hexColor.Length <= 0 || hexColor[0] != '#') return null;
            if (hexColor.Length == 9)
            {
                if (!byte.TryParse(hexColor[1..3], NumberStyles.HexNumber, null, out var r)) return null;
                if (!byte.TryParse(hexColor[3..5], NumberStyles.HexNumber, null, out var g)) return null;
                if (!byte.TryParse(hexColor[5..7], NumberStyles.HexNumber, null, out var b)) return null;
                if (!byte.TryParse(hexColor[7..9], NumberStyles.HexNumber, null, out var a)) return null;
                return new Color(r, g, b, a);
            }
            if (hexColor.Length == 7)
            {
                if (!byte.TryParse(hexColor[1..3], NumberStyles.HexNumber, null, out var r)) return null;
                if (!byte.TryParse(hexColor[3..5], NumberStyles.HexNumber, null, out var g)) return null;
                if (!byte.TryParse(hexColor[5..7], NumberStyles.HexNumber, null, out var b)) return null;
                return new Color(r, g, b);
            }

            static bool ParseDup(char chr, out byte value)
            {
                Span<char> buf = stackalloc char[2];
                buf[0] = chr;
                buf[1] = chr;

                return byte.TryParse(buf, NumberStyles.HexNumber, null, out value);
            }

            if (hexColor.Length == 5)
            {
                if (!ParseDup(hexColor[1], out var rByte)) return null;
                if (!ParseDup(hexColor[2], out var gByte)) return null;
                if (!ParseDup(hexColor[3], out var bByte)) return null;
                if (!ParseDup(hexColor[4], out var aByte)) return null;

                return new Color(rByte, gByte, bByte, aByte);
            }
            if (hexColor.Length == 4)
            {
                if (!ParseDup(hexColor[1], out var rByte)) return null;
                if (!ParseDup(hexColor[2], out var gByte)) return null;
                if (!ParseDup(hexColor[3], out var bByte)) return null;

                return new Color(rByte, gByte, bByte);
            }
            return null;
        }

        public static Color FromHex(ReadOnlySpan<char> hexColor, Color? fallback = null)
        {
            var color = TryFromHex(hexColor);
            if (color.HasValue)
                return color.Value;
            if (fallback.HasValue)
                return fallback.Value;
            throw new ArgumentException("Invalid color code and no fallback provided.", nameof(hexColor));
        }

        public static Color Blend(Color dstColor, Color srcColor, BlendFactor dstFactor, BlendFactor srcFactor)
        {
            var dst = new SysVector3(dstColor.R, dstColor.G, dstColor.B);
            var src = new SysVector3(srcColor.R, srcColor.G, srcColor.B);

            var ret = new SysVector3();

            switch (dstFactor)
            {
                case BlendFactor.Zero:
                    break;
                case BlendFactor.One:
                    ret = dst;
                    break;
                case BlendFactor.SrcColor:
                    ret = dst * src;
                    break;
                case BlendFactor.OneMinusSrcColor:
                    ret = dst * (SysVector3.One - src);
                    break;
                case BlendFactor.DstColor:
                    ret = dst * dst;
                    break;
                case BlendFactor.OneMinusDstColor:
                    ret = dst * (SysVector3.One - dst);
                    break;
                case BlendFactor.SrcAlpha:
                    ret = dst * srcColor.A;
                    break;
                case BlendFactor.OneMinusSrcAlpha:
                    ret = dst * (1 - srcColor.A);
                    break;
                case BlendFactor.DstAlpha:
                    ret = dst * dstColor.A;
                    break;
                case BlendFactor.OneMinusDstAlpha:
                    ret = dst * (1 - dstColor.A);
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (srcFactor)
            {
                case BlendFactor.Zero:
                    break;
                case BlendFactor.One:
                    ret += src;
                    break;
                case BlendFactor.SrcColor:
                    ret += src * src;
                    break;
                case BlendFactor.OneMinusSrcColor:
                    ret += src * (SysVector3.One - src);
                    break;
                case BlendFactor.DstColor:
                    ret += src * dst;
                    break;
                case BlendFactor.OneMinusDstColor:
                    ret += src * (SysVector3.One - dst);
                    break;
                case BlendFactor.SrcAlpha:
                    ret += src * srcColor.A;
                    break;
                case BlendFactor.OneMinusSrcAlpha:
                    ret += src * (1 - srcColor.A);
                    break;
                case BlendFactor.DstAlpha:
                    ret += src * dstColor.A;
                    break;
                case BlendFactor.OneMinusDstAlpha:
                    ret += src * (1 - dstColor.A);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new Color(ret.X, ret.Y, ret.Z, MathF.Min(1, dstColor.A + dstColor.A * srcColor.A));
        }

        /// <summary>
        ///     Component wise multiplication of two colors.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Color operator *(Color a, Color b)
        {
            return new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
        }

        public readonly string ToHex()
        {
            var hexColor = 0;
            hexColor += RByte << 24;
            hexColor += GByte << 16;
            hexColor += BByte << 8;
            hexColor += AByte;

            return $"#{hexColor:X8}";
        }

        public readonly string ToHexNoAlpha()
        {
            var hexColor = 0;
            hexColor += RByte << 16;
            hexColor += GByte << 8;
            hexColor += BByte;

            return $"#{hexColor:X6}";
        }

        /// <summary>
        ///     Compares whether this Color4 structure is equal to the specified Color4.
        /// </summary>
        /// <param name="other">The Color4 structure to compare to.</param>
        /// <returns>True if both Color4 structures contain the same components; false otherwise.</returns>
        public readonly bool Equals(Color other)
        {
            return
                MathHelper.CloseTo(R, other.R) &&
                MathHelper.CloseTo(G, other.G) &&
                MathHelper.CloseTo(B, other.B) &&
                MathHelper.CloseTo(A, other.A);
        }

        [PublicAPI]
        public enum BlendFactor : byte
        {
            Zero,
            One,
            SrcColor,
            OneMinusSrcColor,
            DstColor,
            OneMinusDstColor,
            SrcAlpha,
            OneMinusSrcAlpha,
            DstAlpha,
            OneMinusDstAlpha,
        }

        #region Static Colors

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 255, 255, 0).
        /// </summary>
        public static Color Transparent => new(255, 255, 255, 0);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (240, 248, 255, 255).
        /// </summary>
        public static Color AliceBlue => new(240, 248, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (250, 235, 215, 255).
        /// </summary>
        public static Color AntiqueWhite => new(250, 235, 215, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Color Aqua => new(0, 255, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (127, 255, 212, 255).
        /// </summary>
        public static Color Aquamarine => new(127, 255, 212, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (240, 255, 255, 255).
        /// </summary>
        public static Color Azure => new(240, 255, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (245, 245, 220, 255).
        /// </summary>
        public static Color Beige => new(245, 245, 220, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 228, 196, 255).
        /// </summary>
        public static Color Bisque => new(255, 228, 196, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 0, 0, 255).
        /// </summary>
        public static Color Black => new(0, 0, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 235, 205, 255).
        /// </summary>
        public static Color BlanchedAlmond => new(255, 235, 205, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 0, 255, 255).
        /// </summary>
        public static Color Blue => new(0, 0, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (138, 43, 226, 255).
        /// </summary>
        public static Color BlueViolet => new(138, 43, 226, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (165, 42, 42, 255).
        /// </summary>
        public static Color Brown => new(165, 42, 42, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (222, 184, 135, 255).
        /// </summary>
        public static Color BurlyWood => new(222, 184, 135, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (95, 158, 160, 255).
        /// </summary>
        public static Color CadetBlue => new(95, 158, 160, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (127, 255, 0, 255).
        /// </summary>
        public static Color Chartreuse => new(127, 255, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (210, 105, 30, 255).
        /// </summary>
        public static Color Chocolate => new(210, 105, 30, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 127, 80, 255).
        /// </summary>
        public static Color Coral => new(255, 127, 80, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (100, 149, 237, 255).
        /// </summary>
        public static Color CornflowerBlue => new(100, 149, 237, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 248, 220, 255).
        /// </summary>
        public static Color Cornsilk => new(255, 248, 220, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (220, 20, 60, 255).
        /// </summary>
        public static Color Crimson => new(220, 20, 60, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 255, 255, 255).
        /// </summary>
        public static Color Cyan => new(0, 255, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 0, 139, 255).
        /// </summary>
        public static Color DarkBlue => new(0, 0, 139, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 139, 139, 255).
        /// </summary>
        public static Color DarkCyan => new(0, 139, 139, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (184, 134, 11, 255).
        /// </summary>
        public static Color DarkGoldenrod => new(184, 134, 11, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (169, 169, 169, 255).
        /// </summary>
        public static Color DarkGray => new(169, 169, 169, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 100, 0, 255).
        /// </summary>
        public static Color DarkGreen => new(0, 100, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (189, 183, 107, 255).
        /// </summary>
        public static Color DarkKhaki => new(189, 183, 107, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (139, 0, 139, 255).
        /// </summary>
        public static Color DarkMagenta => new(139, 0, 139, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (85, 107, 47, 255).
        /// </summary>
        public static Color DarkOliveGreen => new(85, 107, 47, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 140, 0, 255).
        /// </summary>
        public static Color DarkOrange => new(255, 140, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (153, 50, 204, 255).
        /// </summary>
        public static Color DarkOrchid => new(153, 50, 204, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (139, 0, 0, 255).
        /// </summary>
        public static Color DarkRed => new(139, 0, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (233, 150, 122, 255).
        /// </summary>
        public static Color DarkSalmon => new(233, 150, 122, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (143, 188, 143, 255).
        ///     Previously (R, G, B, A) = (143, 188, 139, 255) before .NET 5.
        /// </summary>
        public static Color DarkSeaGreen => new(143, 188, 143, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (72, 61, 139, 255).
        /// </summary>
        public static Color DarkSlateBlue => new(72, 61, 139, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (47, 79, 79, 255).
        /// </summary>
        public static Color DarkSlateGray => new(47, 79, 79, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 206, 209, 255).
        /// </summary>
        public static Color DarkTurquoise => new(0, 206, 209, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (148, 0, 211, 255).
        /// </summary>
        public static Color DarkViolet => new(148, 0, 211, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 20, 147, 255).
        /// </summary>
        public static Color DeepPink => new(255, 20, 147, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 191, 255, 255).
        /// </summary>
        public static Color DeepSkyBlue => new(0, 191, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (105, 105, 105, 255).
        /// </summary>
        public static Color DimGray => new(105, 105, 105, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (30, 144, 255, 255).
        /// </summary>
        public static Color DodgerBlue => new(30, 144, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (178, 34, 34, 255).
        /// </summary>
        public static Color Firebrick => new(178, 34, 34, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 250, 240, 255).
        /// </summary>
        public static Color FloralWhite => new(255, 250, 240, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (34, 139, 34, 255).
        /// </summary>
        public static Color ForestGreen => new(34, 139, 34, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Color Fuchsia => new(255, 0, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (220, 220, 220, 255).
        /// </summary>
        public static Color Gainsboro => new(220, 220, 220, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (248, 248, 255, 255).
        /// </summary>
        public static Color GhostWhite => new(248, 248, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 215, 0, 255).
        /// </summary>
        public static Color Gold => new(255, 215, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (218, 165, 32, 255).
        /// </summary>
        public static Color Goldenrod => new(218, 165, 32, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (128, 128, 128, 255).
        /// </summary>
        public static Color Gray => new(128, 128, 128, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 128, 0, 255).
        /// </summary>
        public static Color Green => new(0, 128, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (173, 255, 47, 255).
        /// </summary>
        public static Color GreenYellow => new(173, 255, 47, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (240, 255, 240, 255).
        /// </summary>
        public static Color Honeydew => new(240, 255, 240, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 105, 180, 255).
        /// </summary>
        public static Color HotPink => new(255, 105, 180, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (205, 92, 92, 255).
        /// </summary>
        public static Color IndianRed => new(205, 92, 92, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (75, 0, 130, 255).
        /// </summary>
        public static Color Indigo => new(75, 0, 130, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 255, 240, 255).
        /// </summary>
        public static Color Ivory => new(255, 255, 240, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (240, 230, 140, 255).
        /// </summary>
        public static Color Khaki => new(240, 230, 140, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (230, 230, 250, 255).
        /// </summary>
        public static Color Lavender => new(230, 230, 250, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 240, 245, 255).
        /// </summary>
        public static Color LavenderBlush => new(255, 240, 245, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (124, 252, 0, 255).
        /// </summary>
        public static Color LawnGreen => new(124, 252, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 250, 205, 255).
        /// </summary>
        public static Color LemonChiffon => new(255, 250, 205, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (173, 216, 230, 255).
        /// </summary>
        public static Color LightBlue => new(173, 216, 230, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (240, 128, 128, 255).
        /// </summary>
        public static Color LightCoral => new(240, 128, 128, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (224, 255, 255, 255).
        /// </summary>
        public static Color LightCyan => new(224, 255, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (250, 250, 210, 255).
        /// </summary>
        public static Color LightGoldenrodYellow => new(250, 250, 210, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (144, 238, 144, 255).
        /// </summary>
        public static Color LightGreen => new(144, 238, 144, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (211, 211, 211, 255).
        /// </summary>
        public static Color LightGray => new(211, 211, 211, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 182, 193, 255).
        /// </summary>
        public static Color LightPink => new(255, 182, 193, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 160, 122, 255).
        /// </summary>
        public static Color LightSalmon => new(255, 160, 122, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (32, 178, 170, 255).
        /// </summary>
        public static Color LightSeaGreen => new(32, 178, 170, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (135, 206, 250, 255).
        /// </summary>
        public static Color LightSkyBlue => new(135, 206, 250, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (119, 136, 153, 255).
        /// </summary>
        public static Color LightSlateGray => new(119, 136, 153, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (176, 196, 222, 255).
        /// </summary>
        public static Color LightSteelBlue => new(176, 196, 222, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 255, 224, 255).
        /// </summary>
        public static Color LightYellow => new(255, 255, 224, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 255, 0, 255).
        /// </summary>
        public static Color Lime => new(0, 255, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (50, 205, 50, 255).
        /// </summary>
        public static Color LimeGreen => new(50, 205, 50, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (250, 240, 230, 255).
        /// </summary>
        public static Color Linen => new(250, 240, 230, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 0, 255, 255).
        /// </summary>
        public static Color Magenta => new(255, 0, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (128, 0, 0, 255).
        /// </summary>
        public static Color Maroon => new(128, 0, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (102, 205, 170, 255).
        /// </summary>
        public static Color MediumAquamarine => new(102, 205, 170, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 0, 205, 255).
        /// </summary>
        public static Color MediumBlue => new(0, 0, 205, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (186, 85, 211, 255).
        /// </summary>
        public static Color MediumOrchid => new(186, 85, 211, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (147, 112, 219, 255).
        /// </summary>
        public static Color MediumPurple => new(147, 112, 219, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (60, 179, 113, 255).
        /// </summary>
        public static Color MediumSeaGreen => new(60, 179, 113, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (123, 104, 238, 255).
        /// </summary>
        public static Color MediumSlateBlue => new(123, 104, 238, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 250, 154, 255).
        /// </summary>
        public static Color MediumSpringGreen => new(0, 250, 154, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (72, 209, 204, 255).
        /// </summary>
        public static Color MediumTurquoise => new(72, 209, 204, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (199, 21, 133, 255).
        /// </summary>
        public static Color MediumVioletRed => new(199, 21, 133, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (25, 25, 112, 255).
        /// </summary>
        public static Color MidnightBlue => new(25, 25, 112, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (245, 255, 250, 255).
        /// </summary>
        public static Color MintCream => new(245, 255, 250, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 228, 225, 255).
        /// </summary>
        public static Color MistyRose => new(255, 228, 225, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 228, 181, 255).
        /// </summary>
        public static Color Moccasin => new(255, 228, 181, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 222, 173, 255).
        /// </summary>
        public static Color NavajoWhite => new(255, 222, 173, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 0, 128, 255).
        /// </summary>
        public static Color Navy => new(0, 0, 128, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (253, 245, 230, 255).
        /// </summary>
        public static Color OldLace => new(253, 245, 230, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (128, 128, 0, 255).
        /// </summary>
        public static Color Olive => new(128, 128, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (107, 142, 35, 255).
        /// </summary>
        public static Color OliveDrab => new(107, 142, 35, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 165, 0, 255).
        /// </summary>
        public static Color Orange => new(255, 165, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 69, 0, 255).
        /// </summary>
        public static Color OrangeRed => new(255, 69, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (218, 112, 214, 255).
        /// </summary>
        public static Color Orchid => new(218, 112, 214, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (238, 232, 170, 255).
        /// </summary>
        public static Color PaleGoldenrod => new(238, 232, 170, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (152, 251, 152, 255).
        /// </summary>
        public static Color PaleGreen => new(152, 251, 152, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (175, 238, 238, 255).
        /// </summary>
        public static Color PaleTurquoise => new(175, 238, 238, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (219, 112, 147, 255).
        /// </summary>
        public static Color PaleVioletRed => new(219, 112, 147, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 239, 213, 255).
        /// </summary>
        public static Color PapayaWhip => new(255, 239, 213, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 218, 185, 255).
        /// </summary>
        public static Color PeachPuff => new(255, 218, 185, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (205, 133, 63, 255).
        /// </summary>
        public static Color Peru => new(205, 133, 63, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 192, 203, 255).
        /// </summary>
        public static Color Pink => new(255, 192, 203, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (221, 160, 221, 255).
        /// </summary>
        public static Color Plum => new(221, 160, 221, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (176, 224, 230, 255).
        /// </summary>
        public static Color PowderBlue => new(176, 224, 230, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (128, 0, 128, 255).
        /// </summary>
        public static Color Purple => new(128, 0, 128, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 0, 0, 255).
        /// </summary>
        public static Color Red => new(255, 0, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (188, 143, 143, 255).
        /// </summary>
        public static Color RosyBrown => new(188, 143, 143, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (65, 105, 225, 255).
        /// </summary>
        public static Color RoyalBlue => new(65, 105, 225, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (139, 69, 19, 255).
        /// </summary>
        public static Color SaddleBrown => new(139, 69, 19, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (250, 128, 114, 255).
        /// </summary>
        public static Color Salmon => new(250, 128, 114, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (244, 164, 96, 255).
        /// </summary>
        public static Color SandyBrown => new(244, 164, 96, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (46, 139, 87, 255).
        /// </summary>
        public static Color SeaGreen => new(46, 139, 87, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 245, 238, 255).
        /// </summary>
        public static Color SeaShell => new(255, 245, 238, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (160, 82, 45, 255).
        /// </summary>
        public static Color Sienna => new(160, 82, 45, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (192, 192, 192, 255).
        /// </summary>
        public static Color Silver => new(192, 192, 192, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (135, 206, 235, 255).
        /// </summary>
        public static Color SkyBlue => new(135, 206, 235, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (106, 90, 205, 255).
        /// </summary>
        public static Color SlateBlue => new(106, 90, 205, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (112, 128, 144, 255).
        /// </summary>
        public static Color SlateGray => new(112, 128, 144, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 250, 250, 255).
        /// </summary>
        public static Color Snow => new(255, 250, 250, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 255, 127, 255).
        /// </summary>
        public static Color SpringGreen => new(0, 255, 127, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (70, 130, 180, 255).
        /// </summary>
        public static Color SteelBlue => new(70, 130, 180, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (210, 180, 140, 255).
        /// </summary>
        public static Color Tan => new(210, 180, 140, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (0, 128, 128, 255).
        /// </summary>
        public static Color Teal => new(0, 128, 128, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (216, 191, 216, 255).
        /// </summary>
        public static Color Thistle => new(216, 191, 216, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 99, 71, 255).
        /// </summary>
        public static Color Tomato => new(255, 99, 71, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (64, 224, 208, 255).
        /// </summary>
        public static Color Turquoise => new(64, 224, 208, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (238, 130, 238, 255).
        /// </summary>
        public static Color Violet => new(238, 130, 238, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (245, 222, 179, 255).
        /// </summary>
        public static Color Wheat => new(245, 222, 179, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 255, 255, 255).
        /// </summary>
        public static Color White => new(255, 255, 255, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (245, 245, 245, 255).
        /// </summary>
        public static Color WhiteSmoke => new(245, 245, 245, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (255, 255, 0, 255).
        /// </summary>
        public static Color Yellow => new(255, 255, 0, 255);

        /// <summary>
        ///     Gets the system color with (R, G, B, A) = (154, 205, 50, 255).
        /// </summary>
        public static Color YellowGreen => new(154, 205, 50, 255);

        private static readonly Dictionary<string, Color> DefaultColors = new()
        {
            ["transparent"] = Transparent,
            ["aliceblue"] = AliceBlue,
            ["antiquewhite"] = AntiqueWhite,
            ["aqua"] = Aqua,
            ["aquamarine"] = Aquamarine,
            ["azure"] = Azure,
            ["beige"] = Beige,
            ["bisque"] = Bisque,
            ["black"] = Black,
            ["blanchedalmond"] = BlanchedAlmond,
            ["blue"] = Blue,
            ["blueviolet"] = BlueViolet,
            ["brown"] = Brown,
            ["burlywood"] = BurlyWood,
            ["cadetblue"] = CadetBlue,
            ["chartreuse"] = Chartreuse,
            ["chocolate"] = Chocolate,
            ["coral"] = Coral,
            ["cornflowerblue"] = CornflowerBlue,
            ["cornsilk"] = Cornsilk,
            ["crimson"] = Crimson,
            ["cyan"] = Cyan,
            ["darkblue"] = DarkBlue,
            ["darkcyan"] = DarkCyan,
            ["darkgoldenrod"] = DarkGoldenrod,
            ["darkgray"] = DarkGray,
            ["darkgreen"] = DarkGreen,
            ["darkkhaki"] = DarkKhaki,
            ["darkmagenta"] = DarkMagenta,
            ["darkolivegreen"] = DarkOliveGreen,
            ["darkorange"] = DarkOrange,
            ["darkorchid"] = DarkOrchid,
            ["darkred"] = DarkRed,
            ["darksalmon"] = DarkSalmon,
            ["darkseagreen"] = DarkSeaGreen,
            ["darkslateblue"] = DarkSlateBlue,
            ["darkslategray"] = DarkSlateGray,
            ["darkturquoise"] = DarkTurquoise,
            ["darkviolet"] = DarkViolet,
            ["deeppink"] = DeepPink,
            ["deepskyblue"] = DeepSkyBlue,
            ["dimgray"] = DimGray,
            ["dodgerblue"] = DodgerBlue,
            ["firebrick"] = Firebrick,
            ["floralwhite"] = FloralWhite,
            ["forestgreen"] = ForestGreen,
            ["fuchsia"] = Fuchsia,
            ["gainsboro"] = Gainsboro,
            ["ghostwhite"] = GhostWhite,
            ["gold"] = Gold,
            ["goldenrod"] = Goldenrod,
            ["gray"] = Gray,
            ["green"] = Green,
            ["greenyellow"] = GreenYellow,
            ["honeydew"] = Honeydew,
            ["hotpink"] = HotPink,
            ["indianred"] = IndianRed,
            ["indigo"] = Indigo,
            ["ivory"] = Ivory,
            ["khaki"] = Khaki,
            ["lavender"] = Lavender,
            ["lavenderblush"] = LavenderBlush,
            ["lawngreen"] = LawnGreen,
            ["lemonchiffon"] = LemonChiffon,
            ["lightblue"] = LightBlue,
            ["lightcoral"] = LightCoral,
            ["lightcyan"] = LightCyan,
            ["lightgoldenrodyellow"] = LightGoldenrodYellow,
            ["lightgreen"] = LightGreen,
            ["lightgray"] = LightGray,
            ["lightpink"] = LightPink,
            ["lightsalmon"] = LightSalmon,
            ["lightseagreen"] = LightSeaGreen,
            ["lightskyblue"] = LightSkyBlue,
            ["lightslategray"] = LightSlateGray,
            ["lightsteelblue"] = LightSteelBlue,
            ["lightyellow"] = LightYellow,
            ["lime"] = Lime,
            ["limegreen"] = LimeGreen,
            ["linen"] = Linen,
            ["magenta"] = Magenta,
            ["maroon"] = Maroon,
            ["mediumaquamarine"] = MediumAquamarine,
            ["mediumblue"] = MediumBlue,
            ["mediumorchid"] = MediumOrchid,
            ["mediumpurple"] = MediumPurple,
            ["mediumseagreen"] = MediumSeaGreen,
            ["mediumslateblue"] = MediumSlateBlue,
            ["mediumspringgreen"] = MediumSpringGreen,
            ["mediumturquoise"] = MediumTurquoise,
            ["mediumvioletred"] = MediumVioletRed,
            ["midnightblue"] = MidnightBlue,
            ["mintcream"] = MintCream,
            ["mistyrose"] = MistyRose,
            ["moccasin"] = Moccasin,
            ["navajowhite"] = NavajoWhite,
            ["navy"] = Navy,
            ["oldlace"] = OldLace,
            ["olive"] = Olive,
            ["olivedrab"] = OliveDrab,
            ["orange"] = Orange,
            ["orangered"] = OrangeRed,
            ["orchid"] = Orchid,
            ["palegoldenrod"] = PaleGoldenrod,
            ["palegreen"] = PaleGreen,
            ["paleturquoise"] = PaleTurquoise,
            ["palevioletred"] = PaleVioletRed,
            ["papayawhip"] = PapayaWhip,
            ["peachpuff"] = PeachPuff,
            ["peru"] = Peru,
            ["pink"] = Pink,
            ["plum"] = Plum,
            ["powderblue"] = PowderBlue,
            ["purple"] = Purple,
            ["red"] = Red,
            ["rosybrown"] = RosyBrown,
            ["royalblue"] = RoyalBlue,
            ["saddlebrown"] = SaddleBrown,
            ["salmon"] = Salmon,
            ["sandybrown"] = SandyBrown,
            ["seagreen"] = SeaGreen,
            ["seashell"] = SeaShell,
            ["sienna"] = Sienna,
            ["silver"] = Silver,
            ["skyblue"] = SkyBlue,
            ["slateblue"] = SlateBlue,
            ["slategray"] = SlateGray,
            ["snow"] = Snow,
            ["springgreen"] = SpringGreen,
            ["steelblue"] = SteelBlue,
            ["tan"] = Tan,
            ["teal"] = Teal,
            ["thistle"] = Thistle,
            ["tomato"] = Tomato,
            ["turquoise"] = Turquoise,
            ["violet"] = Violet,
            ["wheat"] = Wheat,
            ["white"] = White,
            ["whitesmoke"] = WhiteSmoke,
            ["yellow"] = Yellow,
            ["yellowgreen"] = YellowGreen,
        };

        #endregion

        private static readonly Dictionary<Color, string> DefaultColorsInverted =
            DefaultColors.ToLookup(pair => pair.Value).ToDictionary(i => i.Key, i => i.First().Key);

        public readonly string? Name()
        {
            return DefaultColorsInverted.TryGetValue(this, out var name) ? name : null;
        }
    }
}
