using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using JetBrains.Annotations;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using ImageConfiguration = SixLabors.ImageSharp.Configuration;

namespace Robust.Shared.Resources;

/// <summary>
/// RSI manipulation and loading behavior. Server (Packaging/ACZ) and client (loading).
/// </summary>
internal static class RsiLoading
{
    private static readonly float[] OneArray = {1};

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     The minimum version of RSI we can load.
    /// </summary>
    public const uint MINIMUM_RSI_VERSION = 1;

    /// <summary>
    ///     The maximum version of RSI we can load.
    /// </summary>
    public const uint MAXIMUM_RSI_VERSION = 1;

    internal const string RsicPngField = "robusttoolbox_rsic_meta";

    internal static RsiMetadata LoadRsiMetadata(string metadata)
    {
        var manifestJson = JsonSerializer.Deserialize<RsiJsonMetadata>(metadata, SerializerOptions);

        if (manifestJson == null)
            throw new RSILoadException("Manifest JSON failed to deserialize!");

        return LoadRsiMetadataCore(manifestJson);
    }

    internal static RsiMetadata LoadRsiMetadata(Stream manifestFile)
    {
        var manifestJson = JsonSerializer.Deserialize<RsiJsonMetadata>(manifestFile, SerializerOptions);

        if (manifestJson == null)
            throw new RSILoadException($"Manifest JSON failed to deserialize!");

        return LoadRsiMetadataCore(manifestJson);
    }

    private static RsiMetadata LoadRsiMetadataCore(RsiJsonMetadata manifestJson)
    {
        var size = manifestJson.Size;
        var states = new StateMetadata[manifestJson.States.Length];

        for (var stateI = 0; stateI < manifestJson.States.Length; stateI++)
        {
            var stateObject = manifestJson.States[stateI];
            var stateName = stateObject.Name;
            int dirValue;

            if (stateObject.Directions is { } dirVal)
            {
                dirValue = dirVal;
                if (dirVal is not (1 or 4 or 8))
                {
                    throw new RSILoadException(
                        $"Invalid direction for state '{stateName}': {dirValue}. Expected 1, 4 or 8");
                }
            }
            else
            {
                dirValue = 1;
            }

            // We can ignore selectors and flags for now,
            // because they're not used yet!

            // Get the lists of delays.
            float[][] delays;
            if (stateObject.Delays != null)
            {
                delays = stateObject.Delays;

                if (delays.Length != dirValue)
                {
                    throw new RSILoadException(
                        $"Direction frames list count ({dirValue}) does not match amount of delays specified ({delays.Length}) for state '{stateName}'.");
                }

                for (var i = 0; i < delays.Length; i++)
                {
                    var delayList = delays[i];
                    if (delayList.Length == 0)
                    {
                        delays[i] = OneArray;
                    }
                }
            }
            else
            {
                delays = new float[dirValue][];
                // No delays specified, default to 1 frame per dir.
                for (var i = 0; i < dirValue; i++)
                {
                    delays[i] = OneArray;
                }
            }

            states[stateI] = new StateMetadata(stateName, dirValue, delays);
        }

        var textureParams = TextureLoadParameters.Default;
        if (manifestJson.Load is { } load)
        {
            textureParams = new TextureLoadParameters
            {
                SampleParameters = TextureSampleParameters.Default,
                Srgb = load.Srgb
            };
        }

        // Check for duplicate states
        for (var i = 0; i < states.Length; i++)
        {
            var stateId = states[i].StateId;

            for (int j = i + 1; j < states.Length; j++)
            {
                if (stateId == states[j].StateId)
                    throw new RSILoadException($"RSI has a duplicate stateId '{stateId}'.");
            }
        }

        return new RsiMetadata(size, states, textureParams, manifestJson.MetaAtlas, manifestJson.Rsic);
    }

    internal static int[] CalculateFrameCounts(RsiMetadata metadata)
    {
        var counts = new int[metadata.States.Length];

        for (var i = 0; i < metadata.States.Length; i++)
        {
            var state = metadata.States[i];
            counts[i] = state.Delays.Sum(delayList => delayList.Length);
        }

        return counts;
    }

