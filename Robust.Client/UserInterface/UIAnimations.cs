using System;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

internal static class UIAnimations
{
    // From https://blog.pkh.me/p/41-fixing-the-iterative-damping-interpolation-in-video-games.html
    public static float LerpAnimate(float a, float b, float dt, float rate)
    {
        return MathHelper.Lerp(a, b, 1f - MathF.Exp(-dt * rate));
    }
}
