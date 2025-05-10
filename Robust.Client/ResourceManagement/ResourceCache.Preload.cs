using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.ResourceManagement
{
    internal partial class ResourceCache
    {
        [field: Dependency] public IClyde Clyde { get; } = default!;
        [field: Dependency] public IAudioInternal ClydeAudio { get; } = default!;
        [Dependency] private readonly IResourceManager _manager = default!;
        [field: Dependency] public IFontManager FontManager { get; } = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public void PreloadTextures()
        {
            var sawmill = _logManager.GetSawmill("res.preload");

            if (!_configurationManager.GetCVar(CVars.ResTexturePreloadingEnabled))
            {
                sawmill.Debug($"Skipping texture preloading due to CVar value.");
                return;
            }

            PreloadTextures(sawmill);
            PreloadRsis(sawmill);
        }

        private void PreloadTextures(ISawmill sawmill)
        {
            sawmill.Debug("Preloading textures...");
            var sw = Stopwatch.StartNew();
            var resList = GetTypeData<TextureResource>().Resources;

            var texList = _manager.ContentFindFiles("/Textures/")
                // Skip PNG files inside RSIs.
                .Where(p => p.Extension == "png" && !p.ToString().Contains(".rsi/") && !resList.ContainsKey(p))
                .Select(p => new TextureResource.LoadStepData {Path = p})
                .ToArray();

            Parallel.ForEach(texList, data =>
            {
                try
                {
                    TextureResource.LoadTextureParameters(_manager, data);
                    if (!data.LoadParameters.Preload)
                    {
                        data.Skip = true;
                        return;
                    }

                    TextureResource.LoadPreTextureData(_manager, data);
                }
                catch (Exception e)
                {
                    // Mark failed loads as bad and skip them in the next few stages.
                    // Avoids any silly array resizing or similar.
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            });

            foreach (var data in texList)
            {
                if (data.Bad || data.Skip)
                    continue;

                try
                {
                    TextureResource.LoadTexture(Clyde, data);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            }

            var errors = 0;
            var skipped = 0;
            foreach (var data in texList)
            {
                if (data.Bad)
                {
                    errors += 1;
                    continue;
                }

                if (data.Skip)
                {
                    skipped += 1;
                    continue;
                }

                try
                {
                    var texResource = new TextureResource();
                    texResource.LoadFinish(this, data);
                    resList[data.Path] = texResource;
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                    errors += 1;
                }
            }

            sawmill.Debug(
                "Preloaded {CountLoaded} textures ({CountErrored} errored, {CountSkipped} skipped) in {LoadTime}",
                texList.Length - skipped - errors,
                errors,
                skipped,
                sw.Elapsed);
        }

        private void PreloadRsis(ISawmill sawmill)
        {
            var sw = Stopwatch.StartNew();
            var resList = GetTypeData<RSIResource>().Resources;

            var rsiList = _manager.ContentFindFiles("/Textures/")
                .Where(p => p.ToString().EndsWith(".rsi/meta.json"))
                .Select(c => c.Directory)
                .Where(p => !resList.ContainsKey(p))
                .Select(p => new RSIResource.LoadStepData {Path = p})
                .ToArray();

            Parallel.ForEach(rsiList, data =>
            {
                try
                {
                    RSIResource.LoadPreTexture(_manager, data);
                }
                catch (Exception e)
                {
                    // Mark failed loads as bad and skip them in the next few stages.
                    // Avoids any silly array resizing or similar.
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            });

            var atlasLookup = rsiList.ToLookup(ShouldMetaAtlas);
            var atlasList = atlasLookup[true].ToArray();
            var nonAtlasList = atlasLookup[false].ToArray();

            foreach (var data in nonAtlasList)
            {
                if (data.Bad)
                    continue;

                try
                {
                    RSIResource.LoadTexture(Clyde, data);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                }
            }

            // This combines individual RSI atlases into larger atlases to reduce draw batches. currently this is a VERY
            // lazy bundling and is not at all compact, its basically an atlas of RSI atlases. Really what this should
            // try to do is to have each RSI write directly to the atlas, rather than having each RSI write to its own
            // sub-atlas first.
            //
            // Also if the max texture size is too small, such that there needs to be more than one atlas, then each
            // atlas should somehow try to group things by draw-depth & frequency to minimize batches? But currently
            // everything fits onto a single 8k x 8k image so as long as the computer can manage that, it should be
            // fine.

            // TODO allow RSIs to opt out (useful for very big & rare RSIs)
            // TODO combine with (non-rsi) texture atlas?

            // We now need to insert the RSIs into the atlas. This specific problem is 2BP|O|F - the items are oriented
            // and cutting is free. The sorting is done by a slightly modified FFDH algorithm. The algorithm is exactly
            // the same as the standard FFDH algorithm with one main difference: We create new "levels" above placed
            // blocks. For example if the first block was 10x20, then the second was 10x10 units, we would create a
            // 10x10 level above the second block that would be treated as a normal level. This increases the packing
            // efficiency from ~85% to ~95% with very little extra computational effort. The algorithm appears to be
            // ~97% effective for storing SS14s RSIs.
            //
            // Here are some more resources about the strip packing problem!
            //   -  https://en.wikipedia.org/w/index.php?title=Strip_packing_problem&oldid=1263496949#First-fit_decreasing-height_(FFDH)
            //   -  https://www.csc.liv.ac.uk/~epa/surveyhtml.html
            //   -  https://www.dei.unipd.it/~fisch/ricop/tesi/tesi_dottorato_Lodi_1999.pdf

            // The array must be sorted from biggest to smallest first.
            Array.Sort(atlasList, (b, a) => a.AtlasSheet.Height.CompareTo(b.AtlasSheet.Height));

            var maxSize = Math.Min(GL.GetInteger(GetPName.MaxTextureSize), _configurationManager.GetCVar(CVars.ResRSIAtlasSize));

            // THIS IS NOT GUARANTEED TO HAVE ANY PARTICULARLY LOGICAL ORDERING.
            // E.G you could have atlas 1 RSIs appear *before* you're done seeing atlas 2 RSIs.
            var levels = new ValueList<Level>();

            // List of all the image atlases.
            var imageAtlases = new ValueList<Image<Rgba32>>();

            // List of all the actual atlases.
            var finalAtlases = new ValueList<OwnedTexture>();

            // Number of total pixels in each atlas.
            var finalPixels = new ValueList<int>();

            // First we just find the location of all the RSIs in the atlas before actually placing them.
            // This allows us to effectively determine how much space we need to allocate for the images.
            var currentHeight = 0;
            var currentAtlasIndex = 0;
            foreach (var rsi in atlasList)
            {
                var insertHeight = rsi.AtlasSheet.Height;
                var insertWidth = rsi.AtlasSheet.Width;

                var found = false;
                for (var i = 0; i < levels.Count && !found; i++)
                {
                    var levelPosition = levels[i].Position;
                    var levelWidth = levels[i].Width;
                    var levelHeight = levels[i].Height;

                    // Check if it can fit in this level.
                    if (levelHeight < insertHeight || levelWidth + insertWidth > levels[i].MaxWidth)
                        continue;

                    found = true;

                    levels[i].Width += insertWidth;
                    rsi.AtlasOffset = levelPosition + new Vector2i(levelWidth, 0);
                    levels[i].RSIList.Add(rsi);

                    // Creating the extra "free" space above blocks that can be used for inserting more items.
                    // This differs from the FFDH spec which just ignores this space.
                    Debug.Assert(levelHeight >= insertHeight); // Must be true because the array needs to be sorted
                    if (levelHeight - insertHeight == 0)
                        continue;

                    var freeLevel = new Level
                    {
                        AtlasId = levels[i].AtlasId,
                        Position = levelPosition + new Vector2i(levelWidth, insertHeight),
                        Height = levelHeight - insertHeight,
                        Width = 0,
                        MaxWidth = insertWidth,
                        RSIList = [ ]
                    };

                    levels.Add(freeLevel);
                }

                if (found)
                    continue;

                // Ran out of space, we need to move on to the next atlas.
                // This also isn't in the normal FFDH algorithm (obviously) but its close enough.
                if (currentHeight + insertHeight > maxSize)
                {
                    imageAtlases.Add(new Image<Rgba32>(maxSize, currentHeight));
                    finalPixels.Add(0);
                    currentHeight = 0;
                    currentAtlasIndex++;
                }

                rsi.AtlasOffset = new Vector2i(0, currentHeight);

                var newLevel = new Level
                {
                    AtlasId = currentAtlasIndex,
                    Position = new Vector2i(0, currentHeight),
                    Height = insertHeight,
                    Width = insertWidth,
                    MaxWidth = maxSize,
                    RSIList = [ rsi ]
                };
                levels.Add(newLevel);

                currentHeight += insertHeight;
            }

            // This allocation takes a long time.
            imageAtlases.Add(new Image<Rgba32>(maxSize, currentHeight));
            finalPixels.Add(0);

            // Put all textures on the atlases
            foreach (var level in levels)
            {
                foreach (var rsi in level.RSIList)
                {
                    var box = new UIBox2i(0, 0, rsi.AtlasSheet.Width, rsi.AtlasSheet.Height);

                    rsi.AtlasSheet.Blit(box, imageAtlases[level.AtlasId], rsi.AtlasOffset);
                    finalPixels[level.AtlasId] += rsi.AtlasSheet.Width * rsi.AtlasSheet.Height;
                }
            }

            // Finalize the atlases.
            for (var i = 0; i < imageAtlases.Count; i++)
            {
                var atlasTexture = Clyde.LoadTextureFromImage(imageAtlases[i], $"Meta atlas {i}");
                finalAtlases.Add(atlasTexture);

                sawmill.Debug($"(Meta atlas {i}) - cropped utilization: {(float)finalPixels[i] / (maxSize * imageAtlases[i].Height):P2}, fill percentage: {(float)imageAtlases[i].Height / maxSize:P2}");
            }

            // Finally, reference the actual atlas from the RSIs.
            foreach (var level in levels)
            {
                foreach (var rsi in level.RSIList)
                {
                    rsi.AtlasTexture = finalAtlases[level.AtlasId];
                }
            }

            Parallel.ForEach(rsiList, data =>
            {
                if (data.Bad)
                    return;

                try
                {
                    RSIResource.LoadPostTexture(data);
                }
                catch (Exception e)
                {
                    data.Bad = true;
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                }
            });

            var errors = 0;
            foreach (var data in rsiList)
            {
                if (data.Bad)
                {
                    errors += 1;
                    continue;
                }

                try
                {
                    var rsiRes = new RSIResource();
                    rsiRes.LoadFinish(this, data);
                    resList[data.Path] = rsiRes;
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    data.Bad = true;
                    errors += 1;
                }
            }

            sawmill.Debug(
                "Preloaded {CountLoaded} RSIs into {CountAtlas} Atlas(es?) ({CountNotAtlas} not atlassed, {CountErrored} errored) in {LoadTime}",
                rsiList.Length,
                finalAtlases.Count,
                nonAtlasList.Length,
                errors,
                sw.Elapsed);
        }

        private static bool ShouldMetaAtlas(RSIResource.LoadStepData rsi)
        {
            return rsi.MetaAtlas && rsi.LoadParameters == TextureLoadParameters.Default;
        }
    }

    /// <summary>
    ///     A "Level" to place boxes. Similar to FFDH levels, but with more parameters so we can fit in "free" levels
    ///     above placed boxes.
    /// </summary>
    internal sealed class Level
    {
        /// <summary>
        ///     Index of the atlas this is located.
        /// </summary>
        public required int AtlasId;
        /// <summary>
        ///     Bottom left of the location for the RSIs.
        /// </summary>
        public required Vector2i Position;
        /// <summary>
        ///     The current width of the level.
        /// </summary>
        /// <remarks>This can (and will) be 0. Will change.</remarks>
        public required int Width;
        /// <summary>
        ///     The current height of the level.
        /// </summary>
        /// <remarks>This value should never change.</remarks>
        public required int Height;
        /// <summary>
        ///     Maximum width of the level.
        /// </summary>
        public required int MaxWidth;
        /// <summary>
        ///     List of all the RSIs stored in this level. RSIs are ordered from tallest to smallest per level.
        /// </summary>
        public required List<RSIResource.LoadStepData> RSIList;
    }
}
