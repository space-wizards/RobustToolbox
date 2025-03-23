using System;
using System.IO;
using System.Text.Json;
using JetBrains.Annotations;
using Robust.Shared.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

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

    internal static RsiMetadata LoadRsiMetadata(Stream manifestFile)
    {
        var manifestJson = JsonSerializer.Deserialize<RsiJsonMetadata>(manifestFile, SerializerOptions);

        if (manifestJson == null)
            throw new RSILoadException($"Manifest JSON failed to deserialize!");

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

        return new RsiMetadata(size, states, textureParams, manifestJson.MetaAtlas);
    }

    public static void Warmup()
    {
        // Just a random RSI I pulled from SS14.
        const string warmupJson = @"{""version"":1,""license"":""CC-BY-SA-3.0"",""copyright"":""Space Wizards Federation"",""size"":{""x"":32,""y"":32},""states"":[{""name"":""mono""}]}";
        JsonSerializer.Deserialize<RsiJsonMetadata>(warmupJson, SerializerOptions);
    }

    internal sealed class RsiMetadata(Vector2i size, StateMetadata[] states, TextureLoadParameters loadParameters, bool metaAtlas)
    {
        public readonly Vector2i Size = size;
        public readonly StateMetadata[] States = states;
        public readonly TextureLoadParameters LoadParameters = loadParameters;
        public readonly bool MetaAtlas = metaAtlas;
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
        bool MetaAtlas = true);

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
