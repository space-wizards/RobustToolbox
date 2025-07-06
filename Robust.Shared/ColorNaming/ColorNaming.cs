using System;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Shared.ColorNaming;

// color naming algorithim is inspired by https://react-spectrum.adobe.com/blog/accessible-color-descriptions.html

public static class ColorNaming
{
    private static ILocalizationManager LocalizationManager => IoCManager.Resolve<ILocalizationManager>();

    private static (float Hue, string Loc)[] HueNames =
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
    private static (float Hue, string Loc) HueFallback = (float.DegreesToRadians(360f), "color-pink");

    private const float BrownLightnessThreshold = 0.675f;
    private static LocId OrangeString = "color-orange";
    private static LocId BrownString = "color-brown";

    private const float VeryDarkLightnessThreshold = 0.25f;
    private const float DarkLightnessThreshold = 0.5f;
    private const float NeutralLightnessThreshold = 0.7f;
    private const float LightLightnessThreshold = 0.85f;

    private static LocId VeryDarkString = "color-very-dark";
    private static LocId DarkString = "color-dark";
    private static LocId LightString = "color-light";
    private static LocId VeryLightString = "color-very-light";

    private static LocId MixedHueString = "color-mixed-hue";
    private static LocId LightLowChromaString = "color-pale";
    private static LocId DarkLowChromaString = "color-gray-adjective";
    private static LocId HighChromaString = "color-strong";

    private static LocId WhiteString = "color-white";
    private static LocId GrayString = "color-gray";
    private static LocId BlackString = "color-black";

    private const float LowChromaThreshold = 0.07f;
    private const float HighChromaThreshold = 0.16f;
    private const float LightLowChromaThreshold = 0.6f;

    private const float WhiteLightnessThreshold = 0.99f;
    private const float BlackLightnessThreshold = 0.01f;
    private const float GrayChromaThreshold = 0.01f;

    private static (string Loc, float AdjustedLightness) DescribeHue(Vector4 oklch)
    {
        var (lightness, _, hue, _) = oklch;

        for (var i = 0; i < HueNames.Length; i++)
        {
            var prevData = HueNames[i];
            var nextData = i+1 < HueNames.Length ? HueNames[i+1] : HueFallback;

            if (prevData.Hue >= hue || hue > nextData.Hue)
                continue;

            var loc = prevData.Loc;
            var adjustedLightness = lightness;

            if (prevData.Loc == OrangeString && lightness <= BrownLightnessThreshold)
                loc = BrownString;
            else if (prevData.Loc == OrangeString)
                adjustedLightness = lightness - BrownLightnessThreshold + DarkLightnessThreshold;

            if (hue >= (prevData.Hue + nextData.Hue)/2f && prevData.Loc != nextData.Loc)
            {
                if (LocalizationManager.TryGetString($"{loc}-{nextData.Loc}", out var hueName))
                    return (hueName!, adjustedLightness);
                else
                    return (Loc.GetString(MixedHueString, ("a", Loc.GetString(loc)), ("b", Loc.GetString(nextData.Loc))), adjustedLightness);
            }

            return (Loc.GetString(loc), adjustedLightness);
        }

        throw new ArgumentOutOfRangeException("oklch", $"colour ({oklch}) hue {hue} is outside of expected bounds");
    }

    private static string? DescribeChroma(Vector4 oklch)
    {
        var (lightness, chroma, _, _) = oklch;

        if (chroma <= LowChromaThreshold)
        {
            if (lightness >= LightLowChromaThreshold)
                return Loc.GetString(LightLowChromaString);
            else
                return Loc.GetString(DarkLowChromaString);
        }
        else if (chroma >= HighChromaThreshold)
        {
            return Loc.GetString(HighChromaString);
        }

        return null;
    }

    private static string? DescribeLightness(Vector4 oklch)
    {
        return oklch.X switch
        {
            < VeryDarkLightnessThreshold => Loc.GetString(VeryDarkString),
            < DarkLightnessThreshold => Loc.GetString(DarkString),
            < NeutralLightnessThreshold => null,
            < LightLightnessThreshold => Loc.GetString(LightString),
            _ => Loc.GetString(VeryLightString)
        };
    }

    /// <summary>
    /// Textually describes a color
    /// </summary>
    /// <returns>
    /// Returns a localized textual description of the provided color
    /// </returns>
    /// <param name="srgb">A Color that is assumed to be in SRGB (the default for most cases)</param>
    public static string Describe(Color srgb)
    {
        var oklch = Color.ToLch(Color.ToLab(Color.FromSrgb(srgb)));

        if (oklch.X >= WhiteLightnessThreshold)
            return Loc.GetString(WhiteString);

        if (oklch.X <= BlackLightnessThreshold)
            return Loc.GetString(BlackString);

        var (hueDescription, adjustedLightness) = DescribeHue(oklch);
        oklch.X = adjustedLightness;
        var chromaDescription = DescribeChroma(oklch);
        var lightnessDescription = DescribeLightness(oklch);

        if (oklch.Y <= GrayChromaThreshold)
        {
            hueDescription = Loc.GetString(GrayString);
            chromaDescription = null;
        }

        return (hueDescription, chromaDescription, lightnessDescription) switch
        {
            ({ } hue, { } chroma, { } lightness) => Loc.GetString("color-hue-chroma-lightness", ("hue", hue), ("chroma", chroma), ("lightness", lightness)),
            ({ } hue, { } chroma, null) => Loc.GetString("color-hue-chroma", ("hue", hue), ("chroma", chroma)),
            ({ } hue, null, { } lightness) => Loc.GetString("color-hue-lightness", ("hue", hue), ("lightness", lightness)),
            ({ } hue, null, null) => hue,
        };
    }
}