    internal static Image<Rgba32>[] LoadImages(
        RsiMetadata metadata,
        ImageConfiguration configuration,
        Func<string, Stream> openStream)
    {
        var images = new Image<Rgba32>[metadata.States.Length];

        var decoderOptions = new DecoderOptions
        {
            Configuration = configuration,
        };

        var frameSize = metadata.Size;

        try
        {
            for (var i = 0; i < metadata.States.Length; i++)
            {
                var state = metadata.States[i];
                using var stream = openStream(state.StateId);

                var image = Image.Load<Rgba32>(decoderOptions, stream);
                images[i] = image;

                if (image.Width % frameSize.X != 0 || image.Height % frameSize.Y != 0)
                {
                    var regDims = $"{image.Width}x{image.Height}";
                    var iconDims = $"{frameSize.X}x{frameSize.Y}";
                    throw new RSILoadException($"State '{state.StateId}' image size ({regDims}) is not a multiple of the icon size ({iconDims}).");
                }
            }

            return images;
        }
        catch
        {
            foreach (var image in images)
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                image?.Dispose();
            }

            throw;
        }
    }

    internal static Image<Rgba32> GenerateAtlas(
        RsiMetadata metadata,
        int[] frameCounts,
        Image<Rgba32>[] images,
        ImageConfiguration configuration,
        out int dimX)
    {
        var frameSize = metadata.Size;

        // Poorly hacked in texture atlas support here.
        var totalFrameCount = frameCounts.Sum();

        // Generate atlas.
        var dimensionX = (int) MathF.Ceiling(MathF.Sqrt(totalFrameCount));
        var dimensionY = (int) MathF.Ceiling((float) totalFrameCount / dimensionX);

        dimX = dimensionX;

        var sheet = new Image<Rgba32>(configuration, dimensionX * frameSize.X, dimensionY * frameSize.Y);

        try
        {
            var sheetIndex = 0;
            for (var index = 0; index < frameCounts.Length; index++)
            {
                var frameCount = frameCounts[index];
                var image = images[index];

                // Blit all the frames over.
                for (var i = 0; i < frameCount; i++)
                {
                    var srcWidth = (image.Width / frameSize.X);
                    var srcColumn = i % srcWidth;
                    var srcRow = i / srcWidth;
                    var srcPos = (srcColumn * frameSize.X, srcRow * frameSize.Y);

                    var sheetColumn = (sheetIndex + i) % dimensionX;
                    var sheetRow = (sheetIndex + i) / dimensionX;
                    var sheetPos = (sheetColumn * frameSize.X, sheetRow * frameSize.Y);

                    var srcBox = UIBox2i.FromDimensions(srcPos, frameSize);

                    ImageOps.Blit(image, srcBox, sheet, sheetPos);
                }

                sheetIndex += frameCount;
            }
        }
        catch
        {
            sheet.Dispose();
            throw;
        }

        return sheet;
    }

    public static void Warmup()
    {
        // Just a random RSI I pulled from SS14.
        const string warmupJson = @"{""version"":1,""license"":""CC-BY-SA-3.0"",""copyright"":""Space Wizards Federation"",""size"":{""x"":32,""y"":32},""states"":[{""name"":""mono""}]}";
        JsonSerializer.Deserialize<RsiJsonMetadata>(warmupJson, SerializerOptions);
    }

    internal sealed class RsiMetadata(Vector2i size, StateMetadata[] states, TextureLoadParameters loadParameters, bool metaAtlas, bool rsic)
    {
        public readonly Vector2i Size = size;
        public readonly StateMetadata[] States = states;
        public readonly TextureLoadParameters LoadParameters = loadParameters;
        public readonly bool MetaAtlas = metaAtlas;
        public readonly bool Rsic = rsic;
    }

    internal sealed class StateMetadata
    {
        public readonly string StateId;
        public readonly int DirCount;
        public readonly float[][] Delays;

        public StateMetadata(string stateId, int dirCount, float[][] delays)
        {
            StateId = stateId;
            DirCount = dirCount;

            Delays = delays;

            DebugTools.Assert(delays.Length == DirCount);
        }
    }

    // To be directly deserialized.
    [UsedImplicitly]
    private sealed record RsiJsonMetadata(
        Vector2i Size,
        StateJsonMetadata[] States,
        RsiJsonLoad? Load,
        bool MetaAtlas = true,
        bool Rsic = true);

    [UsedImplicitly]
    private sealed record StateJsonMetadata(string Name, int? Directions, float[][]? Delays);

    [UsedImplicitly]
    private sealed record RsiJsonLoad(bool Srgb = true);
}

[Serializable]
[Virtual]
public class RSILoadException : Exception
{
    public RSILoadException()
    {
    }

    public RSILoadException(string message) : base(message)
    {
    }

    public RSILoadException(string message, Exception inner) : base(message, inner)
    {
    }
}
