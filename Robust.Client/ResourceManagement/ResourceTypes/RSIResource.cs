using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

using Newtonsoft.Json.Linq;
#if DEBUG
using NJsonSchema;
#endif
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
            var manifestPath = path / "meta.json";
            string manifestContents;

            using (var manifestFile = cache.ContentFileRead(manifestPath))
            using (var reader = new StreamReader(manifestFile))
            {
                manifestContents = reader.ReadToEnd();
            }

#if DEBUG
            if (RSISchema != null)
            {
                var errors = RSISchema.Validate(manifestContents);
                if (errors.Count != 0)
                {
                    Logger.Error($"Unable to load RSI from '{path}', {errors.Count} errors:");

                    foreach (var error in errors)
                    {
                        Logger.Error("{0}", error.ToString());
                    }

                    throw new RSILoadException($"{errors.Count} errors while loading RSI. See console.");
                }
            }
#endif

            // Ok schema validated just fine.
            var manifestJson = JObject.Parse(manifestContents);

            var toAtlas = new List<(Image<Rgba32> src, Texture[][] output, int[][] indices, Vector2i[][] offsets, int totalFrameCount)>();

            var metaData = ParseMetaData(manifestJson);
            var frameSize = metaData.Size;
            var rsi = new RSI(frameSize, path);

            var callbackOffsets = new Dictionary<RSI.StateId, Vector2i[][]>();

            // Do every state.
            foreach (var stateObject in metaData.States)
            {
                // Load image from disk.
                var texPath = path / (stateObject.StateId + ".png");
                var stream = cache.ContentFileRead(texPath);
                Image<Rgba32> image;
                using (stream)
                {
                    image = Image.Load<Rgba32>(stream);
                }
                var sheetSize = new Vector2i(image.Width, image.Height);

                if (sheetSize.X % frameSize.X != 0 || sheetSize.Y % frameSize.Y != 0)
                {
                    throw new RSILoadException("State image size is not a multiple of the icon size.");
                }

                // Load all frames into a list so we can operate on it more sanely.
                var frameCount = stateObject.Delays.Sum(delayList => delayList.Length);

                var (foldedDelays, foldedIndices) = FoldDelays(stateObject.Delays);

                var textures = new Texture[foldedIndices.Length][];
                var callbackOffset = new Vector2i[foldedIndices.Length][];

                for (var i = 0; i < textures.Length; i++)
                {
                    textures[i] = new Texture[foldedIndices[0].Length];
                    callbackOffset[i] = new Vector2i[foldedIndices[0].Length];
                }

                var state = new RSI.State(frameSize, stateObject.StateId, stateObject.DirType, foldedDelays, textures);
                rsi.AddState(state);

                toAtlas.Add((image, textures, foldedIndices, callbackOffset, frameCount));
                callbackOffsets[stateObject.StateId] = callbackOffset;
            }

            // Poorly hacked in texture atlas support here.
            var totalFrameCount = toAtlas.Sum(p => p.totalFrameCount);

            // Generate atlas.
            var dimensionX = (int) MathF.Ceiling(MathF.Sqrt(totalFrameCount));
            var dimensionY = (int) MathF.Ceiling((float) totalFrameCount / dimensionX);

            using var sheet = new Image<Rgba32>(dimensionX * frameSize.X, dimensionY * frameSize.Y);

            var sheetIndex = 0;
            foreach (var (src, _, _, _, frameCount) in toAtlas)
            {
                // Blit all the frames over.
                for (var i = 0; i < frameCount; i++)
                {
                    var srcWidth = (src.Width / frameSize.X);
                    var srcColumn = i % srcWidth;
                    var srcRow = i / srcWidth;
                    var srcPos = (srcColumn * frameSize.X, srcRow * frameSize.Y);

                    var sheetColumn = (sheetIndex + i) % dimensionX;
                    var sheetRow = (sheetIndex + i) / dimensionX;
                    var sheetPos = (sheetColumn * frameSize.X, sheetRow * frameSize.Y);

                    var srcBox = UIBox2i.FromDimensions(srcPos, frameSize);

                    src.Blit(srcBox, sheet, sheetPos);
                }

                sheetIndex += frameCount;
            }

            // Load atlas.
            var texture = Texture.LoadFromImage(sheet, path.ToString());

            var sheetOffset = 0;
            foreach (var (_, output, indices, offsets, frameCount) in toAtlas)
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    var dirIndices = indices[i];
                    var dirOutput = output[i];
                    var dirOffsets = offsets[i];

                    for (var j = 0; j < dirIndices.Length; j++)
                    {
                        var index = sheetOffset + dirIndices[j];

                        var sheetColumn = index % dimensionX;
                        var sheetRow = index / dimensionX;
                        var sheetPos = (sheetColumn * frameSize.X, sheetRow * frameSize.Y);

                        dirOffsets[j] = sheetPos;
                        dirOutput[j] = new AtlasTexture(texture, UIBox2.FromDimensions(sheetPos, frameSize));
                    }
                }

                sheetOffset += frameCount;
            }

            foreach (var (image, _, _, _, _) in toAtlas)
            {
                image.Dispose();
            }

            RSI = rsi;

            if (cache is IResourceCacheInternal cacheInternal)
            {
                cacheInternal.RsiLoaded(new RsiLoadedEventArgs(path, this, sheet, callbackOffsets));
            }
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

        internal static RsiMetadata ParseMetaData(JObject manifestJson)
        {
            var size = manifestJson["size"]!.ToObject<Vector2i>();
            var states = new List<StateMetadata>();

            foreach (var stateObject in manifestJson["states"]!.Cast<JObject>())
            {
                var stateName = stateObject["name"]!.ToObject<string>()!;
                RSI.State.DirectionType directions;
                int dirValue;

                if (stateObject.TryGetValue("directions", out var dirJToken))
                {
                    dirValue= dirJToken.ToObject<int>();
                    directions = dirValue switch
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
                if (stateObject.TryGetValue("delays", out var delayToken))
                {
                    delays = delayToken.ToObject<float[][]>()!;

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
                            delays[i] = new float[] {1};
                        }
                    }
                }
                else
                {
                    delays = new float[dirValue][];
                    // No delays specified, default to 1 frame per dir.
                    for (var i = 0; i < dirValue; i++)
                    {
                        delays[i] = new float[] {1};
                    }
                }

                states.Add(new StateMetadata(new RSI.StateId(stateName), directions, delays));
            }

            return new RsiMetadata(size, states);
        }

#if DEBUG
        private static readonly JsonSchema? RSISchema = GetSchema();

        private static JsonSchema? GetSchema()
        {
            try
            {
                string schema;
                using (var schemaStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Robust.Client.Graphics.RSI.RSISchema.json")!)
                using (var schemaReader = new StreamReader(schemaStream))
                {
                    schema = schemaReader.ReadToEnd();
                }

                return JsonSchema.FromJsonAsync(schema).Result;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Failed to load RSI JSON Schema!\n{0}", e);
                return null;
            }
        }
#endif

        internal sealed class RsiMetadata
        {
            public RsiMetadata(Vector2i size, List<StateMetadata> states)
            {
                Size = size;
                States = states;
            }

            public Vector2i Size { get; }
            public List<StateMetadata> States { get; }
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
