using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenToolkit.Graphics.OpenGL4;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Utility;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.ResourceManagement
{
    internal partial class ResourceCache
    {
        [Dependency] private IClyde _clyde = null!;
        public IClyde Clyde => _clyde;
        [Dependency] private IResourceManager _manager = default!;
        [Dependency] private IFontManager _fontManager = null!;
        public IFontManager FontManager => _fontManager;
        [Dependency] private ILogManager _logManager = default!;
        [Dependency] private IConfigurationManager _configurationManager = default!;

        private readonly List<SpriteComponent> _toDeserialize = new();

        public void PreloadTextures()
        {
            var sawmill = _logManager.GetSawmill("res.preload");

            PreloadRsis(sawmill);
            if (!_configurationManager.GetCVar(CVars.ResTexturePreloadingEnabled))
            {
                sawmill.Debug($"Skipping texture preloading due to CVar value.");
                return;
            }

            PreloadTextures(sawmill);
        }

        public void AddToDeserialize(SpriteComponent component)
        {
            _toDeserialize.Add(component);
        }

        public void LoadBaseRsi(EntityUid uid, SpriteComponent component)
        {
            if (!string.IsNullOrWhiteSpace(component.rsi))
            {
                var rsiPath = SpriteSystem.TextureRoot / component.rsi;
                if (TryGetResource(rsiPath, out RSIResource? resource))
                    component._baseRsi = resource.RSI;
                else
                    Sawmill.Error($"Unable to load RSI '{rsiPath}'.");
            }

            if (component.layerDatums.Count != 0)
            {
                component.LayerMap.Clear();
                component.Layers.Clear();
                foreach (var datum in component.layerDatums)
                {
                    var layer = new SpriteComponent.Layer((uid, component), component.Layers.Count);
                    component.Layers.Add(layer);
                    component.LayerSetData(layer, datum);
                }
            }
        }

        public void AfterDeserialization()
        {
            try
			{
        		foreach (var sprite in _toDeserialize)
        		{
            		LoadBaseRsi(default, sprite);
        		}
            }
			finally
			{
				_toDeserialize.Clear();
			}
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

            var foundRsiList = _manager.ContentFindFiles("/Textures/")
                .Where(p => p.ToString().EndsWith(".rsi/meta.json"))
                .Select(c => c.Directory);

            var foundRsicList = _manager.ContentFindFiles("/Textures/")
                .Where(p => p.Extension == "rsic")
                .Select(c => c.WithExtension("rsi"));

            var rsiListEnumerable = foundRsiList
                .Concat(foundRsicList);

            if (resList.Count > 0)
                rsiListEnumerable = rsiListEnumerable.Where(p => !resList.ContainsKey(p));

            var rsiList = rsiListEnumerable
                .Select(p => new RSIResource.LoadStepData {Path = p})
                .ToArray();

            Parallel.For(
                0,
                rsiList.Length,
                i =>
                {
                    ref var datum = ref rsiList[i];
                    try
                    {
                        RSIResource.LoadPreTexture(_manager, ref datum);
                    }
                    catch (Exception e)
                    {
                        // Mark failed loads as bad and skip them in the next few stages.
                        // Avoids any silly array resizing or similar.
                        sawmill.Error($"Exception while loading RSI {datum.Path}:\n{e}");
                        datum.Bad = true;
                    }
                }
            );

            var atlasList = new List<int>();
            var nonAtlasList = new List<int>();
            var span = rsiList.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                ref var data = ref span[i];
                if (ShouldMetaAtlas(data))
                    atlasList.Add(i);
                else
                    nonAtlasList.Add(i);
            }

            foreach (var i in nonAtlasList)
            {
                ref var data = ref rsiList[i];
                if (data.Bad)
                    continue;

                try
                {
                    RSIResource.LoadTexture(Clyde, ref data);
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
            atlasList.Sort((b, a) => rsiList[a].AtlasSheet.Height.CompareTo(rsiList[b].AtlasSheet.Height));

            #if FULL_RELEASE
            var maxSize = Math.Min(GL.GetInteger(GetPName.MaxTextureSize), _configurationManager.GetCVar(CVars.ResRSIAtlasSize));
            #else
            // For tests
            var maxSize = 12288;
            if (_clyde is not ClydeHeadless)
                maxSize = Math.Min(GL.GetInteger(GetPName.MaxTextureSize), _configurationManager.GetCVar(CVars.ResRSIAtlasSize));
            #endif

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
            foreach (var i in atlasList)
            {
                ref var rsi = ref rsiList[i];
                var insertHeight = rsi.AtlasSheet.Height;
                var insertWidth = rsi.AtlasSheet.Width;

                var found = false;
                for (var j = 0; j < levels.Count && !found; j++)
                {
                    var levelPosition = levels[j].Position;
                    var levelWidth = levels[j].Width;
                    var levelHeight = levels[j].Height;

                    // Check if it can fit in this level.
                    if (levelHeight < insertHeight || levelWidth + insertWidth > levels[j].MaxWidth)
                        continue;

                    found = true;

                    levels[j].Width += insertWidth;
                    rsi.AtlasOffset = levelPosition + new Vector2i(levelWidth, 0);
                    levels[j].RSIList.Add(i);

                    // Creating the extra "free" space above blocks that can be used for inserting more items.
                    // This differs from the FFDH spec which just ignores this space.
                    Debug.Assert(levelHeight >= insertHeight); // Must be true because the array needs to be sorted
                    if (levelHeight - insertHeight == 0)
                        continue;

                    var freeLevel = new Level
                    {
                        AtlasId = levels[j].AtlasId,
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
                    RSIList = [ i ]
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
                foreach (var i in level.RSIList)
                {
                    ref var rsi = ref rsiList[i];
                    var box = new UIBox2i(0, 0, rsi.AtlasSheet.Width, rsi.AtlasSheet.Height);

                    rsi.AtlasSheet.Blit(box, imageAtlases[level.AtlasId], rsi.AtlasOffset);
                    finalPixels[level.AtlasId] += rsi.AtlasSheet.Width * rsi.AtlasSheet.Height;
                }
            }

            // Finalize the atlases.
            for (var i = 0; i < imageAtlases.Count; i++)
            {
                var imageAtlas = imageAtlases[i];
                try
                {
                    var atlasTexture = Clyde.LoadTextureFromImage(imageAtlas, $"Meta atlas {i}");
                    finalAtlases.Add(atlasTexture);

                    sawmill.Debug($"(Meta atlas {i}) - cropped utilization: {(float)finalPixels[i] / (maxSize * imageAtlas.Height):P2}, fill percentage: {(float)imageAtlas.Height / maxSize:P2}");
                }
                finally
                {
                    imageAtlas.Dispose();
                }
            }

            // Finally, reference the actual atlas from the RSIs.
            foreach (var level in levels)
            {
                var levelSpan = CollectionsMarshal.AsSpan(level.RSIList);
                foreach (var i in levelSpan)
                {
                    ref var rsi = ref rsiList[i];
                    rsi.AtlasTexture = finalAtlases[level.AtlasId];
                }
            }

            Parallel.For(
                0,
                rsiList.Length,
                i =>
                {
                    ref var data = ref rsiList[i];
                    if (data.Bad)
                        return;

                    try
                    {
                        RSIResource.LoadPostTexture(ref data);
                    }
                    catch (Exception e)
                    {
                        data.Bad = true;
                        sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                    }
                }
            );

            var errors = 0;
            foreach (ref var data in rsiList.AsSpan())
            {
                try
                {
                    if (data.Bad)
                    {
                        errors += 1;
                        continue;
                    }

                    try
                    {
                        var rsiRes = new RSIResource();
                        rsiRes.LoadFinish(this, ref data);
                        resList[data.Path] = rsiRes;
                    }
                    catch (Exception e)
                    {
                        sawmill.Error($"Exception while loading RSI {data.Path}:\n{e}");
                        data.Bad = true;
                        errors += 1;
                    }
                }
                finally
                {
                    data.AtlasSheet?.Dispose();
                }
            }

            sawmill.Debug(
                "Preloaded {CountLoaded} RSIs into {CountAtlas} Atlas(es?) ({CountNotAtlas} not atlassed, {CountErrored} errored) in {LoadTime}",
                rsiList.Length,
                finalAtlases.Count,
                nonAtlasList.Count,
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
        public required List<int> RSIList;
    }
}
