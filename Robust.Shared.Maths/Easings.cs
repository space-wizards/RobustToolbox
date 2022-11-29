using System;

namespace Robust.Shared.Maths;

// Reference: https://easings.net/

internal static class Easings
{
    public static float InOutQuint(float p)
    {
        return p < 0.5f ? (16 * p * p * p * p * p) : 1 - MathF.Pow(-2 * p + 2, 5) / 2;
    }
}
