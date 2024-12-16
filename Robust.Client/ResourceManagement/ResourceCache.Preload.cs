using System;
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
            // So fuck it, just default to letting it be as large as it needs to and crop it as needed?
            var maxSize = Math.Min(GL.GetInteger(GetPName.MaxTextureSize), _configurationManager.GetCVar(CVars.ResRSIAtlasSize));
            var sheet = new Image<Rgba32>(maxSize, maxSize);

            var deltaY = 0;
            Vector2i offset = default;
            int finalized = -1;
            int atlasCount = 0;
            for (int i = 0; i < atlasList.Length; i++)
            {
                var rsi = atlasList[i];
                if (rsi.Bad)
                    continue;

                DebugTools.Assert(rsi.AtlasSheet.Width < sheet.Width);
                DebugTools.Assert(rsi.AtlasSheet.Height < sheet.Height);

                if (offset.X + rsi.AtlasSheet.Width > sheet.Width)
                {
                    offset.X = 0;
                    offset.Y += deltaY;
                }

                if (offset.Y + rsi.AtlasSheet.Height > sheet.Height)
                {
                    FinalizeMetaAtlas(i-1, sheet);
                    sheet = new Image<Rgba32>(maxSize, maxSize);
                    deltaY = 0;
                    offset = default;
                }

                deltaY = Math.Max(deltaY, rsi.AtlasSheet.Height);
                var box = new UIBox2i(0, 0, rsi.AtlasSheet.Width, rsi.AtlasSheet.Height);
                rsi.AtlasSheet.Blit(box, sheet, offset);
                rsi.AtlasOffset = offset;
                offset.X += rsi.AtlasSheet.Width;
            }

            var height = offset.Y + deltaY;
            var croppedSheet = new Image<Rgba32>(maxSize, height);
            sheet.Blit(new UIBox2i(0, 0, maxSize, height), croppedSheet, default);
            FinalizeMetaAtlas(atlasList.Length - 1, croppedSheet);

            void FinalizeMetaAtlas(int toIndex, Image<Rgba32> sheet)
            {
                var fromIndex = finalized + 1;
                var atlas = Clyde.LoadTextureFromImage(sheet, $"Meta atlas {fromIndex}-{toIndex}");
                for (int i = fromIndex; i <= toIndex; i++)
                {
                    var rsi = atlasList[i];
                    rsi.AtlasTexture = atlas;
                }

                finalized = toIndex;
                atlasCount++;
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
                atlasCount,
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
