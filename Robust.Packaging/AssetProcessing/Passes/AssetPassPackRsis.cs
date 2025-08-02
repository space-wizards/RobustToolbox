using System.Text.RegularExpressions;
using Robust.Shared.Resources;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png.Chunks;

namespace Robust.Packaging.AssetProcessing.Passes;

// This is a proof of concept/example. The client is not currently able to load these.

/// <summary>
/// Packs .rsi bundles into .rsic files,
/// that are single pre-atlassed PNG files with JSON metadata embedded in the PNG header.
/// </summary>
public sealed class AssetPassPackRsis : AssetPass
{
    private readonly Dictionary<string, RsiDat> _foundRsis = new();

    private static readonly Regex RegexMetaJson = new(@"^(.+)\.rsi/meta\.json$");
    private static readonly Regex RegexPng = new(@"^(.+)\.rsi/(.+)\.png$");

    private readonly Configuration _imageConfiguration;

    public AssetPassPackRsis()
    {
        _imageConfiguration = Configuration.Default.Clone();
        _imageConfiguration.PreferContiguousImageBuffers = true;
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        if (!file.Path.Contains(".rsi/"))
            return AssetFileAcceptResult.Pass;

        // .rsi/meta.json
        var matchMetaJson = RegexMetaJson.Match(file.Path);
        if (matchMetaJson.Success)
        {
            lock (_foundRsis)
            {
                var dat = _foundRsis.GetOrNew(matchMetaJson.Groups[1].Value);
                dat.MetaJson = file;
            }

            return AssetFileAcceptResult.Consumed;
        }

        // .rsi/*.png
        var matchPng = RegexPng.Match(file.Path);
        if (matchPng.Success)
        {
            lock (_foundRsis)
            {
                var dat = _foundRsis.GetOrNew(matchPng.Groups[1].Value);
                dat.StatesFound.Add(matchPng.Groups[2].Value, file);

                return AssetFileAcceptResult.Consumed;
            }
        }

        return AssetFileAcceptResult.Pass;
    }

    protected override void AcceptFinished()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        foreach (var (key, dat) in _foundRsis)
        {
            if (dat.MetaJson == null)
                continue;

            RunJob(() =>
            {
                // Console.WriteLine($"Packing RSI: {key}");

                var result = PackRsi($"{key}.rsi", dat);
                if (result == null)
                {
                    // Don't rsic pack this one.
                    SkipRsiPack(dat);
                    return;
                }
                SendFile(result);
            });
        }
    }

    private void SkipRsiPack(RsiDat dat)
    {
        SendFile(dat.MetaJson!);
        foreach (var file in dat.StatesFound.Values)
        {
            SendFile(file);
        }
    }

    private AssetFile? PackRsi(string rsiPath, RsiDat dat)
    {
        RsiLoading.RsiMetadata metadata;
        string metaJson;
        using (var manifestFile = dat.MetaJson!.Open())
        {
            metadata = RsiLoading.LoadRsiMetadata(manifestFile);

            manifestFile.Position = 0;
            using var sr = new StreamReader(manifestFile);
            metaJson = sr.ReadToEnd();
        }

        if (!metadata.Rsic)
            return null;

        var frameCounts = RsiLoading.CalculateFrameCounts(metadata);
        var images = RsiLoading.LoadImages(metadata, _imageConfiguration, name => dat.StatesFound[name].Open());

        try
        {
            using var sheet = RsiLoading.GenerateAtlas(metadata, frameCounts, images, _imageConfiguration, out _);
            var ms = new MemoryStream();
            sheet.Metadata.GetPngMetadata().TextData.Add(new PngTextData(RsiLoading.RsicPngField, metaJson, "", ""));
            sheet.SaveAsPng(ms);

            Logger?.Verbose($"Done packing {rsiPath}");

            return new AssetFileMemory($"{rsiPath}c", ms.ToArray());
        }
        finally
        {
            foreach (var image in images)
            {
                image.Dispose();
            }
        }
    }

    private sealed class RsiDat
    {
        public AssetFile? MetaJson;
        public readonly Dictionary<string, AssetFile> StatesFound = new();
    }
}
