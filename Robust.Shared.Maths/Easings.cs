using System;
using JetBrains.Annotations;

namespace Robust.Shared.Maths;

// Reference: https://easings.net/

/// <summary>
///     A static class for computing easings for animations.
///     The parameter "p" is the absolute progress of the animation between 0 and 1.
/// </summary>
[PublicAPI]
public static class Easings
{
    #region Trig

    public static float InSine(float p)
    {
        return 1.0f - MathF.Cos(p * MathF.PI / 2.0f);
    }

    public static float OutSine(float p)
    {
        return MathF.Sin(p * MathF.PI / 2);
    }

    public static float InOutSine(float p)
    {
        return -(MathF.Cos(MathF.PI * p) - 1.0f) / 2.0f;
    }

    #endregion

    #region Polynomial

    public static float InQuad(float p)
    {
        return p * p;
    }

    public static float OutQuad(float p)
    {
        return 1 - (1 - p) * (1 - p);
    }

    public static float InOutQuad(float p)
    {
        return p < 0.5 ? 2 * p * p : 1 - MathF.Pow(-2 * p + 2, 2) / 2;
    }

    public static float InCubic(float p)
    {
        return p * p * p;
    }

    public static float OutCubic(float p)
    {
        return 1 - MathF.Pow(1 - p, 3);
    }

    public static float InOutCubic(float p)
    {
        return p < 0.5 ? 4 * p * p * p : 1 - MathF.Pow(-2 * p + 2, 3) / 2;
    }

    public static float InQuart(float p)
    {
        return p * p * p * p;
    }

    public static float OutQuart(float p)
    {
        return 1 - MathF.Pow(1 - p, 4);
    }

    public static float InOutQuart(float p)
    {
        return p < 0.5 ? 8 * p * p * p * p : 1 - MathF.Pow(-2 * p + 2, 4) / 2;
    }

    public static float InQuint(float p)
    {
        return p * p * p * p * p;
    }

    public static float OutQuint(float p)
    {
        return 1 - MathF.Pow(1 - p, 5);
    }

    public static float InOutQuint(float p)
    {
        return p < 0.5f ? 16 * p * p * p * p * p : 1 - MathF.Pow(-2 * p + 2, 5) / 2;
    }

    #endregion

    #region Other

    public static float InExpo(float p)
    {
        return p == 0 ? 0 : MathF.Pow(2, 10 * p - 10);
    }

    public static float OutExpo(float p)
    {
        return Math.Abs(p - 1) < 0.0001f ? 1 : 1 - MathF.Pow(2, -10 * p);
    }

    public static float InOutExpo(float p)
    {
        return p == 0.0f
            ? 0
            : Math.Abs(p - 1) < 0.0001f
                ? 1
                : p < 0.5f
                    ? MathF.Pow(2, 20 * p - 10) / 2
                    : (2 - MathF.Pow(2, -20 * p + 10)) / 2;
    }

    public static float InCirc(float p)
    {
        return 1 - MathF.Sqrt(1 - MathF.Pow(p, 2));
    }

    public static float OutCirc(float p)
    {
        return MathF.Sqrt(1 - MathF.Pow(p - 1, 2));
    }

    public static float InOutCirc(float p)
    {
        return p < 0.5
            ? (1 - MathF.Sqrt(1 - MathF.Pow(2 * p, 2))) / 2
            : (MathF.Sqrt(1 - MathF.Pow(-2 * p + 2, 2)) + 1) / 2;
    }

    public static float InBack(float p)
    {
        var c1 = 1.70158f;
        var c3 = c1 + 1;

        return c3 * p * p * p - c1 * p * p;
    }

    public static float OutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;

        return 1 + c3 * MathF.Pow(p - 1, 3) + c1 * MathF.Pow(p - 1, 2);
    }

    public static float InOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;

        return p < 0.5
            ? MathF.Pow(2 * p, 2) * ((c2 + 1) * 2 * p - c2) / 2
            : (MathF.Pow(2 * p - 2, 2) * ((c2 + 1) * (p * 2 - 2) + c2) + 2) / 2;
    }

    /// <remarks>
    ///     elastic in, not "inelastic"
    /// </remarks>
    public static float InElastic(float p)
    {
        const float c4 = 2 * MathF.PI / 3;

        return p == 0
            ? 0
            : Math.Abs(p - 1) < 0.0001f
                ? 1
                : -MathF.Pow(2, 10 * p - 10) * MathF.Sin((p * 10 - 10.75f) * c4);
    }

    public static float OutElastic(float p)
    {
        const float c4 = 2.0f * MathF.PI / 3.0f;

        return p == 0
            ? 0
            : Math.Abs(p - 1) < 0.0001f
                ? 1
                : MathF.Pow(2, -10 * p) * MathF.Sin((p * 10.0f - 0.75f) * c4) + 1.0f;
    }

    public static float InOutElastic(float p)
    {
        const float c5 = 2.0f * MathF.PI / 4.5f;

        return p == 0
            ? 0
            : Math.Abs(p - 1) < 0.0001f
                ? 1
                : p < 0.5
                    ? -(MathF.Pow(2, 20 * p - 10) * MathF.Sin((20.0f * p - 11.125f) * c5)) / 2.0f
                    : MathF.Pow(2, -20.0f * p + 10.0f) * MathF.Sin((20.0f * p - 11.125f) * c5) / 2.0f + 1.0f;
    }

    public static float InBounce(float p)
    {
        return 1 - OutBounce(1 - p);
    }

    public static float OutBounce(float p)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (p < 1 / d1) return n1 * p * p;

        if (p < 2 / d1) return n1 * (p -= 1.5f / d1) * p + 0.75f;

        if (p < 2.5 / d1) return n1 * (p -= 2.25f / d1) * p + 0.9375f;

        return n1 * (p -= 2.625f / d1) * p + 0.984375f;
    }

    public static float InOutBounce(float p)
    {
        return p < 0.5
            ? (1 - OutBounce(1 - 2 * p)) / 2
            : (1 + OutBounce(2 * p - 1)) / 2;
    }

    #endregion
}
