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
                    TextureResource.LoadPreTexture(_manager, data);
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
                if (data.Bad)
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
            foreach (var data in texList)
            {
                if (data.Bad)
                {
                    errors += 1;
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
                "Preloaded {CountLoaded} textures ({CountErrored} errored) in {LoadTime}",
                texList.Length,
                errors,
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

            Array.Sort(atlasList, (b, a) => (b.AtlasSheet?.Height ?? 0).CompareTo(a.AtlasSheet?.Height ?? 0));

            // Each RSI sub atlas has a different size.
            // Even if we iterate through them once to estimate total area, I have NFI how to sanely estimate an optimal square-texture size.
            // So fuck it, just default to adding them in from smallest to largest.
            var maxSize = Math.Min(GL.GetInteger(GetPName.MaxTextureSize), _configurationManager.GetCVar(CVars.ResRSIAtlasSize));

            // (final meta atlas index, offset)
            var rsiPositions = new (int, Vector2i)[atlasList.Length];

            // (start rsi index, end rsi index, final meta atlas reference).
            var finalMetaAtlases = new List<(int, int, Image<Rgba32>)>();

            // First calculate the position of all sub atlases in the actual atlases.
            // This allows us to get the correct size of the atlases before allocating them.
            var deltaY = 0;
            Vector2i offset = default;
            int atlasRsiIndexStart = 0;
            int filledPixels = 0;
            for (int i = 0; i < atlasList.Length; i++)
            {
                var rsi = atlasList[i];
                if (rsi.Bad)
                    continue;

                DebugTools.Assert(rsi.AtlasSheet.Width < maxSize);
                DebugTools.Assert(rsi.AtlasSheet.Height < maxSize);

                if (offset.X + rsi.AtlasSheet.Width > maxSize)
                {
                    offset.X = 0;
                    offset.Y += deltaY;
                }

                // Make a new atlas - there isn't enough room on only one.
                if (offset.Y + rsi.AtlasSheet.Height > maxSize)
                {
                    AddAtlas(atlasRsiIndexStart, i);
                    deltaY = 0;
                    offset = default;
                    atlasRsiIndexStart = i + 1;
                    filledPixels = 0;
                }

                deltaY = Math.Max(deltaY, rsi.AtlasSheet.Height);
                rsiPositions[i] = (finalMetaAtlases.Count, offset);
                offset.X += rsi.AtlasSheet.Width;
                filledPixels += rsi.AtlasSheet.Width * rsi.AtlasSheet.Height;
            }

            AddAtlas(atlasRsiIndexStart, atlasList.Length-1);

            // Load the RSI atlases into their respective final atlas.
            for (var i = 0; i < atlasList.Length; i++)
            {
                var rsi = atlasList[i];
                var atlasIndex = rsiPositions[i].Item1;
                var newOff = rsiPositions[i].Item2;
                var box = new UIBox2i(0, 0, rsi.AtlasSheet.Width, rsi.AtlasSheet.Height);

                rsi.AtlasSheet.Blit(box, finalMetaAtlases[atlasIndex].Item3, newOff);
                rsi.AtlasOffset = newOff;
            }

            foreach (var atlas in finalMetaAtlases)
            {
                FinalizeMetaAtlas(atlas.Item1, atlas.Item2, atlas.Item3);
            }

            void FinalizeMetaAtlas(int fromIndex, int toIndex, Image<Rgba32> sheet)
            {
                var atlas = Clyde.LoadTextureFromImage(sheet, $"Meta atlas {fromIndex}-{toIndex}");
                for (int i = fromIndex; i <= toIndex; i++)
                {
                    var rsi = atlasList[i];
                    rsi.AtlasTexture = atlas;
                }
            }

            void AddAtlas(int fromIndex, int toIndex)
            {
                var width = maxSize;
                var height = offset.Y + deltaY;

                finalMetaAtlases.Add((fromIndex, toIndex, new Image<Rgba32>(width, height)));
                sawmill.Info($"Atlas {finalMetaAtlases.Count-1} - Cropped utilization: {(float)filledPixels / (maxSize * height):P2}, fill percentage: {(float)height / maxSize:P2}");
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
                finalMetaAtlases.Count,
                nonAtlasList.Length,
                errors,
                sw.Elapsed);
        }

        private static bool ShouldMetaAtlas(RSIResource.LoadStepData rsi)
        {
            return rsi.MetaAtlas && rsi.LoadParameters == TextureLoadParameters.Default;
        }
    }
}
