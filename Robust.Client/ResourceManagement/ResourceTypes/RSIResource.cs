using System;
using System.Collections.Generic;
using System.IO;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Resources;
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
        public override ResPath? Fallback => new("/Textures/error.rsi");

        public RSI RSI { get; private set; } = default!;

        /// <summary>
        ///     The minimum version of RSI we can load.
        /// </summary>
        public const uint MINIMUM_RSI_VERSION = RsiLoading.MINIMUM_RSI_VERSION;

        /// <summary>
        ///     The maximum version of RSI we can load.
        /// </summary>
        public const uint MAXIMUM_RSI_VERSION = RsiLoading.MAXIMUM_RSI_VERSION;

        public override void Load(IDependencyCollection dependencies, ResPath path)
        {
            var loadStepData = new LoadStepData {Path = path};
            var manager = dependencies.Resolve<IResourceManager>();
            LoadPreTexture(manager, ref loadStepData);
            LoadTexture(dependencies.Resolve<IClyde>(), ref loadStepData);
            LoadPostTexture(ref loadStepData);
            LoadFinish(dependencies.Resolve<IResourceCacheInternal>(), ref loadStepData);

            loadStepData.AtlasSheet.Dispose();
        }

        internal static void LoadTexture(IClyde clyde, ref LoadStepData loadStepData)
        {
            loadStepData.AtlasTexture = clyde.LoadTextureFromImage(
                loadStepData.AtlasSheet,
                loadStepData.Path.ToString(),
                loadStepData.LoadParameters);
        }

        internal static void LoadPreTexture(IResourceManager manager, ref LoadStepData data)
        {
            var manifestPath = data.Path / "meta.json";
            if (manager.TryContentFileRead(manifestPath, out var manifestFile))
            {
                LoadPreTextureFolder(manager, ref data, manifestFile);
            }
            else
            {
                var rsicPath = data.Path.WithExtension("rsic");
                if (manager.TryContentFileRead(rsicPath, out var rsicFile))
                {
                    LoadPreTextureRsic(ref data, rsicFile);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to find .rsi file: {data.Path}");
                }
            }
        }

        private static void LoadPreTextureFolder(IResourceManager manager, ref LoadStepData data, Stream manifestFile)
        {
            RsiLoading.RsiMetadata metadata;
            using (manifestFile)
            {
                metadata = RsiLoading.LoadRsiMetadata(manifestFile);
            }

            Image<Rgba32>[]? images = null;
            Image<Rgba32> sheet;

            try
            {
                data.FrameCounts = RsiLoading.CalculateFrameCounts(metadata);
                var path = data.Path;
                images = RsiLoading.LoadImages(
                    metadata,
                    SixLabors.ImageSharp.Configuration.Default,
                    name =>
                    {
                        var texPath = path / (name + ".png");
                        return manager.ContentFileRead(texPath);
                    });

                sheet = RsiLoading.GenerateAtlas(
                    metadata,
                    data.FrameCounts,
                    images,
                    SixLabors.ImageSharp.Configuration.Default,
                    out var dimensionX);

                data.AtlasSheet = sheet;
                data.DimX = dimensionX;
            }
            finally
            {
                if (images != null)
                {
                    foreach (var image in images)
                    {
                        image.Dispose();
                    }
                }
            }

            LoadPreTextureCommon(metadata, ref data);

            data.LoadParameters = metadata.LoadParameters;
            data.MetaAtlas = metadata.MetaAtlas;
        }

        private static void LoadPreTextureRsic(ref LoadStepData data, Stream rsicFile)
        {
            Image<Rgba32> image;
            using (rsicFile)
            {
                image = Image.Load<Rgba32>(rsicFile);
            }

            data.AtlasSheet = image;

            var textDataList = image.Metadata.GetPngMetadata().TextData;
            if (!textDataList.TryFirstOrNull(
                    static data => data.Keyword == RsiLoading.RsicPngField,
                    out var pngMetadata))
                throw new InvalidDataException(".rsic does not have metadata field");

            var metadata = RsiLoading.LoadRsiMetadata(pngMetadata.Value.Value);

            data.FrameCounts = RsiLoading.CalculateFrameCounts(metadata);

            LoadPreTextureCommon(metadata, ref data);

            data.DimX = image.Width / metadata.Size.X;
            data.LoadParameters = metadata.LoadParameters;
            data.MetaAtlas = metadata.MetaAtlas;
        }

        private static void LoadPreTextureCommon(
            RsiLoading.RsiMetadata metadata,
            ref LoadStepData data)
        {
            var stateCount = metadata.States.Length;
            var toAtlas = new StateReg[stateCount];

            var frameSize = metadata.Size;
            var rsi = new RSI(frameSize, data.Path, metadata.States.Length);

            var callbackOffsets = new Dictionary<RSI.StateId, Vector2i[][]>(stateCount);

            // Do every state.
            for (var index = 0; index < metadata.States.Length; index++)
            {
                ref var reg = ref toAtlas[index];

                var stateObject = metadata.States[index];

                var (foldedDelays, foldedIndices) = FoldDelays(stateObject.Delays);

                var textures = new AtlasTexture[foldedIndices.Length][];
                var callbackOffset = new Vector2i[foldedIndices.Length][];

                for (var i = 0; i < textures.Length; i++)
                {
                    textures[i] = new AtlasTexture[foldedIndices[0].Length];
                    callbackOffset[i] = new Vector2i[foldedIndices[0].Length];
                }

                reg.Output = textures;
                reg.Indices = foldedIndices;
                reg.Offsets = callbackOffset;

                var dirType = stateObject.DirCount switch
                {
                    1 => RsiDirectionType.Dir1,
                    4 => RsiDirectionType.Dir4,
                    8 => RsiDirectionType.Dir8,
                    _ => throw new InvalidOperationException()
                };

                var state = new RSI.State(
                    frameSize,
                    rsi,
                    stateObject.StateId,
                    dirType,
                    foldedDelays,
                    textures);

                rsi.AddState(state);

                callbackOffsets[stateObject.StateId] = callbackOffset;
            }

            data.Rsi = rsi;
            data.CallbackOffsets = callbackOffsets;
            data.AtlasList = toAtlas;
            data.FrameSize = frameSize;
        }

        internal static void LoadPostTexture(ref LoadStepData data)
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
                        dirOutput[j] = new AtlasTexture(texture, UIBox2.FromDimensions(data.AtlasOffset + sheetPos, frameSize));
                    }
                }

                sheetOffset += data.FrameCounts[toAtlasIndex];
            }
        }

        internal void LoadFinish(IResourceCacheInternal cache, ref LoadStepData data)
        {
            RSI = data.Rsi;
            cache.RsiLoaded(new RsiLoadedEventArgs(data.Path, this, data.AtlasSheet, data.CallbackOffsets));
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

        internal struct LoadStepData()
        {
            public bool Bad = false;
            public ResPath Path = default!;
            public Image<Rgba32> AtlasSheet = null!;
            public int DimX = 0;
            public StateReg[] AtlasList = null!;
            public int[] FrameCounts = null!;
            public Vector2i FrameSize = default;
            public Dictionary<RSI.StateId, Vector2i[][]> CallbackOffsets = null!;
            public Texture AtlasTexture = null!;
            public Vector2i AtlasOffset = default;
            public RSI Rsi = null!;
            public TextureLoadParameters LoadParameters = default;
            public bool MetaAtlas = false;
        }

        internal struct StateReg
        {
            public AtlasTexture[][] Output;
            public int[][] Indices;
            public Vector2i[][] Offsets;
        }
    }
}
