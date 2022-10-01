using System.Text.RegularExpressions;
using Robust.Shared.Maths;
using Robust.Shared.Resources;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Packaging.AssetProcessing.Passes;

// This is a proof of concept/example. The client is not currently able to load these.

/// <summary>
/// Packs .rsi bundles into .rsic files,
/// that are single pre-atlassed PNG files with JSON metadata embedded in the PNG header.
/// </summary>
internal sealed class AssetPassPackRsis : AssetPass
{
    private readonly Dictionary<string, RsiDat> _foundRsis = new();

    private static readonly Regex RegexMetaJson = new(@"^(.+)\.rsi/meta\.json$");
    private static readonly Regex RegexPng = new(@"^(.+)\.rsi/(.+)\.png$");

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
                return;

            RunJob(() =>
            {
                // Console.WriteLine($"Packing RSI: {key}");

                var result = PackRsi($"{key}.rsi", dat);
                SendFile(result);
            });
        }
    }

    private static AssetFile PackRsi(string rsiPath, RsiDat dat)
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

        // Check for duplicate states
        for (var i = 0; i < metadata.States.Length; i++)
        {
            var stateId = metadata.States[i].StateId;

            for (int j = i + 1; j < metadata.States.Length; j++)
            {
                if (stateId == metadata.States[j].StateId)
                    throw new RSILoadException($"RSI '{rsiPath}' has a duplicate stateId '{stateId}'.");
            }
        }

        var stateCount = metadata.States.Length;
        var toAtlas = new StateReg[stateCount];

        var frameSize = metadata.Size;

        // Do every state.
        for (var index = 0; index < metadata.States.Length; index++)
        {
            ref var reg = ref toAtlas[index];

            var stateObject = metadata.States[index];
            // Load image from disk.
            var texFile = dat.StatesFound[stateObject.StateId];
            using (var stream = texFile.Open())
            {
                reg.Src = Image.Load<Rgba32>(stream);
            }

            if (reg.Src.Width % frameSize.X != 0 || reg.Src.Height % frameSize.Y != 0)
            {
                var regDims = $"{reg.Src.Width}x{reg.Src.Height}";
                var iconDims = $"{frameSize.X}x{frameSize.Y}";
                throw new RSILoadException(
                    $"State '{stateObject.StateId}' image size ({regDims}) is not a multiple of the icon size ({iconDims}).");
            }

            // Load all frames into a list so we can operate on it more sanely.
            reg.TotalFrameCount = stateObject.Delays.Sum(delayList => delayList.Length);
        }

        // Poorly hacked in texture atlas support here.
        var totalFrameCount = toAtlas.Sum(p => p.TotalFrameCount);

        // Generate atlas.
        var dimensionX = (int) MathF.Ceiling(MathF.Sqrt(totalFrameCount));
        var dimensionY = (int) MathF.Ceiling((float) totalFrameCount / dimensionX);

        var sheet = new Image<Rgba32>(dimensionX * frameSize.X, dimensionY * frameSize.Y);

        var sheetIndex = 0;
        for (var index = 0; index < toAtlas.Length; index++)
        {
            ref var reg = ref toAtlas[index];
            // Blit all the frames over.
            for (var i = 0; i < reg.TotalFrameCount; i++)
            {
                var srcWidth = (reg.Src.Width / frameSize.X);
                var srcColumn = i % srcWidth;
                var srcRow = i / srcWidth;
                var srcPos = (srcColumn * frameSize.X, srcRow * frameSize.Y);

                var sheetColumn = (sheetIndex + i) % dimensionX;
                var sheetRow = (sheetIndex + i) / dimensionX;
                var sheetPos = (sheetColumn * frameSize.X, sheetRow * frameSize.Y);

                var srcBox = UIBox2i.FromDimensions(srcPos, frameSize);

                ImageOps.Blit(reg.Src, srcBox, sheet, sheetPos);
            }

            sheetIndex += reg.TotalFrameCount;
        }

        for (var i = 0; i < toAtlas.Length; i++)
        {
            ref var reg = ref toAtlas[i];
            reg.Src.Dispose();
        }

        var ms = new MemoryStream();
        sheet.Metadata.GetPngMetadata().TextData.Add(new PngTextData("Description", metaJson, null, null));
        sheet.SaveAsPng(ms);

        sheet.Dispose();

        return new AssetFileMemory($"{rsiPath}c", ms.ToArray());
    }

    internal struct StateReg
    {
        public Image<Rgba32> Src;
        public int TotalFrameCount;
    }

    private sealed class RsiDat
    {
        public AssetFile? MetaJson;
        public readonly Dictionary<string, AssetFile> StatesFound = new();
    }
}
