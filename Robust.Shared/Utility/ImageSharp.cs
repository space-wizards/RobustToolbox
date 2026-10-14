using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Webp;

namespace Robust.Shared.Utility;

/// <summary>
/// ImageSharp (a dependency)'s default configuration setup.
/// </summary>
internal static class ImageSharp
{
    /// <summary>
    /// Detectors that Load() can use to match to the specific implementations.
    /// </summary>
    private static readonly List<IImageFormatDetector> Detectors =
    [
        // Explicitly allowed in sandbox:
        new PngImageFormatDetector(),
        new JpegImageFormatDetector(),
        new GifImageFormatDetector(),
        new BmpImageFormatDetector(),
        new TgaImageFormatDetector(),

        // Webp is registered in clyde and used in WizDen
        new WebpImageFormatDetector(),

        // No usages (explicitly deprecate at some point):
        new QoiImageFormatDetector(),
        new PbmImageFormatDetector()

        // Has (or had) vulnerabilities:
        // new TiffImageFormatDetector()
    ];

    // We use ModuleInitializer here to ensure this always runs before any other code (it's security sensitive),
    // and it helpfully makes sure this will only run once, to avoid race conditions in the static Default object.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void InitializeImageSharp()
    {
        RestrictImageSharpDetectors();
    }

    /// <summary>
    /// Restricts the format detectors ImageSharp uses for the dynamic Load() method.
    /// </summary>
    /// <remarks>
    /// This is security sensitive as it blocks methods that cannot be detected by the sandbox due to its dynamic nature.
    /// </remarks>
    private static void RestrictImageSharpDetectors()
    {
        var formats = SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager;
        formats.ClearImageFormatDetectors();

        foreach (var detector in Detectors)
        {
            formats.AddImageFormatDetector(detector);
        }
    }
}
