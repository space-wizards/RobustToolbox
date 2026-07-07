// ReSharper disable InconsistentNaming
#if MACOS

namespace Robust.Client.Interop.MacOS;

/// <summary>
/// Binding to macOS AppKit.
/// </summary>
internal static class AppKit
{
    // Values pulled from here:
    // https://chromium.googlesource.com/chromium/src/+/b5019b491932dfa597acb3a13a9e7780fb6525a9/ui/gfx/platform_font_mac.mm#53
    public const double NSFontWeightUltraLight = -0.8;
    public const double NSFontWeightThin = -0.6;
    public const double NSFontWeightLight = -0.4;
    public const double NSFontWeightRegular = 0;
    public const double NSFontWeightMedium = 0.23;
    public const double NSFontWeightSemiBold = 0.30;
    public const double NSFontWeightBold = 0.40;
    public const double NSFontWeightHeavy = 0.56;
    public const double NSFontWeightBlack = 0.62;
}

#endif
