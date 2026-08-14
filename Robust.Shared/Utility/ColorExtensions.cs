using System;
using System.Numerics;
using Robust.Shared.Random;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility;

public static class ColorExtensions
{
    public static readonly float AnalogousHueDelta = 30f / 360f; // +/- 1/12. 30 degrees, 0.08333... over hue
    public static readonly float TriadicHueDelta = 120f / 360f; // +/- 1/3. 120 degrees, 0.333 over hue
    public static readonly float SplitComplementaryHueDelta = 150f / 360f; // +/- 5/12. 150 degrees, 0.4166... over hue
    public static readonly float ComplementaryHueDelta = 180f / 360f; // +/- 1/2. 180 degrees

    /// <summary>
    ///     Generates a list of analogous complementary colors
    /// </summary>
    public static Color[] GetAnalogousComplementaries(this Color color)
    {
        return GetComplementaryColors(color, AnalogousHueDelta);
    }

    /// <summary>
    ///     Generates a list of triadic complementary colors
    /// </summary>
    public static Color[] GetTriadicComplementaries(this Color color)
    {
        return GetComplementaryColors(color, TriadicHueDelta);
    }

    /// <summary>
    ///     Generates a list of split complementary colors
    /// </summary>
    public static Color[] GetSplitComplementaries(this Color color)
    {
        return GetComplementaryColors(color, SplitComplementaryHueDelta);
    }

    /// <summary>
    ///     Generates a list containing the base color and two copies of a single complementary color
    /// </summary>
    public static Color[] GetOneComplementary(this Color color)
    {
        return GetComplementaryColors(color, ComplementaryHueDelta);
    }

    /// <summary>
    ///    Generates a complementary color palette for a provided
    ///    color by rotating a set amount of degrees around the
    ///    color wheel, and then varying the value and saturation
    ///    slightly.
    /// </summary>
    /// <returns>
    ///     A list of 3 colors.
    /// </returns>
    public static Color[] GetComplementaryColors(Color color, float hueDelta)
    {
        var hsl = Color.ToHsl(color);
        var random = new RobustRandom();

        // sorry about how messy these are, but to get all random values we need to reroll for positive and negative HSL.
        // since we want to rotate x degrees around the colour wheel, we need to do so in both directions- doing x + x degrees will give us the wrong hue!

        // also varying the saturation and lightness just a little to add some contrast.
        // since 'color' is our main color, we are desaturating our secondary colors.
        // this means our main color will always stand out the most.

        var hVal = hsl.X + hueDelta;
        hVal -= MathF.Floor(hVal);
        var positiveHSL = new Vector4(
            hVal,
            MathHelper.Clamp01(hsl.Y + random.Next(-20 / 100, 0)),
            MathHelper.Clamp01(hsl.Z + random.Next(-15 / 100, 16 / 100)),
            hsl.W);

        var hVal1 = hsl.X - hueDelta;
        if (hVal1 < 0f)
            hVal1 += 1f;
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
