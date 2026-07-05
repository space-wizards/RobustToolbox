using System;
using System.Numerics;
using Robust.Shared.Random;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility;

public static class ColorExtensions
{

    /// <summary>
    ///     Generates a list of triadic complementary colors
    /// </summary>
    public static Color[] GetTriadicComplementaries(this Color color)
    {
        return GetComplementaryColors(color, 0.120f);
    }

    /// <summary>
    ///     Generates a list of split complementary colors
    /// </summary>
    public static Color[] GetSplitComplementaries(this Color color)
    {
        return GetComplementaryColors(color, 0.150f);
    }

    /// <summary>
    ///     Generates a list containing the base color and two copies of a single complementary color
    /// </summary>
    public static Color[] GetOneComplementary(this Color color)
    {
        return GetComplementaryColors(color, 0.180f);
    }

    /// <summary>
    ///    Generates a complementary colour palette for a provided
    ///    colour by rotating a set amount of degrees around the
    ///    colour wheel, and then varying the value and saturation
    ///    slightly.
    /// </summary>
    /// <returns>
    ///     A list of 3 colors.
    /// </returns>
    public static Color[] GetComplementaryColors(Color color, float angle)
    {
        var hsl = Color.ToHsl(color);
        var random = new RobustRandom();
        // sorry about how messy these are, but to get all random values we need to reroll for positive and negative HSL.
        // since we want to rotate x degrees around the colour wheel, we need to do so in both directions- doing x + x degrees will give us the wrong hue!

        var hVal = hsl.X + angle;
        hVal -= MathF.Floor(hVal);
        var positiveHSL = new Vector4(
            hVal,
            MathHelper.Clamp01(hsl.Y + random.Next(-20 / 100, 0)),
            MathHelper.Clamp01(hsl.Z + random.Next(-15 / 100, 16/ 100)),
            hsl.W);

        var hVal1 = hsl.X - angle;
        hVal1 += hVal1 <= 0f ? hVal1 + 0.360f : hVal1;
        var negativeHSL = new Vector4(
            hVal1,
            MathHelper.Clamp01(hsl.Y + random.Next(-20 / 100, 0)),
            MathHelper.Clamp01(hsl.Z + random.Next(-15 / 100, 16 / 100)),
            hsl.W);

        var c0 = Color.FromHsl(positiveHSL);
        var c1 = Color.FromHsl(negativeHSL);

        var palette = new Color[] { color, c0, c1 };
        return palette;
    }
}
