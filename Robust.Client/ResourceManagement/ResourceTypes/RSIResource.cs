using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Resources;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Windows;
using IResourceManager = Robust.Shared.ContentPack.IResourceManager;

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
            => Load(dependencies, path, false);

        public override void Load(IDependencyCollection dependencies, ResPath path, bool normalLoading)
        {
            var loadStepData = new LoadStepData {Path = path};
            var manager = dependencies.Resolve<IResourceManager>();
            LoadPreTexture(manager, loadStepData, normalLoading);
            LoadTexture(dependencies.Resolve<IClyde>(), loadStepData);
            LoadPostTexture(loadStepData);
            LoadFinish(dependencies.Resolve<IResourceCacheInternal>(), loadStepData);

            loadStepData.AtlasSheet.Dispose();
        }

        internal static void LoadTexture(IClyde clyde, LoadStepData loadStepData)
        {
            loadStepData.AtlasTexture = clyde.LoadTextureFromImage(
                loadStepData.AtlasSheet,
                loadStepData.Path.ToString(),
                loadStepData.LoadParameters);
        }

        internal static void ConvertBumpToNormal(Image<Rgba32> bumpmap, Vector2i blocksize, out Image<Rgba32> normal, float factor = 2f)
        {
            int Width = bumpmap.Width;
            int Height = bumpmap.Height;
            normal = new Image<Rgba32>(Width, Height);
            int x, y;
            for (x = 0; x < Width; x++)
            for (y = 0; y < Height; y++)
            {
                int XFactor = x / blocksize.X;
                int YFactor = y / blocksize.Y;

                int XL = x - 1;
                int YL = y - 1;
                int XR = x + 1;
                int YR = y + 1;

                float Left = 0f, Right = 0f, Up = 0f, Down = 0f;

                if (XL - XFactor * blocksize.X >= 0)
                    Left = bumpmap[XL, y].R / 255f;
                if (XR - XFactor * blocksize.X < blocksize.X)
                    Right = bumpmap[XR, y].R / 255f;
                if (YL - YFactor * blocksize.Y >= 0)
                    Up = bumpmap[x, YL].R / 255f;
                if (YR - YFactor * blocksize.Y < blocksize.Y)
                    Down = bumpmap[x, YR].R / 255f;

                // very good math
                // cross product of a certain two vectors
                var Cross = new Vector3((Left - Right) * factor, (Down - Up) * factor, 1f);
                Cross.Normalize();

                var NewColor = new Rgba32((Cross.X + 1) * 0.5f, (Cross.Y + 1) * 0.5f, Cross.Z, bumpmap[x, y].A);
                normal[x, y] = NewColor;
            }
        }

        internal static void CreatePlaceholderBump(Image<Rgba32> original, Vector2i blocksize, out Image<Rgba32> bumpmap)
        {
            int x, y;
            int Width = original.Width;
            int Height = original.Height;
            bumpmap = new Image<Rgba32>(Width, Height);
            for (x = 0; x < Width; x++)
            for (y = 0; y < Height; y++)
            {
                if (original[x, y].A == 0)
                {
                    bumpmap[x, y] = new Rgba32(0f, 0f, 0f);
                    continue;
                }

                int XFactor = x / blocksize.X;
                int YFactor = y / blocksize.Y;

                int XL = x - 1;
                int YL = y - 1;
                int XR = x + 1;
                int YR = y + 1;

                byte Left = 0, Right = 0, Up = 0, Down = 0;

                if (XL - XFactor * blocksize.X >= 0)
                    Left = bumpmap[XL, y].A;
                if (XR - XFactor * blocksize.X < blocksize.X)
                    Right = bumpmap[XR, y].A;
                if (YL - YFactor * blocksize.Y >= 0)
                    Up = bumpmap[x, YL].A;
                if (YR - YFactor * blocksize.Y < blocksize.Y)
                    Down = bumpmap[x, YR].A;

                if (Left == 0 || Right == 0 || Up == 0 || Down == 0)
                {
                    bumpmap[x, y] = new Rgba32(0.5f, 0.5f, 0.5f, original[x, y].A);
                }
                else
                {
                    bumpmap[x, y] = new Rgba32(1f, 1f, 1f, original[x, y].A);
                }
            }
        }

        internal static void CreatePlaceholderNormal(Image<Rgba32> original, Vector2i blocksize, out Image<Rgba32> normalmap, bool? flat = false)
        {
            if (flat ?? false)
            {
                normalmap = new Image<Rgba32>(original.Width, original.Height);
                int x, y;
                for (x = 0; x < original.Width; x++)
                for (y = 0; y < original.Height; y++)
                    normalmap[x, y] = new Rgba32(0.5f, 0.5f, 1f);
                return;
            }
            CreatePlaceholderBump(original, blocksize, out var bumpmap);
            ConvertBumpToNormal(bumpmap, blocksize, out normalmap);
            bumpmap.Dispose();
        }

        internal static void LoadPreTexture(IResourceManager manager, LoadStepData data, bool normalLoading = false)
        {
            var manifestPath = data.Path / "meta.json";
            RsiLoading.RsiMetadata metadata;
            using (var manifestFile = manager.ContentFileRead(manifestPath))
            {
                metadata = RsiLoading.LoadRsiMetadata(manifestFile);
            }

            var stateCount = metadata.States.Length;
            var toAtlas = new StateReg[stateCount];

            var frameSize = metadata.Size;
            var rsi = new RSI(frameSize, data.Path, metadata.States.Length);

            var callbackOffsets = new Dictionary<RSI.StateId, Vector2i[][]>(stateCount);

            // Check for duplicate states
            for (var i = 0; i < metadata.States.Length; i++)
            {
                var stateId = metadata.States[i].StateId;

                for (int j = i + 1; j < metadata.States.Length; j++)
                {
                    if (stateId == metadata.States[j].StateId)
                        throw new RSILoadException($"RSI '{data.Path}' has a duplicate stateId '{stateId}'.");
                }
            }

            // Do every state.
            for (var index = 0; index < metadata.States.Length; index++)
            {
                ref var reg = ref toAtlas[index];

                var stateObject = metadata.States[index];
                // Load image from disk.
                var texPath = data.Path / (stateObject.StateId + ".png");
                var normalPath = data.Path / (stateObject.NormalId + ".png");
                var bumpPath = data.Path / (stateObject.BumpId + ".png");
                using (var stream = manager.ContentFileRead(texPath))
                {
                    var texture = Image.Load<Rgba32>(stream);
                    Image<Rgba32> normalImage;
                    if (!normalLoading) normalImage = texture;
                    else if (stateObject.NormalId is {})
                    {
                        using (var normalStream = manager.ContentFileRead(normalPath))
                        {
                            normalImage = Image.Load<Rgba32>(normalStream);
                        }
                    }
                    else
                    {
                        if (stateObject.BumpId is {})
                        {
                            using (var bumpStream = manager.ContentFileRead(bumpPath))
                            {
                                var bump = Image.Load<Rgba32>(bumpStream);
                                ConvertBumpToNormal(bump, stateObject.Size, out normalImage);
                                bump.Dispose();
                            }
                        }
                        else
                        {
                            CreatePlaceholderNormal(texture, stateObject.Size, out normalImage, metadata.FlatNormal ?? stateObject.FlatNormal ?? false);
                        }
                    }
                    if (normalLoading)
                        for (int nX = 0; nX < texture.Width; nX++)
                        for (int nY = 0; nY < texture.Height; nY++)
                        {
                            var CPixel = normalImage[nX, nY].ToVector4();
                            var NormVector = new Vector3(CPixel.X, CPixel.Y, CPixel.Z);
                            NormVector = NormVector * new Vector3(2f, 2f, 1f) - new Vector3(1f, 1f, 0f);
                            NormVector.Normalize();
                            NormVector = (NormVector + new Vector3(1f, 1f, 0f)) * new Vector3(0.5f, 0.5f, 1f);
                            var NewColor = new Rgba32(NormVector.X, NormVector.Y, NormVector.Z, texture[nX, nY].A);
                            normalImage[nX, nY] = NewColor;
                        }

                    reg.Src = (texture, normalImage);
                }
                if (reg.Src.Item1.Width % frameSize.X != 0 || reg.Src.Item1.Height % frameSize.Y != 0)
                {
                    var regDims = $"{reg.Src.Item1.Width}x{reg.Src.Item1.Height}";
                    var iconDims = $"{frameSize.X}x{frameSize.Y}";
                    throw new RSILoadException($"State '{stateObject.StateId}' image size ({regDims}) is not a multiple of the icon size ({iconDims}).");
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

                var dirType = stateObject.DirCount switch
                {
                    1 => RsiDirectionType.Dir1,
                    4 => RsiDirectionType.Dir4,
                    8 => RsiDirectionType.Dir8,
                    _ => throw new InvalidOperationException()
                };

                var state = new RSI.State(frameSize, rsi, stateObject.StateId, dirType, foldedDelays,
                    textures);
                rsi.AddState(state);

                callbackOffsets[stateObject.StateId] = callbackOffset;
            }

            // Poorly hacked in texture atlas support here.
            var totalFrameCount = toAtlas.Sum(p => p.TotalFrameCount);

            // Generate atlas.
            var dimensionX = (int) MathF.Ceiling(MathF.Sqrt(totalFrameCount));
            var dimensionY = (int) MathF.Ceiling((float) totalFrameCount / dimensionX);

            var sheetHalfWidth = dimensionX * frameSize.X;
            var sheet = new Image<Rgba32>(sheetHalfWidth * (normalLoading ? 2 : 1), dimensionY * frameSize.Y);

            var sheetIndex = 0;
            for (var index = 0; index < toAtlas.Length; index++)
            {
                ref var reg = ref toAtlas[index];
                // Blit all the frames over.
                for (var i = 0; i < reg.TotalFrameCount; i++)
                {
                    var srcWidth = (reg.Src.Item1.Width / frameSize.X);
                    var srcColumn = i % srcWidth;
                    var srcRow = i / srcWidth;
                    var srcPos = (srcColumn * frameSize.X, srcRow * frameSize.Y);

                    var sheetColumn = (sheetIndex + i) % dimensionX;
                    var sheetRow = (sheetIndex + i) / dimensionX;
                    var sheetPos = (sheetColumn * frameSize.X, sheetRow * frameSize.Y);
                    var sheetNPos = (sheetColumn * frameSize.X + sheetHalfWidth, sheetRow * frameSize.Y);

                    var srcBox = UIBox2i.FromDimensions(srcPos, frameSize);

                    reg.Src.Item1.Blit(srcBox, sheet, sheetPos);
                    if (normalLoading)
                        reg.Src.Item2.Blit(srcBox, sheet, sheetNPos);
                }

                sheetIndex += reg.TotalFrameCount;
            }

            for (var i = 0; i < toAtlas.Length; i++)
            {
                ref var reg = ref toAtlas[i];
                reg.Src.Item1.Dispose();
                if (normalLoading)
                    reg.Src.Item2.Dispose();
            }

            data.Rsi = rsi;
            data.AtlasSheet = sheet;
            data.SheetNormalOffset = sheetHalfWidth;
            data.AtlasList = toAtlas;
            data.FrameSize = frameSize;
            data.DimX = dimensionX;
            data.CallbackOffsets = callbackOffsets;
            data.LoadParameters = metadata.LoadParameters;
            data.MetaAtlas = metadata.MetaAtlas;
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
                        dirOutput[j] = new AtlasTexture(texture, UIBox2.FromDimensions(data.AtlasOffset + sheetPos, frameSize), data.SheetNormalOffset);
                    }
                }

                sheetOffset += reg.TotalFrameCount;
            }
        }

        internal void LoadFinish(IResourceCacheInternal cache, LoadStepData data)
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

        internal sealed class LoadStepData
        {
            public bool Bad;
            public ResPath Path = default!;
            public Image<Rgba32> AtlasSheet = default!;
            public int SheetNormalOffset = default!;
            public int DimX;
            public StateReg[] AtlasList = default!;
            public Vector2i FrameSize;
            public Dictionary<RSI.StateId, Vector2i[][]> CallbackOffsets = default!;
            public Texture AtlasTexture = default!;
            public Vector2i AtlasOffset;
            public RSI Rsi = default!;
            public TextureLoadParameters LoadParameters;
            public bool MetaAtlas;
        }

        internal struct StateReg
        {
            public (Image<Rgba32>, Image<Rgba32>) Src;
            public Texture[][] Output;
            public int[][] Indices;
            public Vector2i[][] Offsets;
            public int TotalFrameCount;
        }
    }
}
