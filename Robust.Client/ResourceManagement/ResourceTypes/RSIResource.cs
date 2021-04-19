using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.ResourceManagement
{
    /// <summary>
    ///     Handles the loading code for RSI files.
    ///     See <see cref="RSI"/> for the RSI API itself.
    /// </summary>
    public sealed class RSIResource : BaseResource
    {
        private static readonly float[] OneArray = {1};

        private static readonly JsonSerializerOptions SerializerOptions =
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowTrailingCommas = true
            };

        public RSI RSI { get; private set; } = default!;

        /// <summary>
        ///     The minimum version of RSI we can load.
        /// </summary>
        public const uint MINIMUM_RSI_VERSION = 1;

        /// <summary>
        ///     The maximum version of RSI we can load.
        /// </summary>
        public const uint MAXIMUM_RSI_VERSION = 1;

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            var clyde = IoCManager.Resolve<IClyde>();

            var loadStepData = new LoadStepData {Path = path};
            LoadPreTexture(cache, loadStepData);

            // Load atlas.
            LoadTexture(clyde, loadStepData);

            LoadPostTexture(loadStepData);
            LoadFinish(cache, loadStepData);

            loadStepData.AtlasSheet.Dispose();
        }

        internal static void LoadTexture(IClyde clyde, LoadStepData loadStepData)
        {
            loadStepData.AtlasTexture = clyde.LoadTextureFromImage(
                loadStepData.AtlasSheet,
                loadStepData.Path.ToString());
        }

        internal static void LoadPreTexture(IResourceCache cache, LoadStepData data)
        {
            var metadata = LoadRsiMetadata(cache, data.Path);

            var stateCount = metadata.States.Length;
            var toAtlas = new StateReg[stateCount];

            var frameSize = metadata.Size;
            var rsi = new RSI(frameSize, data.Path);

            var callbackOffsets = new Dictionary<RSI.StateId, Vector2i[][]>(stateCount);

            // Do every state.
            for (var index = 0; index < metadata.States.Length; index++)
            {
                ref var reg = ref toAtlas[index];

                var stateObject = metadata.States[index];
                // Load image from disk.
                var texPath = data.Path / (stateObject.StateId + ".png");
                using (var stream = cache.ContentFileRead(texPath))
                {
                    reg.Src = Image.Load<Rgba32>(stream);
                }

                if (reg.Src.Width % frameSize.X != 0 || reg.Src.Height % frameSize.Y != 0)
                {
                    throw new RSILoadException("State image size is not a multiple of the icon size.");
                }

                // Load all frames into a list so we can operate on it more sanely.
                reg.TotalFrameCount = stateObject.Delays.Sum(delayList => delayList.Length);

                var (foldedDelays, foldedIndices) = FoldDelays(stateObject.Delays);

                var textures = new Texture[foldedIndices.Length][];
                var callbackOffset = new Vector2i[foldedIndices.Length][];

                for (var i = 0; i < textures.Length; i++)
                {
                    textures[i] = new Texture[foldedIndices[0].Length];
                    callbackOffset[i] = new Vector2i[foldedIndices[0].Length];
                }

                reg.Output = textures;
                reg.Indices = foldedIndices;
                reg.Offsets = callbackOffset;

                var state = new RSI.State(frameSize, stateObject.StateId, stateObject.DirType, foldedDelays,
                    textures);
                rsi.AddState(state);

                callbackOffsets[stateObject.StateId] = callbackOffset;
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

                    reg.Src.Blit(srcBox, sheet, sheetPos);
                }

                sheetIndex += reg.TotalFrameCount;
            }

            for (var i = 0; i < toAtlas.Length; i++)
            {
                ref var reg = ref toAtlas[i];
                reg.Src.Dispose();
            }

            data.Rsi = rsi;
            data.AtlasSheet = sheet;
            data.AtlasList = toAtlas;
            data.FrameSize = frameSize;
            data.DimX = dimensionX;
            data.CallbackOffsets = callbackOffsets;
        }

        internal static void LoadPostTexture(LoadStepData data)
        {
            var dimX = data.DimX;
            var toAtlas = data.AtlasList;
            var frameSize = data.FrameSize;
            var texture = data.AtlasTexture;

            var sheetOffset = 0;
            for (var toAtlasIndex = 0; toAtlasIndex < toAtlas.Length; toAtlasIndex++)
            {
                ref var reg = ref toAtlas[toAtlasIndex];
                for (var i = 0; i < reg.Indices.Length; i++)
                {
                    var dirIndices = reg.Indices[i];
                    var dirOutput = reg.Output[i];
                    var dirOffsets = reg.Offsets[i];

                    for (var j = 0; j < dirIndices.Length; j++)
                    {
                        var index = sheetOffset + dirIndices[j];

                        var sheetColumn = index % dimX;
                        var sheetRow = index / dimX;
                        var sheetPos = (sheetColumn * frameSize.X, sheetRow * frameSize.Y);

                        dirOffsets[j] = sheetPos;
                        dirOutput[j] = new AtlasTexture(texture, UIBox2.FromDimensions(sheetPos, frameSize));
                    }
                }

                sheetOffset += reg.TotalFrameCount;
            }
        }

        internal void LoadFinish(IResourceCache cache, LoadStepData data)
        {
            RSI = data.Rsi;

            if (cache is IResourceCacheInternal cacheInternal)
            {
                cacheInternal.RsiLoaded(new RsiLoadedEventArgs(data.Path, this, data.AtlasSheet, data.CallbackOffsets));
            }
        }

        private static RsiMetadata LoadRsiMetadata(IResourceCache cache, ResourcePath path)
        {
            var manifestPath = path / "meta.json";
            string manifestContents;

            using (var manifestFile = cache.ContentFileRead(manifestPath))
            using (var reader = new StreamReader(manifestFile))
            {
                manifestContents = reader.ReadToEnd();
            }

            // Ok schema validated just fine.
            var manifestJson = JsonSerializer.Deserialize<RsiJsonMetadata>(manifestContents, SerializerOptions);

            if (manifestJson == null)
                throw new RSILoadException("Manifest JSON was null!");

            var size = manifestJson.Size;
            var states = new StateMetadata[manifestJson.States.Length];

            for (var stateI = 0; stateI < manifestJson.States.Length; stateI++)
            {
                var stateObject = manifestJson.States[stateI];
                var stateName = stateObject.Name;
                RSI.State.DirectionType directions;
                int dirValue;

                if (stateObject.Directions is { } dirVal)
                {
                    dirValue = dirVal;
                    directions = dirVal switch
                    {
                        1 => RSI.State.DirectionType.Dir1,
                        4 => RSI.State.DirectionType.Dir4,
                        8 => RSI.State.DirectionType.Dir8,
                        _ => throw new RSILoadException($"Invalid direction: {dirValue} expected 1, 4 or 8")
                    };
                }
                else
                {
                    dirValue = 1;
                    directions = RSI.State.DirectionType.Dir1;
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
                            "DirectionsdirectionFramesList count does not match amount of delays specified.");
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

                states[stateI] = new StateMetadata(new RSI.StateId(stateName), directions, delays);
            }

            return new RsiMetadata(size, states);
        }

        /// <summary>
        ///     Folds a per-directional sets of animation delays
        ///     into an equivalent set of animation delays and indices that works for every direction.
        /// </summary>
        internal static (float[] delays, int[][] indices) FoldDelays(float[][] delays)
        {
            if (delays.Length == 1)
            {
                // Short circuit handle single directional sprites.
                var delayList = delays[0];
                var output = new float[delayList.Length];
                var indices = new int[delayList.Length];

                for (var i = 0; i < delayList.Length; i++)
                {
                    output[i] = delayList[i];
                    indices[i] = i;
                }

                return (output, new[] {indices});
            }

            // Multiply by 1000 so we have millisecond precision in our fixed point.
            const float fixedPointResolution = 1000;

            var dirCount = delays.Length;

            // Convert to int[][] to use our fixed point and avoid floating point pains.
            // Also we mutate these arrays to make the calculations easier.
            // We also calculate the lengths for later.
            var iDelays = new int[dirCount][];
            Span<int> dirLengths = stackalloc int[dirCount];
            var maxLength = 0;

            for (var d = 0; d < dirCount; d++)
            {
                var length = 0;
                var fDelayList = delays[d];
                var delayList = new int[fDelayList.Length];
                iDelays[d] = delayList;

                for (var i = 0; i < delayList.Length; i++)
                {
                    var delay = (int) (fDelayList[i] * fixedPointResolution);
                    delayList[i] = delay;
                    length += delay;
                }

                maxLength = Math.Max(length, maxLength);
                dirLengths[d] = length;
            }

            // Extend final delay so that all sets have the same total length.
            // Strictly speaking this shouldn't be necessary, since the RSI spec mandates equal lengths.
            // But better safe than sorry, especially if there's some funky floating point conversions.
            for (var d = 0; d < dirCount; d++)
            {
                var length = dirLengths[d];
                var diff = maxLength - length;

                iDelays[d][^1] += diff;
            }

            // Calculate base texture indices for the directions.
            Span<int> dirIndexOffsets = stackalloc int[dirCount];
            dirIndexOffsets.Fill(0);

            for (var i = 0; i < dirCount - 1; i++)
            {
                dirIndexOffsets[i + 1] = dirIndexOffsets[i] + delays[i].Length;
            }

            // Offsets in each directions array, since we don't go through each with the same index.
            Span<int> dirDelayOffsets = stackalloc int[dirCount];
            dirDelayOffsets.Fill(0);

            // Output delays list.
            var newDelays = new List<int>();

            // Output indices list.
            var newIndices = new List<int>[dirCount];
            for (var d = 0; d < dirCount; d++)
            {
                newIndices[d] = new List<int>();
            }

            // Actually get churning through these delays.
            while (true)
            {
                var minDelay = int.MaxValue;

                // Calculate the minimum delay for each direction we're currently at.
                for (var d = 0; d < dirCount; d++)
                {
                    var o = dirDelayOffsets[d];
                    var delay = iDelays[d][o];

                    minDelay = Math.Min(delay, minDelay);

                    // This will obviously be a frame, so write the index for the texture into output indices.
                    newIndices[d].Add(dirIndexOffsets[d] + o);
                }

                // Add said minimum delay to the output delays.
                newDelays.Add(minDelay);

                for (var d = 0; d < dirCount; d++)
                {
                    ref var o = ref dirDelayOffsets[d];
                    ref var delay = ref iDelays[d][o];

                    // Subtract working delays.
                    delay -= minDelay;

                    if (delay == 0)
                    {
                        // Increment offset in array for the direction(s) that fully completed a frame this iteration.
                        o += 1;
                    }

                    // We've reached the end of one direction.
                    // Since every direction has the same length, this *must* mean we're done.
                    if (o == iDelays[d].Length)
                    {
                        goto done;
                    }
                }
            }

            done:

            // Turn output data into a format suitable for returning and we're done.
            var floatDelays = new float[newDelays.Count];

            for (var i = 0; i < newDelays.Count; i++)
            {
                floatDelays[i] = newDelays[i] / fixedPointResolution;
            }

            var arrayIndices = new int[dirCount][];
            for (var d = 0; d < dirCount; d++)
            {
                arrayIndices[d] = newIndices[d].ToArray();
            }

            return (floatDelays, arrayIndices);
        }

        internal sealed class LoadStepData
        {
            public bool Bad;
            public ResourcePath Path = default!;
            public Image<Rgba32> AtlasSheet = default!;
            public int DimX;
            public StateReg[] AtlasList = default!;
            public Vector2i FrameSize;
            public Dictionary<RSI.StateId, Vector2i[][]> CallbackOffsets = default!;
            public Texture AtlasTexture = default!;
            public RSI Rsi = default!;
        }

        internal struct StateReg
        {
            public Image<Rgba32> Src;
            public Texture[][] Output;
            public int[][] Indices;
            public Vector2i[][] Offsets;
            public int TotalFrameCount;
        }

        internal sealed class RsiMetadata
        {
            public RsiMetadata(Vector2i size, StateMetadata[] states)
            {
                Size = size;
                States = states;
            }

            public Vector2i Size { get; }
            public StateMetadata[] States { get; }
        }

        internal sealed class StateMetadata
        {
            public StateMetadata(RSI.StateId stateId, RSI.State.DirectionType dirType, float[][] delays)
            {
                StateId = stateId;
                DirType = dirType;
                Delays = delays;

                DebugTools.Assert(delays.Length == DirCount);
                DebugTools.Assert(StateId.IsValid);
            }

            public RSI.StateId StateId { get; }
            public RSI.State.DirectionType DirType { get; }

            public int DirCount => DirType switch
            {
                RSI.State.DirectionType.Dir1 => 1,
                RSI.State.DirectionType.Dir4 => 4,
                RSI.State.DirectionType.Dir8 => 8,
                _ => 1
            };

            public float[][] Delays { get; }
        }

        // To be directly deserialized.
        [UsedImplicitly]
        private sealed record RsiJsonMetadata(Vector2i Size, StateJsonMetadata[] States)
        {
        }

        [UsedImplicitly]
        private sealed record StateJsonMetadata(string Name, int? Directions, float[][]? Delays)
        {
        }
    }

    [Serializable]
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

        protected RSILoadException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
