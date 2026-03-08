using System;
using System.Numerics;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Maths.Colors;
using Robust.Shared.Utility;

namespace Robust.Shared.ColorNaming;

// color naming algorithim is inspired by https://react-spectrum.adobe.com/blog/accessible-color-descriptions.html

public static class ColorNaming
{
    private static readonly (float Hue, string Loc)[] HueNames =
    {
        (float.DegreesToRadians(0f), "color-pink"),
        (float.DegreesToRadians(15f), "color-red"),
        (float.DegreesToRadians(45f), "color-orange"),
        (float.DegreesToRadians(90f), "color-yellow"),
        (float.DegreesToRadians(135f), "color-green"),
        (float.DegreesToRadians(180f), "color-cyan"),
        (float.DegreesToRadians(240f), "color-blue"),
        (float.DegreesToRadians(285f), "color-purple"),
        (float.DegreesToRadians(330f), "color-pink"),
    };
    // one past 360 because we're now inclusive on the upper for testing if we're out of bounds
    private static readonly (float Hue, string Loc) HueFallback = (float.DegreesToRadians(361f), "color-pink");

    private const float BrownLightnessThreshold = 0.675f;
    private static readonly LocId OrangeString = "color-orange";
    private static readonly LocId BrownString = "color-brown";

    private const float VeryDarkLightnessThreshold = 0.25f;
    private const float DarkLightnessThreshold = 0.5f;
    private const float NeutralLightnessThreshold = 0.7f;
    private const float LightLightnessThreshold = 0.85f;

    private static readonly LocId VeryDarkString = "color-very-dark";
    private static readonly LocId DarkString = "color-dark";
    private static readonly LocId LightString = "color-light";
    private static readonly LocId VeryLightString = "color-very-light";

    private static readonly LocId MixedHueString = "color-mixed-hue";
    private static readonly LocId LightLowChromaString = "color-pale";
    private static readonly LocId DarkLowChromaString = "color-gray-adjective";
    private static readonly LocId HighChromaString = "color-strong";

    private static readonly LocId WhiteString = "color-white";
    private static readonly LocId GrayString = "color-gray";
    private static readonly LocId BlackString = "color-black";

    private const float LowChromaThreshold = 0.07f;
    private const float HighChromaThreshold = 0.16f;
    private const float LightLowChromaThreshold = 0.6f;

    private const float WhiteLightnessThreshold = 0.99f;
    private const float BlackLightnessThreshold = 0.01f;
    private const float GrayChromaThreshold = 0.01f;

    private static (string Loc, float AdjustedLightness) DescribeHue(OklchColor oklch, ILocalizationManager localization)
    {
        var lightness = oklch.L;
        var hue = oklch.H;

        for (var i = 0; i < HueNames.Length; i++)
        {
            var prevData = HueNames[i];
            var nextData = i + 1 < HueNames.Length ? HueNames[i + 1] : HueFallback;

            if (prevData.Hue > hue || hue >= nextData.Hue)
                continue;

            var loc = prevData.Loc;
            var adjustedLightness = lightness;

            if (prevData.Loc == OrangeString && lightness <= BrownLightnessThreshold)
                loc = BrownString;
            else if (prevData.Loc == OrangeString)
                adjustedLightness = lightness - BrownLightnessThreshold + DarkLightnessThreshold;

            if (hue >= (prevData.Hue + nextData.Hue) / 2f && prevData.Loc != nextData.Loc)
            {
                if (localization.TryGetString($"{loc}-{nextData.Loc}", out var hueName))
                    return (hueName!, adjustedLightness);
                else
                    return (localization.GetString(MixedHueString, ("a", localization.GetString(loc)), ("b", localization.GetString(nextData.Loc))), adjustedLightness);
            }

            return (localization.GetString(loc), adjustedLightness);
        }

        DebugTools.Assert($"colour ({oklch}) hue {hue} is outside of expected bounds");
        return (localization.GetString("color-unknown"), lightness);
    }

    private static string? DescribeChroma(OklchColor oklch, ILocalizationManager localization)
    {
        var lightness = oklch.L;
        var chroma = oklch.C;

        if (chroma <= LowChromaThreshold)
        {
            if (lightness >= LightLowChromaThreshold)
                return localization.GetString(LightLowChromaString);
            else
                return localization.GetString(DarkLowChromaString);
        }
        else if (chroma >= HighChromaThreshold)
        {
            return localization.GetString(HighChromaString);
        }

        return null;
    }

    private static string? DescribeLightness(OklchColor oklch, ILocalizationManager localization)
    {
        return oklch.L switch
        {
            < VeryDarkLightnessThreshold => localization.GetString(VeryDarkString),
            < DarkLightnessThreshold => localization.GetString(DarkString),
            < NeutralLightnessThreshold => null,
            < LightLightnessThreshold => localization.GetString(LightString),
            _ => localization.GetString(VeryLightString)
        };
    }

    /// <summary>
    /// Textually describes a color
    /// </summary>
    /// <returns>
    /// Returns a localized textual description of the provided color
    /// </returns>
    /// <param name="srgb">A Color that is assumed to be in SRGB (the default for most cases)</param>
    public static string Describe(Color srgb, ILocalizationManager localization)
    {
        var oklch = srgb.ToLch();

        if (oklch.L >= WhiteLightnessThreshold)
            return localization.GetString(WhiteString);

        if (oklch.L <= BlackLightnessThreshold)
            return localization.GetString(BlackString);

        var (hueDescription, adjustedLightness) = DescribeHue(oklch, localization);
        oklch.L = adjustedLightness;
        var chromaDescription = DescribeChroma(oklch, localization);
        var lightnessDescription = DescribeLightness(oklch, localization);

        if (oklch.C <= GrayChromaThreshold)
        {
            hueDescription = localization.GetString(GrayString);
            chromaDescription = null;
        }

        return (hueDescription, chromaDescription, lightnessDescription) switch
        {
            ({ } hue, { } chroma, { } lightness) => localization.GetString("color-hue-chroma-lightness", ("hue", hue), ("chroma", chroma), ("lightness", lightness)),
            ({ } hue, { } chroma, null) => localization.GetString("color-hue-chroma", ("hue", hue), ("chroma", chroma)),
            ({ } hue, null, { } lightness) => localization.GetString("color-hue-lightness", ("hue", hue), ("lightness", lightness)),
            ({ } hue, null, null) => hue,
        };
    }
}
